// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using SXAVELinker;

namespace Example;

public class Program
{
    public static async Task Main(String[] args)
    {
        Console.WriteLine("=== SXAVELinker Example ===");
        Console.WriteLine();

        String testFile = args.Length > 0 ? args[0] : @"C:\Windows\System32\notepad.exe";
        Console.WriteLine($"Test file: {testFile}");
        Console.WriteLine();

        await using SXLinker linker = new();

        RegisterFunctions(linker);

        Console.WriteLine("Creating workflow...");
        String workflowId = linker.CreateWorkflow("ExampleAnalysis", "LoadFile", "ProcessData", "GenerateOutput");

        SXTask? loadTask = linker.GetTask("ExampleAnalysis_LoadFile_0");
        if (loadTask is not null)
        {
            loadTask.SetParameter("filePath", testFile);
        }

        Console.WriteLine($"Workflow ID: {workflowId}");
        Console.WriteLine();

        Console.WriteLine("Executing workflow...");
        List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);

        Console.WriteLine("=== Results ===");
        foreach (SXReport report in reports)
        {
            Console.WriteLine(report.ToString());
        }

        Console.WriteLine();
        Console.WriteLine("=== Custom Task Example ===");
        await RunCustomTaskExample(linker);

        Console.WriteLine();
        Console.WriteLine("=== Event Example ===");
        await RunEventExample(linker);

        Console.WriteLine();
        Console.WriteLine("Example completed!");
    }

    private static void RegisterFunctions(SXLinker linker)
    {
        linker.RegisterFunction("LoadFile", async (task, ct) =>
        {
            SXReport report = new(task.ID, task.Name);

            String? filePath = task.GetParameter<String>("filePath");
            if (String.IsNullOrEmpty(filePath))
            {
                report.SetFailure("File path is required");
                return report;
            }

            if (!File.Exists(filePath))
            {
                report.SetFailure($"File not found: {filePath}");
                return report;
            }

            FileInfo fileInfo = new(filePath);
            Byte[] bytes = await File.ReadAllBytesAsync(filePath, ct);

            task.Output = new SXData(bytes);

            report.AddResult("FileName", fileInfo.Name);
            report.AddResult("FileSize", bytes.Length);
            report.AddResult("Extension", fileInfo.Extension);
            report.SetSuccess();

            return report;
        });

        linker.RegisterFunction("ProcessData", (task, ct) =>
        {
            SXReport report = new(task.ID, task.Name);

            SXData? input = task.Input;
            if (input is null)
            {
                report.SetFailure("Input data is required");
                return Task.FromResult(report);
            }

            Byte[] data = input.GetData();

            Int32 zeroCount = data.Count(b => b == 0);
            Double zeroRatio = (Double)zeroCount / data.Length;
            Double entropy = CalculateEntropy(data);

            task.Output = new SXData();

            report.AddResult("ZeroRatio", zeroRatio);
            report.AddResult("Entropy", entropy);
            report.AddResult("DataSize", data.Length);
            report.SetSuccess();

            return Task.FromResult(report);
        });

        linker.RegisterFunction("GenerateOutput", (task, ct) =>
        {
            SXReport report = new(task.ID, task.Name);

            report.AddResult("OutputPath", "output.json");
            report.AddResult("GeneratedAt", DateTime.UtcNow.ToString("O"));
            report.SetSuccess();

            return Task.FromResult(report);
        });
    }

    private static async Task RunCustomTaskExample(SXLinker linker)
    {
        SXTask customTask = linker.CreateTask("CustomTask", SXType.Object, SXType.Report);
        customTask.SetParameter("value", 42);
        customTask.ExecuteFunc = (task, ct) =>
        {
            SXReport report = new(task.ID, task.Name);
            Int32 value = task.GetParameter<Int32>("value");
            report.AddResult("DoubledValue", value * 2);
            report.SetSuccess();
            return Task.FromResult(report);
        };

        SXReport result = await linker.ExecuteTaskAsync(customTask.ID.ID);
        Console.WriteLine($"Custom task result: {result.GetResult<Int32>("DoubledValue")}");
    }

    private static async Task RunEventExample(SXLinker linker)
    {
        Int32 taskCount = 0;
        Int32 completedCount = 0;

        linker.TaskAdded += (sender, task) =>
        {
            taskCount++;
            Console.WriteLine($"  Task added: {task.Name}");
        };

        linker.TaskCompleted += (sender, tuple) =>
        {
            completedCount++;
            Console.WriteLine($"  Task completed: {tuple.Task.Name} ({tuple.Report.Duration.TotalMilliseconds:F2}ms)");
        };

        String workflowId = linker.CreateWorkflow("EventTest", "LoadFile", "ProcessData", "GenerateOutput");

        SXTask? loadTask = linker.GetTask("EventTest_LoadFile_0");
        if (loadTask is not null)
        {
            loadTask.SetParameter("filePath", @"C:\Windows\System32\cmd.exe");
        }

        await linker.ExecuteWorkflowAsync(workflowId);

        Console.WriteLine($"  Total tasks: {taskCount}, Completed: {completedCount}");
    }

    private static Double CalculateEntropy(Byte[] data)
    {
        if (data.Length == 0) return 0;

        Int32[] freq = new Int32[256];
        foreach (Byte b in data) freq[b]++;

        Double entropy = 0;
        Double len = data.Length;

        for (Int32 i = 0; i < 256; i++)
        {
            if (freq[i] > 0)
            {
                Double p = freq[i] / len;
                entropy -= p * Math.Log2(p);
            }
        }

        return entropy;
    }
}
