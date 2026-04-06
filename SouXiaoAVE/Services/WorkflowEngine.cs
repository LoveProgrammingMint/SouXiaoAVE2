// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using SouXiaoAVE.Models;
using SXAVELinker;
using System.Text.Json;
using ZeroZone;
using ZeroProcess;
using ZeroRegistry;

namespace SouXiaoAVE.Services;

public sealed class ProcessMalwareDetector : IProcessMalwareDetector
{
    private readonly LightGbmPredictor _predictor;
    private readonly PeFeatureExtractor _featureExtractor;

    public ProcessMalwareDetector()
    {
        _predictor = new LightGbmPredictor(512);
        _featureExtractor = new PeFeatureExtractor();
    }

    public Task<(String Label, Double Confidence, Double Score)> DetectAsync(Byte[] peBytes)
    {
        Double[] doubleFeatures = _featureExtractor.Extract(peBytes);
        Single[] features = doubleFeatures.Select(f => (Single)f).ToArray();
        (Double score, Double probability, String label) = _predictor.PredictWithLabel(features);
        return Task.FromResult((label, probability, score));
    }
}

public sealed class WorkflowEngine : IAsyncDisposable
{
    private readonly SXLinker _linker;
    private readonly Dictionary<String, Func<SXTask, CancellationToken, Task<SXReport>>> _functions;
    private readonly LightGbmPredictor _predictor;
    private readonly IsolationService _isolationService;
    private readonly ProcessScanner _processScanner;
    private readonly RegistryScanner _registryScanner;
    private readonly ProcessMalwareDetector _processDetector;
    private Boolean _disposed;

    public SXLinker Linker => _linker;

    public event EventHandler<SXReport>? TaskCompleted;
    public event EventHandler<String>? WorkflowStarted;
    public event EventHandler<String>? WorkflowCompleted;

    public WorkflowEngine()
    {
        _linker = new SXLinker();
        _functions = new Dictionary<String, Func<SXTask, CancellationToken, Task<SXReport>>>();
        _predictor = new LightGbmPredictor(512);
        _isolationService = new IsolationService();
        _processDetector = new ProcessMalwareDetector();
        _processScanner = new ProcessScanner(_processDetector);
        _registryScanner = new RegistryScanner();

        RegisterDefaultFunctions();
    }

    private void RegisterDefaultFunctions()
    {
        RegisterFunction("LoadPE", LoadPEAsync);
        RegisterFunction("ExtractFeatures", ExtractFeaturesAsync);
        RegisterFunction("PredictMalware", PredictMalwareAsync);
        RegisterFunction("GenerateReport", GenerateReportAsync);
        RegisterFunction("IsolateFile", IsolateFileAsync);
        RegisterFunction("RestoreFile", RestoreFileAsync);
        RegisterFunction("ScanProcess", ScanProcessAsync);
        RegisterFunction("ScanProcessById", ScanProcessByIdAsync);
        RegisterFunction("ScanProcessByName", ScanProcessByNameAsync);
        RegisterFunction("ScanAllProcesses", ScanAllProcessesAsync);
        RegisterFunction("ScanRegistry", ScanRegistryAsync);
    }

    public void RegisterFunction(String name, Func<SXTask, CancellationToken, Task<SXReport>> func)
    {
        _functions[name] = func;
        _linker.RegisterFunction(name, func);
    }

    public void RegisterFunction(String name, Action<SXTask> action)
    {
        _linker.RegisterFunction(name, action);
    }

    public String CreateAnalysisWorkflow(String filePath)
    {
        return _linker.CreateWorkflow("PEAnalysis", "LoadPE", "ExtractFeatures", "PredictMalware", "GenerateReport");
    }

    public async Task<List<SXReport>> ExecuteAnalysisAsync(String filePath, CancellationToken cancellationToken = default)
    {
        String workflowId = CreateAnalysisWorkflow(filePath);

        SXTask? loadTask = _linker.GetTask("PEAnalysis_LoadPE_0");
        if (loadTask is not null)
        {
            loadTask.SetParameter("filePath", filePath);
        }

        WorkflowStarted?.Invoke(this, workflowId);
        List<SXReport> reports = await _linker.ExecuteWorkflowAsync(workflowId, cancellationToken);
        WorkflowCompleted?.Invoke(this, workflowId);

        return reports;
    }

    private async Task<SXReport> LoadPEAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
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
            Byte[] fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            task.Output = new SXData(fileBytes);

            task.SetParameter("FileName", fileInfo.Name);
            task.SetParameter("FileSize", fileBytes.Length);
            task.SetParameter("Extension", fileInfo.Extension);
            task.SetParameter("FullPath", filePath);

