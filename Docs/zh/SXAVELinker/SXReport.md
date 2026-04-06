# SXReport 类

**命名空间**: `SXAVELinker`  
**文件**: `SXAVELinker/SXReport.cs`  
**类型**: 密封类 (Sealed Class)

## 概述

任务执行报告，记录任务执行的完整信息，包括执行状态、结果数据、警告信息和耗时统计。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| TaskID | SXTaskID | 是 | 关联的任务ID |
| TaskName | String | 是 | 任务名称 |
| StartTime | DateTime | 是 | 开始时间（UTC） |
| EndTime | DateTime | 是 | 结束时间（UTC） |
| Duration | TimeSpan | 是 | 执行耗时 |
| IsSuccess | Boolean | 是 | 是否执行成功 |
| ErrorMessage | String? | 是 | 错误信息（失败时） |
| Results | `Dictionary<String, Object>` | 是 | 结果数据字典 |
| Warnings | `List<String>` | 是 | 警告信息列表 |
| Info | `List<String>` | 是 | 附加信息列表 |

## 构造函数

### SXReport(SXTaskID taskID, String taskName)

创建任务报告。

**参数**:
- `taskID`: SXTaskID - 任务唯一标识
- `taskName`: String - 任务名称

**内部流程**:
1. 记录任务ID和名称
2. 记录当前UTC时间为开始时间
3. 初始化空的结果字典和警告列表

```csharp
SXTaskID taskId = new("LoadPE");
SXReport report = new(taskId, "LoadPE");
```

## 方法

### SetSuccess()

标记任务执行成功。

**内部流程**:
1. 设置 `IsSuccess = true`
2. 记录当前UTC时间为结束时间

**输出**: 无返回值

```csharp
SXReport report = new(taskId, "Analyze");
// ... 执行任务 ...
report.SetSuccess();
Console.WriteLine($"耗时: {report.Duration.TotalMilliseconds}ms");
```

### SetFailure(String errorMessage)

标记任务执行失败。

**参数**:
- `errorMessage`: String - 错误描述

**内部流程**:
1. 设置 `IsSuccess = false`
2. 记录错误信息
3. 记录结束时间

**输出**: 无返回值

```csharp
SXReport report = new(taskId, "LoadPE");
report.SetFailure("文件不存在: C:\\test.exe");
Console.WriteLine($"失败原因: {report.ErrorMessage}");
```

### AddResult(String key, Object value)

添加结果数据。

**参数**:
- `key`: String - 结果键名
- `value`: Object - 结果值

**内部流程**:
- 直接存入Results字典（覆盖同名键）

**输出**: 无返回值

```csharp
SXReport report = new(taskId, "ExtractFeatures");
report.AddResult("FeatureCount", 512);
report.AddResult("Sparsity", 0.66);
report.AddResult("NonZeroCount", 174);
report.SetSuccess();
```

### `GetResult<T>`(String key)

获取结果数据。

**参数**:
- `key`: String - 结果键名

**输出**: `T?` - 类型化的结果值，不存在或类型不匹配返回default

**内部流程**:
1. 尝试从Results字典获取值
2. 检查类型是否匹配
3. 匹配则返回，否则返回default

```csharp
SXReport report = GetReportFromSomewhere();
Int32 featureCount = report.GetResult<Int32>("FeatureCount");
Double sparsity = report.GetResult<Double>("Sparsity");
String? label = report.GetResult<String>("Label");
```

### AddWarning(String warning)

添加警告信息。

**参数**:
- `warning`: String - 警告内容

```csharp
report.AddWarning("导入表解析不完整");
report.AddWarning("资源节过大，可能包含嵌入文件");
```

### AddInfo(String info)

添加附加信息。

**参数**:
- `info`: String - 信息内容

```csharp
report.AddInfo("使用特征提取器版本 1.0.0");
report.AddInfo("模型文件: lightgbm_model.txt");
```

### ToString()

生成报告摘要字符串。

**输出**: String - 格式化的报告摘要

**内部流程**:
1. 构建任务名称和ID
2. 添加执行状态
3. 添加耗时信息
4. 失败时添加错误信息
5. 添加结果列表
6. 添加警告计数

```csharp
Console.WriteLine(report.ToString());
// 输出:
// === Task Report: LoadPE ===
// ID: a1b2c3d4e5f6...
// Status: Success
// Duration: 125.50ms
// Results:
//   FileName: notepad.exe
//   FileSize: 245760
// Warnings: 0
```

## 使用示例

### 完整报告示例

```csharp
// 创建报告
SXTaskID taskId = new("PredictMalware");
SXReport report = new(taskId, "PredictMalware");

try
{
    // 模拟任务执行
    report.AddInfo("开始恶意软件检测");
    
    // 添加结果
    report.AddResult("Score", -7.70);
    report.AddResult("Probability", 0.0005);
    report.AddResult("Label", "Benign");
    report.AddResult("IsMalicious", false);
    
    // 添加警告（如有）
    if (someCondition)
    {
        report.AddWarning("特征稀疏度较高");
    }
    
    report.SetSuccess();
}
catch (Exception ex)
{
    report.SetFailure(ex.Message);
}

// 输出报告
Console.WriteLine(report.ToString());
```

### 从报告提取数据

```csharp
List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);

foreach (SXReport report in reports)
{
    if (!report.IsSuccess)
    {
        Console.WriteLine($"任务 {report.TaskName} 失败: {report.ErrorMessage}");
        continue;
    }
    
    // 提取特定结果
    if (report.TaskName == "PredictMalware")
    {
        Double score = report.GetResult<Double>("Score");
        String label = report.GetResult<String>("Label") ?? "Unknown";
        Console.WriteLine($"预测结果: {label} (Score: {score:F4})");
    }
}
```

## 注意事项

- 开始时间在构造时自动记录
- 结束时间在调用 `SetSuccess()` 或 `SetFailure()` 时记录
- `Results` 字典使用字符串键，支持任意类型值
- 使用 `GetResult<T>` 时注意类型匹配，不匹配返回 `default`
- 警告和信息列表不会影响执行状态
