// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.Net;

using SouXiaoAVE.Services;

namespace SouXiaoAVE;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly WorkflowEngine _workflowEngine;
    private readonly PeFeatureExtractor _featureExtractor;
    private TcpServer? _tcpServer;

    public Worker(ILogger<Worker> logger, WorkflowEngine workflowEngine, PeFeatureExtractor featureExtractor)
    {
        _logger = logger;
        _workflowEngine = workflowEngine;
        _featureExtractor = featureExtractor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SouXiaoAVE Service started at: {Time}", DateTimeOffset.Now);
        _logger.LogInformation("Workflow engine initialized with {Count} functions", _workflowEngine.Linker.RegisteredFunctionCount);

        _tcpServer = new TcpServer(9527, IPAddress.Any);
        _tcpServer.MessageReceived += OnMessageReceived;
        _tcpServer.ClientConnected += OnClientConnected;
        _tcpServer.ClientDisconnected += OnClientDisconnected;
        _tcpServer.ErrorOccurred += OnErrorOccurred;

        await _tcpServer.StartAsync(stoppingToken);
        _logger.LogInformation("TCP server started on port 9527");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(60000, stoppingToken);
            _logger.LogInformation("Service running. Connected clients: {Count}", _tcpServer.ConnectedClientCount);
        }
    }

    private void OnMessageReceived(Object? sender, String message)
    {
        _logger.LogInformation("Received: {Message}", message);

        _ = Task.Run(async () =>
        {
            try
            {
                String response = await ProcessMessageAsync(message);
                if (sender is TcpServer server)
                {
                    await server.BroadcastAsync(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        });
    }

    private async Task<String> ProcessMessageAsync(String message)
    {
        String[] parts = message.Split('|');
        if (parts.Length == 0)
            return "ERROR|Invalid message format";

        String command = parts[0].ToUpperInvariant();

        return command switch
        {
            "ANALYZE" => await AnalyzeFileAsync(parts),
            "STATUS" => "OK|Service running",
            "PING" => "PONG",
            _ => $"ERROR|Unknown command: {command}"
        };
    }

    private async Task<String> AnalyzeFileAsync(String[] parts)
    {
        if (parts.Length < 2)
            return "ERROR|Missing file path";

        String filePath = parts[1];
        if (!File.Exists(filePath))
            return $"ERROR|File not found: {filePath}";

        try
        {
            List<SXAVELinker.SXReport> reports = await _workflowEngine.ExecuteAnalysisAsync(filePath);
            SXAVELinker.SXReport? lastReport = reports.LastOrDefault();

            if (lastReport is null)
                return "ERROR|Analysis failed";

            Double prediction = lastReport.GetResult<Double>("Prediction");
            String label = lastReport.GetResult<String>("Label") ?? "Unknown";

            return $"OK|Prediction={prediction:F4}|Label={label}";
        }
        catch (Exception ex)
        {
            return $"ERROR|{ex.Message}";
        }
    }

    private void OnClientConnected(Object? sender, String endpoint)
    {
        _logger.LogInformation("Client connected: {Endpoint}", endpoint);
    }

    private void OnClientDisconnected(Object? sender, String endpoint)
    {
        _logger.LogInformation("Client disconnected: {Endpoint}", endpoint);
    }

    private void OnErrorOccurred(Object? sender, Exception ex)
    {
        _logger.LogError(ex, "TCP server error");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SouXiaoAVE Service stopping...");

        if (_tcpServer is not null)
        {
            await _tcpServer.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
