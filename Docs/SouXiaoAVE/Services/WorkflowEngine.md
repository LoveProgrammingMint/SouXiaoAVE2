# WorkflowEngine Class

**Namespace**: `SouXiaoAVE.Services`  
**File**: `SouXiaoAVE/Services/WorkflowEngine.cs`  
**Type**: Sealed Class  
**Implements**: `IAsyncDisposable`

## Overview

Workflow engine wrapper class that integrates the SXLinker core engine, providing a complete workflow for PE file analysis and malware detection.

## Properties

| Property Name | Type | Read-only | Description |
|---------------|------|-----------|-------------|
| Linker | SXLinker | Yes | Internal SXLinker instance |

## Events

| Event Name | Type | Description |
|------------|------|-------------|
| TaskCompleted | `EventHandler<SXReport>` | Triggered when a task completes |
| WorkflowStarted | `EventHandler<String>` | Triggered when a workflow starts |
| WorkflowCompleted | `EventHandler<String>` | Triggered when a workflow completes |

## Constructor

### WorkflowEngine()

Create a workflow engine instance.

**Internal Flow**:
1. Create SXLinker instance
2. Create function dictionary
3. Create LightGbmPredictor instance (512 dimensions)
4. Register default functions

```csharp
WorkflowEngine engine = new();
Console.WriteLine($"Registered functions: {engine.Linker.RegisteredFunctionCount}"); // 4
```

## Methods

### RegisterFunction(String name, `Func<SXTask, CancellationToken, Task<SXReport>>` func)

Register an async task function.

**Parameters**:
- `name`: String - Function name
- `func`: `Func<SXTask, CancellationToken, Task<SXReport>>` - Execution function

```csharp
engine.RegisterFunction("CustomAnalyze", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    // Custom analysis logic
    report.SetSuccess();
    return report;
});
```

### RegisterFunction(String name, `Action<SXTask>` action)

Register a sync task function.

**Parameters**:
- `name`: String - Function name
- `action`: `Action<SXTask>` - Execution action

```csharp
engine.RegisterFunction("Log", task =>
{
    Console.WriteLine($"Log: {task.GetParameter<String>("message")}");
});
```

### CreateAnalysisWorkflow(String filePath)

Create a PE analysis workflow.

**Parameters**:
- `filePath`: String - File path (unused, kept for interface compatibility)

**Returns**: String - Workflow ID

**Internal Flow**:
- Call Linker to create workflow with 4 tasks: LoadPE → ExtractFeatures → PredictMalware → GenerateReport

```csharp
String workflowId = engine.CreateAnalysisWorkflow("test.exe");
```

### ExecuteAnalysisAsync(String filePath, CancellationToken cancellationToken = default)

Execute PE file analysis.

**Parameters**:
- `filePath`: String - Path to the PE file to analyze
- `cancellationToken`: CancellationToken - Cancellation token

**Returns**: `Task<List<SXReport>>` - Execution reports for all tasks

**Internal Flow**:
1. Create analysis workflow
2. Get LoadPE task and set filePath parameter
3. Trigger WorkflowStarted event
4. Execute workflow
5. Trigger WorkflowCompleted event
6. Return report list

```csharp
List<SXReport> reports = await engine.ExecuteAnalysisAsync(@"C:\test.exe");

foreach (SXReport report in reports)
{
    Console.WriteLine($"{report.TaskName}: {report.IsSuccess}");
}
```

### ExecuteCustomFunctionAsync(String functionName, SXTask task, CancellationToken cancellationToken = default)

Execute a custom function.

**Parameters**:
- `functionName`: String - Function name
- `task`: SXTask - Task instance
- `cancellationToken`: CancellationToken - Cancellation token

**Returns**: `Task<SXReport>` - Execution report

```csharp
SXTask task = new("Custom", SXType.Object, SXType.Report);
task.SetParameter("data", someData);

SXReport report = await engine.ExecuteCustomFunctionAsync("CustomAnalyze", task);
```

### DisposeAsync()

Release resources.

**Internal Flow**:
1. Dispose LightGbmPredictor
2. Dispose SXLinker

```csharp
await using WorkflowEngine engine = new();
// Automatically disposed
```

## Built-in Workflow Functions

### LoadPE

Load PE file into memory.

**Input Parameters**:
- `filePath`: String - File path

**Output Parameters**:
- `FileName`: String - File name
- `FileSize`: Int64 - File size
- `Extension`: String - Extension
- `FullPath`: String - Full path

**Task Output**: SXData - File byte data

**Possible Exceptions**:
- File path is empty
- File does not exist
- Failed to read file

### ExtractFeatures

Extract 512-dimensional feature vector.

**Input**: SXData - PE file byte data

**Output Parameters**:
- `FeatureCount`: Int32 - Feature count (512)
- `Sparsity`: Double - Sparsity
- `NonZeroFeatures`: Int32 - Non-zero feature count

**Task Parameters**:
- `features`: Single[] - Feature array

**Possible Exceptions**:
- Input data is empty
- PE parsing failed

### PredictMalware

