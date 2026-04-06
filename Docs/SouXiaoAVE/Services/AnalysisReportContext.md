# AnalysisReportContext Class

**Namespace**: `SouXiaoAVE.Services`  
**File**: `SouXiaoAVE/Services/AnalysisReportContext.cs`  
**Type**: Sealed Class  
**Base Class**: `JsonSerializerContext`

## Overview

AOT-compatible JSON serialization context for JSON serialization and deserialization in .NET AOT compilation environments. Pre-declares serializable types to ensure necessary serialization code is generated during AOT compilation.

## Attributes

```csharp
[JsonSerializable(typeof(``Dictionary<String, Object>``))]
[JsonSerializable(typeof(AnalysisReport))]
[JsonSourceGenerationOptions(WriteIndented = true)]
```

## Usage

### Serialization

```csharp
AnalysisReport report = new()
{
    GeneratedAt = DateTime.UtcNow.ToString("O"),
    EngineVersion = "1.0.0",
    FileName = "test.exe",
    FileSize = 102400,
    PredictionLabel = "Benign"
};

String json = JsonSerializer.Serialize(report, AnalysisReportContext.Default.AnalysisReport);
Console.WriteLine(json);
```

### Deserialization

```csharp
String json = File.ReadAllText("report.json");
AnalysisReport? report = JsonSerializer.Deserialize(json, AnalysisReportContext.Default.AnalysisReport);
```

---

# AnalysisReport Class

**Namespace**: `SouXiaoAVE.Services`  
**File**: `SouXiaoAVE/Services/AnalysisReportContext.cs`  
**Type**: Sealed Class

## Overview

PE file analysis report data model containing file information, feature statistics, and prediction results.

## Properties

| Property Name | Type | Default Value | Description |
|---------------|------|---------------|-------------|
| GeneratedAt | String | String.Empty | Report generation time (ISO 8601 format) |
| EngineVersion | String | String.Empty | Engine version number |
| FileName | String? | null | Analyzed file name |
| FileSize | Int64 | 0 | File size (bytes) |
| FeatureCount | Int32 | 0 | Feature count |
| Sparsity | Double | 0 | Feature sparsity |
| PredictionScore | Double | 0 | Raw prediction score |
| PredictionProbability | Double | 0 | Malware probability |
| PredictionLabel | String | String.Empty | Prediction label |
| IsMalicious | Boolean | false | Whether malicious |

## Usage Examples

### Creating a Report

```csharp
AnalysisReport report = new()
{
    GeneratedAt = DateTime.UtcNow.ToString("O"),
    EngineVersion = "1.0.0",
    FileName = "sample.exe",
    FileSize = 245760,
    FeatureCount = 512,
    Sparsity = 0.66,
    PredictionScore = -7.70,
    PredictionProbability = 0.0005,
    PredictionLabel = "Benign",
    IsMalicious = false
};
```

### Serialize to JSON

```csharp
String json = JsonSerializer.Serialize(report, AnalysisReportContext.Default.AnalysisReport);

// Output:
// {
//   "GeneratedAt": "2026-01-01T12:00:00.0000000Z",
//   "EngineVersion": "1.0.0",
//   "FileName": "sample.exe",
//   "FileSize": 245760,
//   "FeatureCount": 512,
//   "Sparsity": 0.66,
//   "PredictionScore": -7.7,
//   "PredictionProbability": 0.0005,
//   "PredictionLabel": "Benign",
//   "IsMalicious": false
// }
```

### Deserialize from JSON

```csharp
String json = await File.ReadAllTextAsync("report.json");
AnalysisReport? report = JsonSerializer.Deserialize(json, AnalysisReportContext.Default.AnalysisReport);

if (report is not null)
{
    Console.WriteLine($"File: {report.FileName}");
    Console.WriteLine($"Result: {report.PredictionLabel}");
    Console.WriteLine($"Probability: {report.PredictionProbability:P2}");
}
```

### Using in WorkflowEngine

```csharp
// Used internally in GenerateReportAsync
private async Task<SXReport> GenerateReportAsync(SXTask task, CancellationToken cancellationToken)
{
    AnalysisReport analysisReport = new()
    {
        GeneratedAt = DateTime.UtcNow.ToString("O"),
        EngineVersion = "1.0.0",
        FileName = task.GetParameter<String>("FileName"),
        FileSize = task.GetParameter<Int64>("FileSize"),
        FeatureCount = task.GetParameter<Int32>("FeatureCount"),
        Sparsity = task.GetParameter<Double>("Sparsity"),
        PredictionScore = task.GetParameter<Double>("Score"),
        PredictionProbability = task.GetParameter<Double>("Probability"),
        PredictionLabel = task.GetParameter<String>("Label") ?? "Unknown",
        IsMalicious = task.GetParameter<Boolean>("IsMalicious")
    };

    String jsonReport = JsonSerializer.Serialize(analysisReport, AnalysisReportContext.Default.AnalysisReport);
    
    String reportPath = Path.Combine(Path.GetTempPath(), $"analysis_{Guid.NewGuid():N}.json");
    await File.WriteAllTextAsync(reportPath, jsonReport, cancellationToken);
    
    // ...
}
```

## JSON Output Example

```json
{
  "GeneratedAt": "2026-04-04T10:30:45.1234567Z",
  "EngineVersion": "1.0.0",
  "FileName": "malware_sample.exe",
  "FileSize": 524288,
  "FeatureCount": 512,
  "Sparsity": 0.45,
  "PredictionScore": 3.85,
  "PredictionProbability": 0.979,
  "PredictionLabel": "Malicious",
  "IsMalicious": true
}
```

## Notes

- Must use `AnalysisReportContext.Default.AnalysisReport` for serialization
- This context ensures serialization code generation during AOT compilation
- Time is stored as ISO 8601 format string
- All properties have default values to avoid null references
- JSON output is indented for readability
