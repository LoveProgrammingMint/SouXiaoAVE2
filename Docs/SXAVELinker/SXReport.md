# SXReport Class

**Namespace**: `SXAVELinker`  
**File**: `SXAVELinker/SXReport.cs`  
**Type**: Sealed Class

## Overview

Task execution report that records complete information about task execution, including execution status, result data, warning messages, and timing statistics.

## Properties

| Property | Type | Read-only | Description |
|--------|------|------|------|
| TaskID | SXTaskID | Yes | Associated task ID |
| TaskName | String | Yes | Task name |
| StartTime | DateTime | Yes | Start time (UTC) |
| EndTime | DateTime | Yes | End time (UTC) |
| Duration | TimeSpan | Yes | Execution duration |
| IsSuccess | Boolean | Yes | Whether execution succeeded |
| ErrorMessage | String? | Yes | Error message (when failed) |
| Results | `Dictionary<String, Object>` | Yes | Result data dictionary |
| Warnings | `List<String>` | Yes | Warning message list |
| Info | `List<String>` | Yes | Additional information list |

## Constructors

### SXReport(SXTaskID taskID, String taskName)

Creates a task report.

**Parameters**:
- `taskID`: SXTaskID - Task unique identifier
- `taskName`: String - Task name

**Internal Flow**:
1. Record task ID and name
2. Record current UTC time as start time
3. Initialize empty result dictionary and warning list

```csharp
SXTaskID taskId = new("LoadPE");
SXReport report = new(taskId, "LoadPE");
```

## Methods

### SetSuccess()

Marks task execution as successful.

**Internal Flow**:
1. Set `IsSuccess = true`
2. Record current UTC time as end time

**Output**: No return value

```csharp
SXReport report = new(taskId, "Analyze");
// ... execute task ...
report.SetSuccess();
Console.WriteLine($"Duration: {report.Duration.TotalMilliseconds}ms");
```

### SetFailure(String errorMessage)

Marks task execution as failed.

**Parameters**:
- `errorMessage`: String - Error description

**Internal Flow**:
1. Set `IsSuccess = false`
2. Record error message
3. Record end time

**Output**: No return value

```csharp
SXReport report = new(taskId, "LoadPE");
report.SetFailure("File does not exist: C:\\test.exe");
Console.WriteLine($"Failure reason: {report.ErrorMessage}");
```

### AddResult(String key, Object value)

Adds result data.

**Parameters**:
- `key`: String - Result key name
- `value`: Object - Result value

**Internal Flow**:
- Store directly in Results dictionary (overwrites same key)

**Output**: No return value

```csharp
SXReport report = new(taskId, "ExtractFeatures");
report.AddResult("FeatureCount", 512);
report.AddResult("Sparsity", 0.66);
report.AddResult("NonZeroCount", 174);
report.SetSuccess();
```

### `GetResult<T>`(String key)

Gets result data.

**Parameters**:
- `key`: String - Result key name

**Returns**: `T?` - Typed result value, returns default if not exists or type mismatch

**Internal Flow**:
1. Try to get value from Results dictionary
2. Check if type matches
3. Return if matched, otherwise return default

```csharp
SXReport report = GetReportFromSomewhere();
Int32 featureCount = report.GetResult<Int32>("FeatureCount");
Double sparsity = report.GetResult<Double>("Sparsity");
String? label = report.GetResult<String>("Label");
```

### AddWarning(String warning)

Adds warning message.

**Parameters**:
- `warning`: String - Warning content

```csharp
report.AddWarning("Import table parsing incomplete");
report.AddWarning("Resource section too large, may contain embedded files");
```

### AddInfo(String info)

Adds additional information.

**Parameters**:
- `info`: String - Information content

```csharp
report.AddInfo("Using feature extractor version 1.0.0");
report.AddInfo("Model file: lightgbm_model.txt");
```

### ToString()

Generates report summary string.

**Output**: String - Formatted report summary

**Internal Flow**:
1. Build task name and ID
2. Add execution status
3. Add duration information
4. Add error message if failed
5. Add result list
6. Add warning count

```csharp
Console.WriteLine(report.ToString());
// Output:
// === Task Report: LoadPE ===
// ID: a1b2c3d4e5f6...
// Status: Success
// Duration: 125.50ms
// Results:
//   FileName: notepad.exe
//   FileSize: 245760
// Warnings: 0
```

## Usage Examples

### Complete Report Example

```csharp
// Create report
SXTaskID taskId = new("PredictMalware");
SXReport report = new(taskId, "PredictMalware");

try
{
    // Simulate task execution
    report.AddInfo("Starting malware detection");
    
    // Add results
    report.AddResult("Score", -7.70);
    report.AddResult("Probability", 0.0005);
    report.AddResult("Label", "Benign");
    report.AddResult("IsMalicious", false);
    
    // Add warnings (if any)
    if (someCondition)
    {
        report.AddWarning("High feature sparsity");
    }
    
    report.SetSuccess();
}
catch (Exception ex)
{
    report.SetFailure(ex.Message);
}

// Output report
Console.WriteLine(report.ToString());
```

### Extracting Data from Report

```csharp
List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);

foreach (SXReport report in reports)
{
    if (!report.IsSuccess)
    {
        Console.WriteLine($"Task {report.TaskName} failed: {report.ErrorMessage}");
        continue;
    }
    
    // Extract specific results
    if (report.TaskName == "PredictMalware")
    {
        Double score = report.GetResult<Double>("Score");
        String label = report.GetResult<String>("Label") ?? "Unknown";
        Console.WriteLine($"Prediction result: {label} (Score: {score:F4})");
    }
}
```

## Notes

- Start time is automatically recorded during construction
- End time is recorded when `SetSuccess()` or `SetFailure()` is called
- `Results` dictionary uses string keys, supports any type of value
- Pay attention to type matching when using `GetResult<T>`, returns default if mismatch
- Warning and information lists do not affect execution status
