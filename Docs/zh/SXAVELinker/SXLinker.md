# SXLinker 类

**命名空间**: `SXAVELinker`  
**文件**: `SXAVELinker/SXLinker.cs`  
**类型**: 密封类 (Sealed Class)  
**实现接口**: `IAsyncDisposable`

## 概述

核心工作流引擎，负责函数注册、任务创建、工作流管理和执行调度。是SXAVELinker库的主要入口点。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| TaskCount | Int32 | 是 | 已创建任务数量 |
| WorkflowCount | Int32 | 是 | 已创建工作流数量 |
| RegisteredFunctionCount | Int32 | 是 | 已注册函数数量 |

## 事件

| 事件名 | 类型 | 描述 |
|--------|------|------|
| TaskAdded | `EventHandler<SXTask>` | 任务添加时触发 |
| TaskStarted | `EventHandler<SXTask>` | 任务开始执行时触发 |
| TaskCompleted | `EventHandler<(SXTask, SXReport)>` | 任务成功完成时触发 |
| TaskFailed | `EventHandler<(SXTask, Exception)>` | 任务失败时触发 |
| WorkflowStarted | `EventHandler<String>` | 工作流开始时触发 |
| WorkflowCompleted | `EventHandler<String>` | 工作流完成时触发 |

## 构造函数

### SXLinker()

创建工作流引擎实例。

**内部流程**:
1. 初始化任务字典
2. 初始化注册函数字典
3. 初始化工作流字典
4. 创建线程锁对象
5. 创建取消令牌源

```csharp
SXLinker linker = new();
Console.WriteLine($"初始函数数: {linker.RegisteredFunctionCount}"); // 0
```

## 方法

### RegisterFunction(String name, `Func<SXTask, CancellationToken, Task<SXReport>>` func)

注册异步任务函数。

**参数**:
- `name`: String - 函数名称
- `func`: `Func<SXTask, CancellationToken, Task<SXReport>>` - 异步执行函数

**内部流程**:
1. 检查对象是否已释放
2. 存入注册函数字典

**输出**: 无返回值

**异常**:
- `ObjectDisposedException`: 对象已释放

```csharp
linker.RegisterFunction("LoadFile", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    String? path = task.GetParameter<String>("path");
    
    if (String.IsNullOrEmpty(path))
    {
        report.SetFailure("路径不能为空");
        return report;
    }
    
    Byte[] bytes = await File.ReadAllBytesAsync(path, ct);
    task.Output = new SXData(bytes);
    report.AddResult("Size", bytes.Length);
    report.SetSuccess();
    return report;
});
```

### RegisterFunction(String name, `Action<SXTask>` action)

注册同步任务函数。

**参数**:
- `name`: String - 函数名称
- `action`: `Action<SXTask>` - 同步执行动作

**内部流程**:
1. 包装同步动作为异步函数
2. 创建成功报告

```csharp
linker.RegisterFunction("Log", task =>
{
    String? message = task.GetParameter<String>("message");
    Console.WriteLine($"[LOG] {message}");
});
```

### `RegisterFunction<TInput, TOutput>`(String name, `Func<TInput, CancellationToken, Task<TOutput>>` func)

注册泛型转换函数。

**参数**:
- `name`: String - 函数名称
- `func`: `Func<TInput, CancellationToken, Task<TOutput>>` - 转换函数

**内部流程**:
1. 从任务参数获取input
2. 调用转换函数
3. 将output存入报告结果

```csharp
linker.RegisterFunction<String, Int32>("GetLength", async (path, ct) =>
{
    Byte[] bytes = await File.ReadAllBytesAsync(path, ct);
    return bytes.Length;
});
```

### UnregisterFunction(String name)

注销函数。

**参数**:
- `name`: String - 函数名称

**输出**: Boolean - 是否成功注销

```csharp
Boolean removed = linker.UnregisterFunction("OldFunction");
```

### CreateTask(String name, SXType inputType, SXType outputType)

创建自定义任务。

**参数**:
- `name`: String - 任务名称
- `inputType`: SXType - 输入类型
- `outputType`: SXType - 输出类型

**输出**: SXTask - 创建的任务实例

**内部流程**:
1. 检查对象状态
2. 创建SXTask实例
3. 绑定开始和完成回调
4. 存入任务字典
5. 触发TaskAdded事件

**异常**:
- `ObjectDisposedException`: 对象已释放

```csharp
SXTask task = linker.CreateTask("CustomTask", SXType.Object, SXType.Report);
task.SetParameter("value", 42);
task.ExecuteFunc = (t, ct) =>
{
    SXReport report = new(t.ID, t.Name);
    Int32 value = t.GetParameter<Int32>("value");
    report.AddResult("Doubled", value * 2);
    report.SetSuccess();
    return Task.FromResult(report);
};
```

### CreateTask(String name, String functionName)

创建绑定已注册函数的任务。

**参数**:
- `name`: String - 任务名称
- `functionName`: String - 已注册的函数名称

