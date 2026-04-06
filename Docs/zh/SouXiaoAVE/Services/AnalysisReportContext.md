# AnalysisReportContext 类

**命名空间**: `SouXiaoAVE.Services`  
**文件**: `SouXiaoAVE/Services/AnalysisReportContext.cs`  
**类型**: 密封类 (Sealed Class)  
**基类**: `JsonSerializerContext`

## 概述

AOT兼容的JSON序列化上下文，用于.NET AOT编译环境下的JSON序列化和反序列化。预先声明可序列化类型以确保AOT编译时生成必要的序列化代码。

## 特性

```csharp
[JsonSerializable(typeof(``Dictionary<String, Object>``))]
[JsonSerializable(typeof(AnalysisReport))]
[JsonSourceGenerationOptions(WriteIndented = true)]
```

## 使用方式

### 序列化

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

### 反序列化

```csharp
String json = File.ReadAllText("report.json");
AnalysisReport? report = JsonSerializer.Deserialize(json, AnalysisReportContext.Default.AnalysisReport);
```

---

# AnalysisReport 类

**命名空间**: `SouXiaoAVE.Services`  
**文件**: `SouXiaoAVE/Services/AnalysisReportContext.cs`  
**类型**: 密封类 (Sealed Class)

## 概述

PE文件分析报告数据模型，包含文件信息、特征统计和预测结果。

## 属性

| 属性名 | 类型 | 默认值 | 描述 |
|--------|------|--------|------|
| GeneratedAt | String | String.Empty | 报告生成时间（ISO 8601格式） |
| EngineVersion | String | String.Empty | 引擎版本号 |
| FileName | String? | null | 分析文件名 |
| FileSize | Int64 | 0 | 文件大小（字节） |
| FeatureCount | Int32 | 0 | 特征数量 |
| Sparsity | Double | 0 | 特征稀疏度 |
| PredictionScore | Double | 0 | 预测原始分数 |
| PredictionProbability | Double | 0 | 恶意概率 |
| PredictionLabel | String | String.Empty | 预测标签 |
| IsMalicious | Boolean | false | 是否恶意 |

## 使用示例

### 创建报告

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

### 序列化为JSON

```csharp
String json = JsonSerializer.Serialize(report, AnalysisReportContext.Default.AnalysisReport);

// 输出:
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

### 从JSON反序列化

```csharp
String json = await File.ReadAllTextAsync("report.json");
AnalysisReport? report = JsonSerializer.Deserialize(json, AnalysisReportContext.Default.AnalysisReport);

if (report is not null)
{
    Console.WriteLine($"文件: {report.FileName}");
    Console.WriteLine($"结果: {report.PredictionLabel}");
    Console.WriteLine($"概率: {report.PredictionProbability:P2}");
}
```

### 在WorkflowEngine中使用

```csharp
// GenerateReportAsync内部使用
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

## JSON输出示例

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

## 注意事项

- 必须使用 `AnalysisReportContext.Default.AnalysisReport` 进行序列化
- AOT编译时此上下文确保序列化代码生成
- 时间使用ISO 8601格式字符串存储
- 所有属性都有默认值，避免null引用
- JSON输出缩进格式，便于阅读
