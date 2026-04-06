// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using SouXiaoAVE.Models;
using SXAVELinker;
using System.Text.Json;

namespace SouXiaoAVE.Services;

public sealed class WorkflowEngine : IAsyncDisposable
{
    private readonly SXLinker _linker;
    private readonly Dictionary<String, Func<SXTask, CancellationToken, Task<SXReport>>> _functions;
    private readonly LightGbmPredictor _predictor;
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

        RegisterDefaultFunctions();
    }

    private void RegisterDefaultFunctions()
    {
        RegisterFunction("LoadPE", LoadPEAsync);
        RegisterFunction("ExtractFeatures", ExtractFeaturesAsync);
        RegisterFunction("PredictMalware", PredictMalwareAsync);
        RegisterFunction("GenerateReport", GenerateReportAsync);
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
            await _linker.DisposeAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