**输出**: SXTask - 创建的任务实例

**异常**:
- `ObjectDisposedException`: 对象已释放
- `KeyNotFoundException`: 函数未注册

```csharp
linker.RegisterFunction("LoadFile", LoadFileAsync);
SXTask task = linker.CreateTask("MyLoadTask", "LoadFile");
task.SetParameter("path", @"C:\test.bin");
```

### GetTask(Guid id)

按ID获取任务。

**参数**:
- `id`: Guid - 任务ID

**输出**: SXTask? - 任务实例，不存在返回null

```csharp
SXTask? task = linker.GetTask(someGuid);
if (task is not null)
{
    Console.WriteLine($"找到任务: {task.Name}");
}
```

### GetTask(String name)

按名称获取任务。

**参数**:
- `name`: String - 任务名称

**输出**: SXTask? - 第一个匹配的任务，不存在返回null

```csharp
SXTask? task = linker.GetTask("LoadPE");
```

### RemoveTask(Guid id)

移除任务。

**参数**:
- `id`: Guid - 任务ID

**输出**: Boolean - 是否成功移除

```csharp
Boolean removed = linker.RemoveTask(taskId);
```

### GetAllTasks()

获取所有任务。

**输出**: `IEnumerable<SXTask>` - 任务列表副本

```csharp
foreach (SXTask task in linker.GetAllTasks())
{
    Console.WriteLine($"{task.Name}: {(task.IsCompleted ? "已完成" : "待执行")}");
}
```

### GetPendingTasks()

获取待执行任务。

**输出**: `IEnumerable<SXTask>` - 未完成且未在执行的任务列表

```csharp
IEnumerable<SXTask> pending = linker.GetPendingTasks();
Console.WriteLine($"待执行任务数: {pending.Count()}");
```

### CreateWorkflow(String name, params SXTask[] tasks)

从任务数组创建工作流。

**参数**:
- `name`: String - 工作流名称
- `tasks`: SXTask[] - 任务数组

**输出**: String - 工作流ID

```csharp
SXTask task1 = linker.CreateTask("Step1", SXType.Object, SXType.Report);
SXTask task2 = linker.CreateTask("Step2", SXType.Report, SXType.Report);

String workflowId = linker.CreateWorkflow("MyWorkflow", task1, task2);
```

### CreateWorkflow(String name, params String[] functionNames)

从函数名称创建工作流。

**参数**:
- `name`: String - 工作流名称
- `functionNames`: String[] - 函数名称数组

**输出**: String - 工作流ID

**内部流程**:
1. 生成工作流ID
2. 遍历函数名称
3. 为每个函数创建任务（命名格式: `{name}_{funcName}_{index}`）
4. 设置任务依赖链
5. 存入工作流字典

```csharp
linker.RegisterFunction("LoadPE", LoadPEAsync);
linker.RegisterFunction("Analyze", AnalyzeAsync);
linker.RegisterFunction("Report", ReportAsync);

String workflowId = linker.CreateWorkflow("Analysis", "LoadPE", "Analyze", "Report");
// 创建任务: Analysis_LoadPE_0, Analysis_Analyze_1, Analysis_Report_2
```

### ExecuteWorkflowAsync(String workflowId, CancellationToken cancellationToken = default)

执行工作流。

**参数**:
- `workflowId`: String - 工作流ID
- `cancellationToken`: CancellationToken - 取消令牌

**输出**: `Task<List<SXReport>>` - 所有任务的执行报告

**内部流程**:
1. 检查对象状态
2. 查找工作流
3. 触发WorkflowStarted事件
4. 遍历任务列表
5. 传递前一个任务的Output和Parameters到当前任务
6. 执行每个任务
7. 收集报告
8. 触发WorkflowCompleted事件

**异常**:
- `ObjectDisposedException`: 对象已释放
- `KeyNotFoundException`: 工作流不存在

```csharp
String workflowId = linker.CreateWorkflow("PEAnalysis", "LoadPE", "ExtractFeatures", "PredictMalware", "GenerateReport");

SXTask? loadTask = linker.GetTask("PEAnalysis_LoadPE_0");
loadTask?.SetParameter("filePath", @"C:\test.exe");

List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);

foreach (SXReport report in reports)
{
    Console.WriteLine($"{report.TaskName}: {(report.IsSuccess ? "成功" : "失败")}");
}
```

### ExecuteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)

按ID执行单个任务。

**参数**:
- `taskId`: Guid - 任务ID
- `cancellationToken`: CancellationToken - 取消令牌

**输出**: `Task<SXReport>` - 执行报告

**异常**:
- `ObjectDisposedException`: 对象已释放
- `KeyNotFoundException`: 任务不存在

```csharp
SXTask task = linker.CreateTask("Test", SXType.Object, SXType.Report);
SXReport report = await linker.ExecuteTaskAsync(task.ID.ID);
```

### ExecuteTaskAsync(String taskName, CancellationToken cancellationToken = default)

按名称执行单个任务。

