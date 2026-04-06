# WorkflowEngine 类

**命名空间**: `SouXiaoAVE.Services`  
**文件**: `SouXiaoAVE/Services/WorkflowEngine.cs`  
**类型**: 密封类 (Sealed Class)  
**实现接口**: `IAsyncDisposable`

## 概述

工作流引擎封装类，集成SXLinker核心引擎，提供PE文件分析和恶意软件检测的完整工作流。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| Linker | SXLinker | 是 | 内部SXLinker实例 |

## 事件

| 事件名 | 类型 | 描述 |
|--------|------|------|
| TaskCompleted | `EventHandler<SXReport>` | 任务完成时触发 |
| WorkflowStarted | `EventHandler<String>` | 工作流开始时触发 |
| WorkflowCompleted | `EventHandler<String>` | 工作流完成时触发 |

## 构造函数

### WorkflowEngine()

创建工作流引擎实例。

**内部流程**:
1. 创建SXLinker实例
2. 创建函数字典
3. 创建LightGbmPredictor实例（512维）
4. 注册默认函数

```csharp
WorkflowEngine engine = new();
Console.WriteLine($"已注册函数: {engine.Linker.RegisteredFunctionCount}"); // 4
```

## 方法

### RegisterFunction(String name, `Func<SXTask, CancellationToken, Task<SXReport>>` func)

注册异步任务函数。

**参数**:
- `name`: String - 函数名称
- `func`: `Func<SXTask, CancellationToken, Task<SXReport>>` - 执行函数

```csharp
engine.RegisterFunction("CustomAnalyze", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    // 自定义分析逻辑
    report.SetSuccess();
    return report;
});
```

### RegisterFunction(String name, `Action<SXTask>` action)

注册同步任务函数。

**参数**:
- `name`: String - 函数名称
- `action`: `Action<SXTask>` - 执行动作

```csharp
engine.RegisterFunction("Log", task =>
{
    Console.WriteLine($"日志: {task.GetParameter<String>("message")}");
});
```

### CreateAnalysisWorkflow(String filePath)

创建PE分析工作流。

**参数**:
- `filePath`: String - 文件路径（未使用，保留接口兼容）

**输出**: String - 工作流ID

**内部流程**:
- 调用Linker创建工作流，包含4个任务：LoadPE → ExtractFeatures → PredictMalware → GenerateReport

```csharp
String workflowId = engine.CreateAnalysisWorkflow("test.exe");
```

### ExecuteAnalysisAsync(String filePath, CancellationToken cancellationToken = default)

执行PE文件分析。

**参数**:
- `filePath`: String - 要分析的PE文件路径
- `cancellationToken`: CancellationToken - 取消令牌

**输出**: `Task<List<SXReport>>` - 所有任务的执行报告

**内部流程**:
1. 创建分析工作流
2. 获取LoadPE任务并设置filePath参数
3. 触发WorkflowStarted事件
4. 执行工作流
5. 触发WorkflowCompleted事件
6. 返回报告列表

```csharp
List<SXReport> reports = await engine.ExecuteAnalysisAsync(@"C:\test.exe");

foreach (SXReport report in reports)
{
    Console.WriteLine($"{report.TaskName}: {report.IsSuccess}");
}
```

### ExecuteCustomFunctionAsync(String functionName, SXTask task, CancellationToken cancellationToken = default)

执行自定义函数。

**参数**:
- `functionName`: String - 函数名称
- `task`: SXTask - 任务实例
- `cancellationToken`: CancellationToken - 取消令牌

**输出**: `Task<SXReport>` - 执行报告

```csharp
SXTask task = new("Custom", SXType.Object, SXType.Report);
task.SetParameter("data", someData);

SXReport report = await engine.ExecuteCustomFunctionAsync("CustomAnalyze", task);
```

### DisposeAsync()

释放资源。

**内部流程**:
1. 释放LightGbmPredictor
2. 释放SXLinker

```csharp
await using WorkflowEngine engine = new();
// 自动释放
```

## 内置工作流函数

### LoadPE

加载PE文件到内存。

**输入参数**:
- `filePath`: String - 文件路径

**输出参数**:
- `FileName`: String - 文件名
- `FileSize`: Int64 - 文件大小
- `Extension`: String - 扩展名
- `FullPath`: String - 完整路径

**任务输出**: SXData - 文件字节数据

**可能的异常**:
- 文件路径为空
- 文件不存在
- 读取文件失败

### ExtractFeatures

提取512维特征向量。

**输入**: SXData - PE文件字节数据

**输出参数**:
- `FeatureCount`: Int32 - 特征数量（512）
- `Sparsity`: Double - 稀疏度
- `NonZeroFeatures`: Int32 - 非零特征数

**任务参数**:
- `features`: Single[] - 特征数组

**可能的异常**:
- 输入数据为空
- PE解析失败

### PredictMalware

LightGBM模型推理。

**输入参数**:
- `features`: Single[] - 特征数组