LightGBM model inference.

**Input Parameters**:
- `features`: Single[] - Feature array

**Output Parameters**:
- `Score`: Double - Raw prediction score
- `Probability`: Double - Malware probability (after Sigmoid)
- `Label`: String - Label ("Benign" or "Malicious")
- `IsMalicious`: Boolean - Whether malicious

**Possible Exceptions**:
- Features are empty
- Model not loaded
- Inference failed

### GenerateReport

Generate JSON analysis report.

**Input Parameters**:
- `FileName`: String - File name
- `FileSize`: Int64 - File size
- `FeatureCount`: Int32 - Feature count
- `Sparsity`: Double - Sparsity
- `Score`: Double - Prediction score
- `Probability`: Double - Malware probability
- `Label`: String - Label
- `IsMalicious`: Boolean - Whether malicious

**Output Parameters**:
- `ReportPath`: String - Report file path
- `ReportSize`: Int32 - Report size

**Possible Exceptions**:
- Missing parameters
- File write failed

## Usage Examples

### Basic Analysis

```csharp
await using WorkflowEngine engine = new();

// Execute analysis
List<SXReport> reports = await engine.ExecuteAnalysisAsync(@"C:\Windows\System32\notepad.exe");

// View results
foreach (SXReport report in reports)
{
    Console.WriteLine($"=== {report.TaskName} ===");
    Console.WriteLine($"Status: {(report.IsSuccess ? "Success" : "Failed")}");
    Console.WriteLine($"Duration: {report.Duration.TotalMilliseconds:F2}ms");
    
    if (!report.IsSuccess)
    {
        Console.WriteLine($"Error: {report.ErrorMessage}");
        continue;
    }
    
    foreach (KeyValuePair<String, Object> result in report.Results)
    {
        Console.WriteLine($"  {result.Key}: {result.Value}");
    }
}
```

### Get Prediction Result

```csharp
await using WorkflowEngine engine = new();

List<SXReport> reports = await engine.ExecuteAnalysisAsync(filePath);

// Get prediction report (last one)
SXReport? predictionReport = reports.FirstOrDefault(r => r.TaskName.Contains("PredictMalware"));

if (predictionReport?.IsSuccess == true)
{
    String label = predictionReport.GetResult<String>("Label") ?? "Unknown";
    Double score = predictionReport.GetResult<Double>("Score");
    Double probability = predictionReport.GetResult<Double>("Probability");
    
    Console.WriteLine($"Prediction result: {label}");
    Console.WriteLine($"Score: {score:F4}");
    Console.WriteLine($"Probability: {probability:P2}");
}
```

### Subscribe to Events

```csharp
await using WorkflowEngine engine = new();

engine.TaskCompleted += (sender, report) =>
{
    Console.WriteLine($"[Completed] {report.TaskName} - {report.Duration.TotalMilliseconds:F0}ms");
};

engine.WorkflowStarted += (sender, id) =>
{
    Console.WriteLine($"[Started] Workflow: {id}");
};

engine.WorkflowCompleted += (sender, id) =>
{
    Console.WriteLine($"[Finished] Workflow: {id}");
};

await engine.ExecuteAnalysisAsync(filePath);
```

### Custom Workflow

```csharp
await using WorkflowEngine engine = new();

// Register custom function
engine.RegisterFunction("ValidateFile", (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    String? path = task.GetParameter<String>("filePath");
    
    if (!File.Exists(path))
    {
        report.SetFailure("File does not exist");
        return Task.FromResult(report);
    }
    
    FileInfo info = new(path);
    if (info.Length > 100 * 1024 * 1024) // 100MB
    {
        report.AddWarning("File is large, analysis may take time");
    }
    
    report.AddResult("Valid", true);
    report.SetSuccess();
    return Task.FromResult(report);
});

// Create custom workflow
String workflowId = engine.Linker.CreateWorkflow("CustomAnalysis", "ValidateFile", "LoadPE", "ExtractFeatures", "PredictMalware");

SXTask? validateTask = engine.Linker.GetTask("CustomAnalysis_ValidateFile_0");
validateTask?.SetParameter("filePath", @"C:\test.exe");

List<SXReport> reports = await engine.Linker.ExecuteWorkflowAsync(workflowId);
```

## Workflow Data Flow

```
┌─────────────┐     ┌──────────────────┐     ┌────────────────┐     ┌────────────────┐
│   LoadPE    │────▶│ ExtractFeatures  │────▶│ PredictMalware │────▶│ GenerateReport │
└─────────────┘     └──────────────────┘     └────────────────┘     └────────────────┘
      │                     │                        │                      │
      ▼                     ▼                        ▼                      ▼
  SXData(bytes)        features param            Label param            JSON report file
  FileName param       FeatureCount              Score param
  FileSize param       Sparsity                  Probability param
```

## Notes

- Use `await using` to ensure resource disposal
- LightGBM model path is hardcoded as `D:\SouXiaoAVE\AIModel\lightgbm_model.txt`
- Workflow tasks automatically pass parameters and outputs
- Report files are saved in system temp directory