**参数**:
- `taskName`: String - 任务名称
- `cancellationToken`: CancellationToken - 取消令牌

**输出**: `Task<SXReport>` - 执行报告

```csharp
SXReport report = await linker.ExecuteTaskAsync("MyTask");
```

### ExecuteAllPendingAsync(CancellationToken cancellationToken = default)

执行所有待执行任务。

**参数**:
- `cancellationToken`: CancellationToken - 取消令牌

**输出**: `Task<List<SXReport>>` - 所有执行报告

**内部流程**:
1. 获取所有待执行任务
2. 检查依赖是否满足
3. 顺序执行可执行的任务

```csharp
// 创建多个独立任务
for (Int32 i = 0; i < 5; i++)
{
    SXTask task = linker.CreateTask($"Task_{i}", SXType.Object, SXType.Report);
    task.SetParameter("index", i);
    task.ExecuteFunc = ExecuteTaskAsync;
}

List<SXReport> reports = await linker.ExecuteAllPendingAsync();
Console.WriteLine($"执行了 {reports.Count} 个任务");
```

### Clear()

清除所有任务和工作流。

```csharp
linker.Clear();
Console.WriteLine($"清除后任务数: {linker.TaskCount}"); // 0
```

### DisposeAsync()

异步释放资源。

**内部流程**:
1. 取消所有正在执行的任务
2. 清空任务字典
3. 清空工作流字典
4. 清空注册函数字典

```csharp
await using SXLinker linker = new();
// 使用完毕自动释放
```

## 使用示例

### 完整工作流示例

```csharp
await using SXLinker linker = new();

// 注册函数
linker.RegisterFunction("LoadFile", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    String? path = task.GetParameter<String>("path");
    
    if (!File.Exists(path))
    {
        report.SetFailure($"文件不存在: {path}");
        return report;
    }
    
    Byte[] bytes = await File.ReadAllBytesAsync(path, ct);
    task.Output = new SXData(bytes);
    report.AddResult("Size", bytes.Length);
    report.SetSuccess();
    return report;
});

linker.RegisterFunction("CalculateHash", (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    SXData? input = task.Input;
    
    if (input is null)
    {
        report.SetFailure("无输入数据");
        return Task.FromResult(report);
    }
    
    Byte[] data = input.GetData();
    using System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create();
    Byte[] hash = sha.ComputeHash(data);
    
    report.AddResult("SHA256", Convert.ToHexString(hash));
    report.SetSuccess();
    return Task.FromResult(report);
});

linker.RegisterFunction("SaveResult", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    String? hash = task.GetParameter<String>("SHA256");
    
    await File.WriteAllTextAsync("result.txt", hash ?? "N/A", ct);
    report.AddResult("Saved", true);
    report.SetSuccess();
    return report;
});

// 订阅事件
linker.TaskStarted += (s, t) => Console.WriteLine($"  开始: {t.Name}");
linker.TaskCompleted += (s, t) => Console.WriteLine($"  完成: {t.Task.Name} ({t.Report.Duration.TotalMilliseconds:F0}ms)");
linker.WorkflowStarted += (s, id) => Console.WriteLine($"工作流开始: {id}");
linker.WorkflowCompleted += (s, id) => Console.WriteLine($"工作流完成: {id}");

// 创建并执行工作流
String workflowId = linker.CreateWorkflow("FileHash", "LoadFile", "CalculateHash", "SaveResult");

SXTask? loadTask = linker.GetTask("FileHash_LoadFile_0");
loadTask?.SetParameter("path", @"C:\Windows\System32\notepad.exe");

List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);

// 输出结果
Console.WriteLine("\n=== 结果 ===");
foreach (SXReport report in reports)
{
    Console.WriteLine($"{report.TaskName}: {report.GetResult<String>("SHA256")?[..16]}...");
}
```

### 动态任务创建示例

```csharp
await using SXLinker linker = new();

// 注册基础函数
linker.RegisterFunction("Process", ProcessAsync);

// 动态创建任务网络
List<SXTask> tasks = [];
for (Int32 i = 0; i < 10; i++)
{
    SXTask task = linker.CreateTask($"Process_{i}", SXType.Object, SXType.Report);
    task.SetParameter("taskId", i);
    task.ExecuteFunc = ProcessAsync;
    tasks.Add(task);
}

// 设置依赖关系（偶数任务依赖前一个奇数任务）
for (Int32 i = 1; i < tasks.Count; i += 2)
{
    if (i + 1 < tasks.Count)
    {
        tasks[i + 1].AddDependency(tasks[i]);
    }
}

// 执行所有任务
List<SXReport> reports = await linker.ExecuteAllPendingAsync();
```

## 注意事项

- 使用 `await using` 或手动调用 `DisposeAsync()` 释放资源
- 工作流执行时自动传递参数和输出
- 任务命名格式: `{workflowName}_{functionName}_{index}`
- 事件在任务执行过程中触发，可用于日志和监控
- 取消令牌可中断工作流执行