**输出参数**:
- `Score`: Double - 原始预测分数
- `Probability`: Double - 恶意概率（Sigmoid后）
- `Label`: String - 标签（"Benign"或"Malicious"）
- `IsMalicious`: Boolean - 是否恶意

**可能的异常**:
- 特征为空
- 模型未加载
- 推理失败

### GenerateReport

生成JSON分析报告。

**输入参数**:
- `FileName`: String - 文件名
- `FileSize`: Int64 - 文件大小
- `FeatureCount`: Int32 - 特征数量
- `Sparsity`: Double - 稀疏度
- `Score`: Double - 预测分数
- `Probability`: Double - 恶意概率
- `Label`: String - 标签
- `IsMalicious`: Boolean - 是否恶意

**输出参数**:
- `ReportPath`: String - 报告文件路径
- `ReportSize`: Int32 - 报告大小

**可能的异常**:
- 参数缺失
- 文件写入失败

## 使用示例

### 基本分析

```csharp
await using WorkflowEngine engine = new();

// 执行分析
List<SXReport> reports = await engine.ExecuteAnalysisAsync(@"C:\Windows\System32\notepad.exe");

// 查看结果
foreach (SXReport report in reports)
{
    Console.WriteLine($"=== {report.TaskName} ===");
    Console.WriteLine($"状态: {(report.IsSuccess ? "成功" : "失败")}");
    Console.WriteLine($"耗时: {report.Duration.TotalMilliseconds:F2}ms");
    
    if (!report.IsSuccess)
    {
        Console.WriteLine($"错误: {report.ErrorMessage}");
        continue;
    }
    
    foreach (KeyValuePair<String, Object> result in report.Results)
    {
        Console.WriteLine($"  {result.Key}: {result.Value}");
    }
}
```

### 获取预测结果

```csharp
await using WorkflowEngine engine = new();

List<SXReport> reports = await engine.ExecuteAnalysisAsync(filePath);

// 获取预测报告（最后一个）
SXReport? predictionReport = reports.FirstOrDefault(r => r.TaskName.Contains("PredictMalware"));

if (predictionReport?.IsSuccess == true)
{
    String label = predictionReport.GetResult<String>("Label") ?? "Unknown";
    Double score = predictionReport.GetResult<Double>("Score");
    Double probability = predictionReport.GetResult<Double>("Probability");
    
    Console.WriteLine($"预测结果: {label}");
    Console.WriteLine($"分数: {score:F4}");
    Console.WriteLine($"概率: {probability:P2}");
}
```

### 订阅事件

```csharp
await using WorkflowEngine engine = new();

engine.TaskCompleted += (sender, report) =>
{
    Console.WriteLine($"[完成] {report.TaskName} - {report.Duration.TotalMilliseconds:F0}ms");
};

engine.WorkflowStarted += (sender, id) =>
{
    Console.WriteLine($"[开始] 工作流: {id}");
};

engine.WorkflowCompleted += (sender, id) =>
{
    Console.WriteLine($"[结束] 工作流: {id}");
};

await engine.ExecuteAnalysisAsync(filePath);
```

### 自定义工作流

```csharp
await using WorkflowEngine engine = new();

// 注册自定义函数
engine.RegisterFunction("ValidateFile", (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    String? path = task.GetParameter<String>("filePath");
    
    if (!File.Exists(path))
    {
        report.SetFailure("文件不存在");
        return Task.FromResult(report);
    }
    
    FileInfo info = new(path);
    if (info.Length > 100 * 1024 * 1024) // 100MB
    {
        report.AddWarning("文件较大，分析可能耗时");
    }
    
    report.AddResult("Valid", true);
    report.SetSuccess();
    return Task.FromResult(report);
});

// 创建自定义工作流
String workflowId = engine.Linker.CreateWorkflow("CustomAnalysis", "ValidateFile", "LoadPE", "ExtractFeatures", "PredictMalware");

SXTask? validateTask = engine.Linker.GetTask("CustomAnalysis_ValidateFile_0");
validateTask?.SetParameter("filePath", @"C:\test.exe");

List<SXReport> reports = await engine.Linker.ExecuteWorkflowAsync(workflowId);
```

## 工作流数据流

```
┌─────────────┐     ┌──────────────────┐     ┌────────────────┐     ┌────────────────┐
│   LoadPE    │────▶│ ExtractFeatures  │────▶│ PredictMalware │────▶│ GenerateReport │
└─────────────┘     └──────────────────┘     └────────────────┘     └────────────────┘
      │                     │                        │                      │
      ▼                     ▼                        ▼                      ▼
  SXData(字节)        features参数              Label参数              JSON报告文件
  FileName参数        FeatureCount              Score参数
  FileSize参数        Sparsity                  Probability参数
```

## 注意事项

- 使用 `await using` 确保资源释放
- LightGBM模型路径硬编码为 `D:\SouXiaoAVE\AIModel\lightgbm_model.txt`
- 工作流任务自动传递参数和输出
- 报告文件保存在系统临时目录
