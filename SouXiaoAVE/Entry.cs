// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using SouXiaoAVE.Services;
using ZeroProcess;
using ZeroRegistry;

namespace SouXiaoAVE;

public sealed class Entry
{
    public static async Task Main(String[] args)
    {
        if (args.Length == 0)
        {
            await RunServiceAsync(args);
            return;
        }

        switch (args[0])
        {
            case "--analyze" when args.Length > 1:
                await RunAnalysisAsync(args[1]);
                break;

            case "--scan-process":
                await RunProcessScanAsync();
                break;

            case "--scan-process-id" when args.Length > 1 && Int32.TryParse(args[1], out Int32 pid):
                await RunProcessByIdScanAsync(pid);
                break;

            case "--scan-process-name" when args.Length > 1:
                await RunProcessByNameScanAsync(args[1]);
                break;

            case "--scan-registry":
                await RunRegistryScanAsync();
                break;

            case "--scan-all":
                await RunFullScanAsync();
                break;

            case "--help":
            case "-h":
                PrintHelp();
                break;

            default:
                await RunServiceAsync(args);
                break;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("SouXiaoAVE - PE Malware Detection Engine");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SouXiaoAVE                      Run as background service");
        Console.WriteLine("  SouXiaoAVE --analyze <file>     Analyze a PE file for malware");
        Console.WriteLine("  SouXiaoAVE --scan-process       Scan all running processes");
        Console.WriteLine("  SouXiaoAVE --scan-process-id <pid>   Scan process by ID");
        Console.WriteLine("  SouXiaoAVE --scan-process-name <name> Scan process by name");
        Console.WriteLine("  SouXiaoAVE --scan-registry      Scan registry for threats");
        Console.WriteLine("  SouXiaoAVE --scan-all           Run full system scan (processes + registry)");
        Console.WriteLine("  SouXiaoAVE --help               Show this help message");
    }

    private static async Task RunServiceAsync(String[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<PeFeatureExtractor>();
        builder.Services.AddSingleton<WorkflowEngine>();
        builder.Services.AddHostedService<Worker>();

        IHost host = builder.Build();
        await host.RunAsync();
    }

    private static async Task RunAnalysisAsync(String filePath)
    {
        Console.WriteLine("=== SouXiaoAVE PE Analysis ===");
        Console.WriteLine($"File: {filePath}");
        Console.WriteLine();

        await using WorkflowEngine engine = new();
        List<SXAVELinker.SXReport> reports = await engine.ExecuteAnalysisAsync(filePath);

        Console.WriteLine("=== Analysis Results ===");
        foreach (SXAVELinker.SXReport report in reports)
        {
            Console.WriteLine(report.ToString());
        }
    }

    private static async Task RunProcessScanAsync()
    {
        Console.WriteLine("=== SouXiaoAVE Process Scan ===");
        Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        await using WorkflowEngine engine = new();
        ProcessScanReport report = await engine.ExecuteProcessScanAsync();

        Console.WriteLine();
        Console.WriteLine("=== Scan Summary ===");
        Console.WriteLine($"Total Processes: {report.TotalProcesses}");
        Console.WriteLine($"Scanned: {report.ScannedProcesses}");
        Console.WriteLine($"Malicious: {report.MaliciousProcesses}");
        Console.WriteLine($"Benign: {report.BenignProcesses}");
        Console.WriteLine($"Skipped: {report.SkippedProcesses}");
        Console.WriteLine($"Errors: {report.ErrorProcesses}");

        if (report.MaliciousProcesses > 0)
        {
            Console.WriteLine();
            Console.WriteLine("=== Malicious Processes ===");
            foreach (ProcessScanResult result in report.Results.Where(r => r.IsMalicious))
            {
                Console.WriteLine($"  [!] PID: {result.ProcessId}");
                Console.WriteLine($"      Name: {result.ProcessName}");
                Console.WriteLine($"      Path: {result.FilePath}");
                Console.WriteLine($"      Confidence: {result.Confidence:P2}");
                Console.WriteLine($"      Score: {result.Score:F4}");
                Console.WriteLine();
            }
        }
    }

    private static async Task RunProcessByIdScanAsync(Int32 processId)
    {
        Console.WriteLine("=== SouXiaoAVE Process Scan (By ID) ===");
        Console.WriteLine($"Process ID: {processId}");
        Console.WriteLine();

        await using WorkflowEngine engine = new();
        ProcessScanner scanner = engine.GetProcessScanner();
        ProcessScanResult result = await scanner.ScanProcessByIdAsync(processId);

        PrintProcessResult(result);
    }

    private static async Task RunProcessByNameScanAsync(String processName)
    {
        Console.WriteLine("=== SouXiaoAVE Process Scan (By Name) ===");
        Console.WriteLine($"Process Name: {processName}");
        Console.WriteLine();

        await using WorkflowEngine engine = new();
        ProcessScanner scanner = engine.GetProcessScanner();
        ProcessScanResult result = await scanner.ScanProcessByNameAsync(processName);

        PrintProcessResult(result);
    }

    private static void PrintProcessResult(ProcessScanResult result)
    {
        if (!String.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
            return;
        }

        Console.WriteLine($"Process ID: {result.ProcessId}");
        Console.WriteLine($"Process Name: {result.ProcessName}");
        Console.WriteLine($"File Path: {result.FilePath}");
        Console.WriteLine($"Label: {result.Label}");
        Console.WriteLine($"Confidence: {result.Confidence:P2}");
        Console.WriteLine($"Score: {result.Score:F4}");
        Console.WriteLine($"Is Malicious: {result.IsMalicious}");

        if (result.IsMalicious)
        {
            Console.WriteLine();
            Console.WriteLine("[!] WARNING: This process appears to be malicious!");
        }
    }

    private static async Task RunRegistryScanAsync()
    {
        Console.WriteLine("=== SouXiaoAVE Registry Scan ===");
        Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        await using WorkflowEngine engine = new();
        RegistryScanReport report = await engine.ExecuteRegistryScanAsync();

        Console.WriteLine();
        Console.WriteLine("=== Scan Summary ===");
        Console.WriteLine($"Total Keys Scanned: {report.TotalScannedKeys}");
        Console.WriteLine($"Threats Found: {report.ThreatsFound}");
        Console.WriteLine($"Critical: {report.CriticalThreats}");
        Console.WriteLine($"High: {report.HighThreats}");
        Console.WriteLine($"Medium: {report.MediumThreats}");
        Console.WriteLine($"Low: {report.LowThreats}");
        Console.WriteLine($"Scan Duration: {report.ScanDuration.TotalMilliseconds:F0}ms");

        if (report.ThreatsFound > 0)
        {
            Console.WriteLine();
            Console.WriteLine("=== Threats Detected ===");

            foreach (IGrouping<String, RegistryScanResult>? group in report.Results.GroupBy(r => r.ThreatType ?? "Unknown"))
            {
                Console.WriteLine();
                Console.WriteLine($"[{group.Key}] ({group.Count()} items)");
                foreach (RegistryScanResult threat in group)
                {
                    String severity = threat.Severity switch
                    {
                        ThreatSeverity.Critical => "CRITICAL",
                        ThreatSeverity.High => "HIGH",
                        ThreatSeverity.Medium => "MEDIUM",
                        _ => "LOW"
                    };

                    Console.WriteLine($"  [{severity}] {threat.RegistryPath}");
                    Console.WriteLine($"    Value: {threat.ValueName} = {threat.ValueData}");
                    Console.WriteLine($"    Action: {threat.SuggestedAction}");
                }
            }
        }
    }

    private static async Task RunFullScanAsync()
    {
        Console.WriteLine("=== SouXiaoAVE Full System Scan ===");
        Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        await using WorkflowEngine engine = new();

        Console.WriteLine("--- Phase 1: Process Scan ---");
        ProcessScanReport processReport = await engine.ExecuteProcessScanAsync();

        Console.WriteLine();
        Console.WriteLine($"Processes: {processReport.ScannedProcesses} scanned, {processReport.MaliciousProcesses} malicious");

        Console.WriteLine();
        Console.WriteLine("--- Phase 2: Registry Scan ---");
        RegistryScanReport registryReport = await engine.ExecuteRegistryScanAsync();

        Console.WriteLine();
        Console.WriteLine($"Registry: {registryReport.TotalScannedKeys} keys scanned, {registryReport.ThreatsFound} threats");

        Console.WriteLine();
        Console.WriteLine("=== Full Scan Summary ===");
        Console.WriteLine($"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();
        Console.WriteLine("Process Scan:");
        Console.WriteLine($"  Total: {processReport.TotalProcesses}");
        Console.WriteLine($"  Malicious: {processReport.MaliciousProcesses}");
        Console.WriteLine($"  Benign: {processReport.BenignProcesses}");
        Console.WriteLine();
        Console.WriteLine("Registry Scan:");
        Console.WriteLine($"  Total Keys: {registryReport.TotalScannedKeys}");
        Console.WriteLine($"  Threats: {registryReport.ThreatsFound}");
        Console.WriteLine($"  Critical: {registryReport.CriticalThreats}");
        Console.WriteLine($"  High: {registryReport.HighThreats}");
        Console.WriteLine($"  Medium: {registryReport.MediumThreats}");
        Console.WriteLine($"  Low: {registryReport.LowThreats}");

        Int32 totalThreats = processReport.MaliciousProcesses + registryReport.ThreatsFound;
        if (totalThreats > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"[!] Total threats found: {totalThreats}");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("[OK] No threats detected.");
        }
    }
}
