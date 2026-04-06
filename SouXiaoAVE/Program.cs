// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using SouXiaoAVE.Services;

namespace SouXiaoAVE;

public class Program
{
    public static async Task Main(String[] args)
    {
        if (args.Length > 0 && args[0] == "--analyze" && args.Length > 1)
        {
            await RunAnalysisAsync(args[1]);
            return;
        }

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<PeFeatureExtractor>();
        builder.Services.AddSingleton<WorkflowEngine>();
        builder.Services.AddHostedService<Worker>();

        IHost host = builder.Build();
        await host.RunAsync();
    }

    private static async Task RunAnalysisAsync(String filePath)
    {
        Console.WriteLine($"=== SouXiaoAVE PE Analysis ===");
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
}
