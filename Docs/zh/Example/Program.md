# Example Program 类

**命名空间**: `Example`  
**文件**: `Example/Program.cs`  
**类型**: 类 (Class)

## 概述

SXAVELinker使用示例程序，演示工作流创建、任务执行、事件订阅等核心功能。

## 方法

### Main(String[] args)

程序入口。

**参数**:
- `args`: String[] - 命令行参数（可选文件路径）

**内部流程**:
1. 确定测试文件路径
2. 创建SXLinker实例
3. 注册自定义函数
4. 创建并执行工作流
5. 运行自定义任务示例
6. 运行事件订阅示例

```csharp
// 默认测试文件
dotnet run

// 指定测试文件
dotnet run "C:\test.exe"
```

### RegisterFunctions(SXLinker linker)

注册示例函数。

**参数**:
- `linker`: SXLinker - 工作流引擎实例

**注册的函数**:

#### LoadFile

加载文件到内存。

**输入参数**:
- `filePath`: String - 文件路径

**输出**:
- `FileName`: String - 文件名
- `FileSize`: Int32 - 文件大小
- `Extension`: String - 扩展名

**任务输出**: SXData - 文件字节

#### ProcessData

处理数据，计算统计信息。

**输入**: SXData - 文件字节

**输出**:
- `ZeroRatio`: Double - 零字节比例
- `Entropy`: Double - 信息熵
- `DataSize`: Int32 - 数据大小

#### GenerateOutput

生成输出报告。

**输出**:
- `OutputPath`: String - 输出路径
- `GeneratedAt`: String - 生成时间

### RunCustomTaskExample(SXLinker linker)

演示自定义任务创建和执行。

**内部流程**:
1. 创建自定义任务
2. 设置参数
3. 定义执行函数
4. 执行任务
5. 获取结果

```csharp
// 输出示例:
// Custom task result: 84 (42 * 2)
```

### RunEventExample(SXLinker linker)

演示事件订阅机制。

**内部流程**:
1. 订阅TaskAdded事件
2. 订阅TaskCompleted事件
3. 创建并执行工作流
4. 显示事件触发结果

```csharp
// 输出示例:
//   Task added: EventTest_LoadFile_0
//   Task added: EventTest_ProcessData_1
//   Task added: EventTest_GenerateOutput_2
//   Task completed: EventTest_LoadFile_0 (15.23ms)
//   Task completed: EventTest_ProcessData_1 (5.12ms)
//   Task completed: EventTest_GenerateOutput_2 (0.45ms)
//   Total tasks: 3, Completed: 3
```

### CalculateEntropy(Byte[] data)

计算数据熵值。

**参数**:
- `data`: Byte[] - 输入数据

**输出**: Double - 熵值

## 使用示例

### 运行示例

```bash
cd Example
dotnet run
```

### 输出示例

```
=== SXAVELinker Example ===

Test file: C:\Windows\System32\notepad.exe

Creating workflow...
Workflow ID: a1b2c3d4e5f6...

Executing workflow...
=== Results ===
=== Task Report: ExampleAnalysis_LoadFile_0 ===
ID: a1b2c3d4...
Status: Success
Duration: 12.34ms
Results:
  FileName: notepad.exe
  FileSize: 245760
  Extension: .exe

=== Task Report: ExampleAnalysis_ProcessData_1 ===
...

=== Task Report: ExampleAnalysis_GenerateOutput_2 ===
...

=== Custom Task Example ===
Custom task result: 84

=== Event Example ===
  Task added: EventTest_LoadFile_0
  Task added: EventTest_ProcessData_1
  Task added: EventTest_GenerateOutput_2
  Task completed: EventTest_LoadFile_0 (10.23ms)
  Task completed: EventTest_ProcessData_1 (3.45ms)
  Task completed: EventTest_GenerateOutput_2 (0.12ms)
  Total tasks: 3, Completed: 3

Example completed!
```

## 核心代码解析

### 函数注册模式

```csharp
linker.RegisterFunction("LoadFile", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    
    // 1. 获取参数
    String? filePath = task.GetParameter<String>("filePath");
    
    // 2. 验证输入
    if (String.IsNullOrEmpty(filePath) || !File.Exists(filePath))
    {
        report.SetFailure("文件路径无效");
        return report;
    }
    
    // 3. 执行逻辑
    Byte[] bytes = await File.ReadAllBytesAsync(filePath, ct);
    task.Output = new SXData(bytes);
    
    // 4. 设置结果
    report.AddResult("FileName", Path.GetFileName(filePath));
    report.AddResult("FileSize", bytes.Length);
    report.SetSuccess();
    
    return report;
});
```

### 工作流创建模式

```csharp
// 创建工作流（自动创建任务和依赖）
String workflowId = linker.CreateWorkflow("ExampleAnalysis", "LoadFile", "ProcessData", "GenerateOutput");

// 设置初始参数
SXTask? loadTask = linker.GetTask("ExampleAnalysis_LoadFile_0");
loadTask?.SetParameter("filePath", testFile);

// 执行工作流
List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);
```

### 自定义任务模式

```csharp
// 创建任务
SXTask customTask = linker.CreateTask("CustomTask", SXType.Object, SXType.Report);

// 设置参数
customTask.SetParameter("value", 42);

// 定义执行函数
customTask.ExecuteFunc = (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    Int32 value = task.GetParameter<Int32>("value");
    report.AddResult("DoubledValue", value * 2);
    report.SetSuccess();
    return Task.FromResult(report);
};

// 执行任务
SXReport result = await linker.ExecuteTaskAsync(customTask.ID.ID);
```

### 事件订阅模式

```csharp
Int32 taskCount = 0;
Int32 completedCount = 0;

linker.TaskAdded += (sender, task) =>
{
    taskCount++;
    Console.WriteLine($"  Task added: {task.Name}");
};

linker.TaskCompleted += (sender, tuple) =>
{
    completedCount++;
    Console.WriteLine($"  Task completed: {tuple.Task.Name} ({tuple.Report.Duration.TotalMilliseconds:F2}ms)");
};

// 执行工作流后事件自动触发
```

## 扩展示例

### 添加新函数

```csharp
linker.RegisterFunction("HashFile", (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    SXData? input = task.Input;
    
    if (input is null)
    {
        report.SetFailure("无输入数据");
        return Task.FromResult(report);
    }
    
    Byte[] data = input.GetData();
    using var sha = System.Security.Cryptography.SHA256.Create();
    Byte[] hash = sha.ComputeHash(data);
    
    report.AddResult("SHA256", Convert.ToHexString(hash));
    report.SetSuccess();
    return Task.FromResult(report);
});

// 使用新函数
String wf = linker.CreateWorkflow("HashWorkflow", "LoadFile", "HashFile");
```

### 条件任务执行

```csharp
linker.RegisterFunction("ConditionalProcess", (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    Int64 fileSize = task.GetParameter<Int64>("FileSize");
    
    if (fileSize > 10 * 1024 * 1024) // 10MB
    {
        report.AddWarning("文件较大，跳过深度分析");
    }
    else
    {
        // 执行深度分析
        report.AddResult("DeepAnalysis", true);
    }
    
    report.SetSuccess();
    return Task.FromResult(report);
});
```

## 注意事项

- 使用 `await using` 确保资源释放
- 任务名称格式: `{WorkflowName}_{FunctionName}_{Index}`
- 参数通过 `SetParameter` 设置，通过 `GetParameter` 获取
- 数据通过 `task.Output` 传递到下一个任务的 `task.Input`
- 事件在任务执行过程中触发