            report.AddResult("FileName", fileInfo.Name);
            report.AddResult("FileSize", fileBytes.Length);
            report.AddResult("Extension", fileInfo.Extension);
            report.AddResult("FullPath", filePath);
            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        return report;
    }

    private async Task<SXReport> ExtractFeaturesAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            SXData? inputData = task.Input;
            if (inputData is null)
            {
                report.SetFailure("Input data is required");
                return report;
            }

            Byte[] fileBytes = inputData.GetData();
            PeFeatureExtractor extractor = new();
            FeatureVector features = extractor.Extract(fileBytes);

            Single[] featureArray = new Single[features.Features.Length];
            for (Int32 i = 0; i < features.Features.Length; i++)
            {
                featureArray[i] = (Single)features.Features[i];
            }

            task.SetParameter("features", featureArray);
            task.Output = new SXData();

            Int32 zeroCount = features.Features.Count(f => f == 0);
            Double sparsity = (Double)zeroCount / features.Features.Length;

            task.SetParameter("FeatureCount", features.Features.Length);
            task.SetParameter("Sparsity", sparsity);
            task.SetParameter("NonZeroFeatures", features.Features.Length - zeroCount);

            report.AddResult("FeatureCount", features.Features.Length);
            report.AddResult("Sparsity", sparsity);
            report.AddResult("NonZeroFeatures", features.Features.Length - zeroCount);
            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> PredictMalwareAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            Single[]? features = task.GetParameter<Single[]>("features");
            if (features is null || features.Length == 0)
            {
                report.SetFailure("Features are required for prediction");
                return report;
            }

            (Double Score, Double Probability, String Label) prediction = _predictor.PredictWithLabel(features);

            task.SetParameter("Score", prediction.Score);
            task.SetParameter("Probability", prediction.Probability);
            task.SetParameter("Label", prediction.Label);
            task.SetParameter("IsMalicious", prediction.Label == "Malicious");

            report.AddResult("Score", prediction.Score);
            report.AddResult("Probability", prediction.Probability);
            report.AddResult("Label", prediction.Label);
            report.AddResult("IsMalicious", prediction.Label == "Malicious");
            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> GenerateReportAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            AnalysisReport analysisReport = new()
            {
                GeneratedAt = DateTime.UtcNow.ToString("O"),
                EngineVersion = "1.0.0"
            };

            String? fileName = task.GetParameter<String>("FileName");
            if (fileName is not null)
            {
                analysisReport.FileName = fileName;
            }

            Int64 fileSize = task.GetParameter<Int64>("FileSize");
            analysisReport.FileSize = fileSize;

            Int32 featureCount = task.GetParameter<Int32>("FeatureCount");
            analysisReport.FeatureCount = featureCount;

            Double sparsity = task.GetParameter<Double>("Sparsity");
            analysisReport.Sparsity = sparsity;

            Double score = task.GetParameter<Double>("Score");
            analysisReport.PredictionScore = score;

            Double probability = task.GetParameter<Double>("Probability");
            analysisReport.PredictionProbability = probability;

            String? label = task.GetParameter<String>("Label");
            analysisReport.PredictionLabel = label ?? "Unknown";

            Boolean isMalicious = task.GetParameter<Boolean>("IsMalicious");
            analysisReport.IsMalicious = isMalicious;

            String jsonReport = System.Text.Json.JsonSerializer.Serialize(analysisReport, AnalysisReportContext.Default.AnalysisReport);

            String reportPath = Path.Combine(Path.GetTempPath(), $"analysis_{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(reportPath, jsonReport, cancellationToken);

            report.AddResult("ReportPath", reportPath);
            report.AddResult("ReportSize", jsonReport.Length);
            report.AddInfo($"Report saved to: {reportPath}");
            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> IsolateFileAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            String? inputPath = task.GetParameter<String>("inputPath");
            if (String.IsNullOrEmpty(inputPath))
            {
                report.SetFailure("Input path is required");
                return report;
            }

            String? outputPath = task.GetParameter<String>("outputPath");
            String? password = task.GetParameter<String>("password");

            IsolationResult result = await _isolationService.ImportAsync(inputPath, outputPath, password);

            if (result.Success)
            {
                task.SetParameter("IsolatedPath", result.OutputPath ?? "");
                task.SetParameter("EncryptedBytes", result.EncryptedBytes);
                task.SetParameter("IsolationPassword", result.Password ?? "");

                report.AddResult("IsolatedPath", result.OutputPath ?? "");
                report.AddResult("EncryptedBytes", result.EncryptedBytes);
                report.AddResult("Password", result.Password ?? "");
                report.AddInfo($"File isolated to: {result.OutputPath}");
                report.SetSuccess();
            }
            else
            {
                report.SetFailure(result.ErrorMessage ?? "Isolation failed");
            }
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> RestoreFileAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            String? inputPath = task.GetParameter<String>("inputPath");
            if (String.IsNullOrEmpty(inputPath))
            {
                report.SetFailure("Input path is required");
                return report;
            }

            String? outputDir = task.GetParameter<String>("outputDir");
            String? password = task.GetParameter<String>("password");

            ExtractionResult result = await _isolationService.ExportAsync(inputPath, outputDir, password);

            if (result.Success)
            {
                task.SetParameter("RestoredDirectory", result.OutputDirectory ?? "");
                task.SetParameter("RestoredFileCount", result.FileCount);
                task.SetParameter("ExtractedBytes", result.ExtractedBytes);

                report.AddResult("RestoredDirectory", result.OutputDirectory ?? "");
                report.AddResult("RestoredFileCount", result.FileCount);
                report.AddResult("ExtractedBytes", result.ExtractedBytes);
                report.AddInfo($"Files restored to: {result.OutputDirectory}");
                report.SetSuccess();
            }
            else
            {
                report.SetFailure(result.ErrorMessage ?? "Restoration failed");
            }
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> ScanProcessAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            Int32 processId = task.GetParameter<Int32>("processId");
            if (processId <= 0)
            {
                report.SetFailure("Valid process ID is required");
                return report;
            }

            ProcessScanResult result = await _processScanner.ScanProcessByIdAsync(processId);

            task.SetParameter("ProcessId", result.ProcessId);
            task.SetParameter("ProcessName", result.ProcessName ?? "");
            task.SetParameter("FilePath", result.FilePath ?? "");
            task.SetParameter("Label", result.Label ?? "Unknown");
            task.SetParameter("Confidence", result.Confidence);
            task.SetParameter("Score", result.Score);
            task.SetParameter("IsMalicious", result.IsMalicious);

            report.AddResult("ProcessId", result.ProcessId);
            report.AddResult("ProcessName", result.ProcessName ?? "N/A");
            report.AddResult("FilePath", result.FilePath ?? "N/A");
            report.AddResult("Label", result.Label ?? "Unknown");
            report.AddResult("Confidence", result.Confidence);
            report.AddResult("Score", result.Score);
            report.AddResult("IsMalicious", result.IsMalicious);

            if (!String.IsNullOrEmpty(result.ErrorMessage))
            {
                report.AddInfo($"Note: {result.ErrorMessage}");
            }

            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> ScanProcessByIdAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            Int32 processId = task.GetParameter<Int32>("processId");
            if (processId <= 0)
            {
                report.SetFailure("Valid process ID is required");
                return report;
            }

            ProcessScanResult result = await _processScanner.ScanProcessByIdAsync(processId);

            report.AddResult("ProcessId", result.ProcessId);
            report.AddResult("ProcessName", result.ProcessName ?? "N/A");
            report.AddResult("FilePath", result.FilePath ?? "N/A");
            report.AddResult("Label", result.Label ?? "Unknown");
            report.AddResult("Confidence", result.Confidence);
            report.AddResult("Score", result.Score);
            report.AddResult("IsMalicious", result.IsMalicious);

            if (!String.IsNullOrEmpty(result.ErrorMessage))
            {
                report.AddInfo($"Note: {result.ErrorMessage}");
            }

            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> ScanProcessByNameAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            String? processName = task.GetParameter<String>("processName");
            if (String.IsNullOrEmpty(processName))
            {
                report.SetFailure("Process name is required");
                return report;
            }

            ProcessScanResult result = await _processScanner.ScanProcessByNameAsync(processName);

            report.AddResult("ProcessId", result.ProcessId);
            report.AddResult("ProcessName", result.ProcessName ?? "N/A");
            report.AddResult("FilePath", result.FilePath ?? "N/A");
            report.AddResult("Label", result.Label ?? "Unknown");
            report.AddResult("Confidence", result.Confidence);
            report.AddResult("Score", result.Score);
            report.AddResult("IsMalicious", result.IsMalicious);

            if (!String.IsNullOrEmpty(result.ErrorMessage))
            {
                report.AddInfo($"Note: {result.ErrorMessage}");
            }

            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> ScanAllProcessesAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            ProcessScanReport scanReport = await _processScanner.ScanAllProcessesAsync(cancellationToken: cancellationToken);

            task.SetParameter("TotalProcesses", scanReport.TotalProcesses);
            task.SetParameter("ScannedProcesses", scanReport.ScannedProcesses);
            task.SetParameter("MaliciousProcesses", scanReport.MaliciousProcesses);
            task.SetParameter("BenignProcesses", scanReport.BenignProcesses);
            task.SetParameter("SkippedProcesses", scanReport.SkippedProcesses);
            task.SetParameter("ErrorProcesses", scanReport.ErrorProcesses);

            report.AddResult("TotalProcesses", scanReport.TotalProcesses);
            report.AddResult("ScannedProcesses", scanReport.ScannedProcesses);
            report.AddResult("MaliciousProcesses", scanReport.MaliciousProcesses);
            report.AddResult("BenignProcesses", scanReport.BenignProcesses);
            report.AddResult("SkippedProcesses", scanReport.SkippedProcesses);
            report.AddResult("ErrorProcesses", scanReport.ErrorProcesses);

            Int32 maliciousCount = 0;
            foreach (ProcessScanResult result in scanReport.Results)
            {
                if (result.IsMalicious)
                {
                    maliciousCount++;
                    report.AddInfo($"[MALICIOUS] PID: {result.ProcessId}, Name: {result.ProcessName}, Path: {result.FilePath}, Confidence: {result.Confidence:P2}");
                }
            }

            report.AddInfo($"Found {maliciousCount} malicious process(es) out of {scanReport.ScannedProcesses} scanned");

            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    private async Task<SXReport> ScanRegistryAsync(SXTask task, CancellationToken cancellationToken)
    {
        SXReport report = new(task.ID, task.Name);

        try
        {
            RegistryScanReport scanReport = await _registryScanner.ScanAllAsync(cancellationToken: cancellationToken);

            task.SetParameter("TotalScannedKeys", scanReport.TotalScannedKeys);
            task.SetParameter("ThreatsFound", scanReport.ThreatsFound);
            task.SetParameter("CriticalThreats", scanReport.CriticalThreats);
            task.SetParameter("HighThreats", scanReport.HighThreats);
            task.SetParameter("MediumThreats", scanReport.MediumThreats);
            task.SetParameter("LowThreats", scanReport.LowThreats);
            task.SetParameter("ScanDuration", scanReport.ScanDuration.TotalMilliseconds);

            report.AddResult("TotalScannedKeys", scanReport.TotalScannedKeys);
            report.AddResult("ThreatsFound", scanReport.ThreatsFound);
            report.AddResult("CriticalThreats", scanReport.CriticalThreats);
            report.AddResult("HighThreats", scanReport.HighThreats);
            report.AddResult("MediumThreats", scanReport.MediumThreats);
            report.AddResult("LowThreats", scanReport.LowThreats);
            report.AddResult("ScanDurationMs", scanReport.ScanDuration.TotalMilliseconds);

            foreach (RegistryScanResult threat in scanReport.Results)
            {
                String severity = threat.Severity switch
                {
                    ThreatSeverity.Critical => "[CRITICAL]",
                    ThreatSeverity.High => "[HIGH]",
                    ThreatSeverity.Medium => "[MEDIUM]",
                    _ => "[LOW]"
                };

                report.AddInfo($"{severity} {threat.ThreatType}: {threat.RegistryPath}");
                report.AddInfo($"  Value: {threat.ValueName} = {threat.ValueData}");
                report.AddInfo($"  Action: {threat.SuggestedAction}");
            }

            report.AddInfo($"Registry scan completed in {scanReport.ScanDuration.TotalMilliseconds:F0}ms");
            report.AddInfo($"Found {scanReport.ThreatsFound} threat(s) ({scanReport.CriticalThreats} critical, {scanReport.HighThreats} high, {scanReport.MediumThreats} medium, {scanReport.LowThreats} low)");

            report.SetSuccess();
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }

        await Task.CompletedTask;
        return report;
    }

    public ProcessScanner GetProcessScanner() => _processScanner;

    public RegistryScanner GetRegistryScanner() => _registryScanner;

    public async Task<ProcessScanReport> ExecuteProcessScanAsync(
        Func<ProcessScanResult, Task>? onResult = null,
        CancellationToken cancellationToken = default)
    {
        return await _processScanner.ScanAllProcessesAsync(onResult, cancellationToken);
    }

    public async Task<RegistryScanReport> ExecuteRegistryScanAsync(
        Func<RegistryScanResult, Task>? onResult = null,
        CancellationToken cancellationToken = default)
    {
        return await _registryScanner.ScanAllAsync(onResult, cancellationToken);
    }

    public async Task<SXReport> ExecuteCustomFunctionAsync(String functionName, SXTask task, CancellationToken cancellationToken = default)
    {
        if (_functions.TryGetValue(functionName, out Func<SXTask, CancellationToken, Task<SXReport>>? func))
        {
            SXReport report = await func(task, cancellationToken);
            TaskCompleted?.Invoke(this, report);
            return report;
        }

        SXReport errorReport = new(task.ID, task.Name);
        errorReport.SetFailure($"Function '{functionName}' not found");
        return errorReport;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WorkflowEngine));
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _predictor.Dispose();
            _isolationService.Dispose();
            _processScanner.Dispose();
            await _linker.DisposeAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
