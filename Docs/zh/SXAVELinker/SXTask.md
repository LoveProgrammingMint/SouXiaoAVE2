# SXTask 类

**命名空间**: `SXAVELinker`  
**文件**: `SXAVELinker/SXTask.cs`  
**类型**: 密封类 (Sealed Class)

## 概述

可执行任务单元，是工作流的基本组成元素。支持依赖管理、参数传递、状态追踪和异步执行。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| ID | SXTaskID | 是 | 任务唯一标识 |
| Name | String | 是 | 任务名称 |
| InputType | SXType | 是 | 输入数据类型 |
| OutputType | SXType | 是 | 输出数据类型 |
| Input | SXData? | 否 | 输入数据 |
| Output | SXData? | 否 | 输出数据 |
| Parameters | `Dictionary<String, Object>` | 是 | 参数字典 |
| Dependencies | `List<SXTask>` | 是 | 依赖任务列表 |
| ExecuteFunc | `Func<SXTask, CancellationToken, Task<SXReport>>`? | 否 | 执行函数 |
| OnStart | `Action<SXTask>`? | 否 | 开始回调 |
| OnComplete | `Action<SXTask, SXReport>`? | 否 | 完成回调 |
| IsCompleted | Boolean | 是 | 是否已完成 |
| IsRunning | Boolean | 是 | 是否正在执行 |
| LastReport | SXReport? | 是 | 最后一次执行报告 |

## 构造函数

### SXTask(String name, SXType inputType, SXType outputType)

创建任务实例。

**参数**:
- `name`: String - 任务名称
- `inputType`: SXType - 输入数据类型
- `outputType`: SXType - 输出数据类型

**内部流程**:
1. 生成新的SXTaskID
2. 设置名称和类型
3. 初始化空参数字典和依赖列表

```csharp
SXTask task = new("LoadPE", SXType.FilePath, SXType.ByteArray);
```

## 方法

### SetParameter(String key, Object value)

设置参数。

**参数**:
- `key`: String - 参数名
- `value`: Object - 参数值

**内部流程**:
- 直接存入Parameters字典（覆盖同名键）

**输出**: 无返回值

```csharp
task.SetParameter("filePath", @"C:\test.exe");
task.SetParameter("maxSize", 10485760L);
task.SetParameter("enableDebug", true);
```

### `GetParameter<T>`(String key)

获取参数。

**参数**:
- `key`: String - 参数名

**输出**: `T?` - 类型化的参数值，不存在或类型不匹配返回 `default`

**内部流程**:
1. 尝试从Parameters字典获取值
2. 检查类型是否匹配
3. 匹配则返回，否则返回default

```csharp
String? filePath = task.GetParameter<String>("filePath");
Int64 maxSize = task.GetParameter<Int64>("maxSize");
Boolean enableDebug = task.GetParameter<Boolean>("enableDebug");
```

### AddDependency(SXTask task)

添加依赖任务。

**参数**:
- `task`: SXTask - 依赖的任务

**内部流程**:
1. 检查依赖是否已存在
2. 不存在则添加到Dependencies列表

**输出**: 无返回值

```csharp
SXTask loadTask = new("LoadPE", SXType.FilePath, SXType.ByteArray);
SXTask analyzeTask = new("Analyze", SXType.ByteArray, SXType.Report);
analyzeTask.AddDependency(loadTask);
```

### CanExecute()

检查是否可以执行。

**输出**: Boolean - 所有依赖是否已完成

**内部流程**:
- 检查Dependencies列表中所有任务的IsCompleted状态

```csharp
if (task.CanExecute())
{
    Console.WriteLine("任务可以执行");
}
else
{
    Console.WriteLine("存在未完成的依赖");
}
```

### ExecuteAsync(CancellationToken cancellationToken = default)

异步执行任务。

**参数**:
- `cancellationToken`: CancellationToken - 取消令牌

**输出**: `Task<SXReport>` - 执行报告

**内部流程**:
1. 检查是否已完成或正在执行（若是，返回LastReport）
2. 检查依赖是否完成（若否，返回失败报告）
3. 设置IsRunning = true
4. 触发OnStart回调
5. 创建新SXReport
6. 执行ExecuteFunc（若存在）
7. 设置IsCompleted = true
8. 捕获异常并设置失败报告
9. 设置IsRunning = false
10. 保存LastReport
11. 触发OnComplete回调
12. 返回报告

**异常**:
- 执行函数中的异常会被捕获并存入报告

```csharp
SXTask task = new("Test", SXType.Object, SXType.Report);
task.ExecuteFunc = async (t, ct) =>
{
    SXReport report = new(t.ID, t.Name);
    await Task.Delay(100, ct);
    report.AddResult("Status", "OK");
    report.SetSuccess();
    return report;
};

SXReport result = await task.ExecuteAsync();
Console.WriteLine($"成功: {result.IsSuccess}");
```

### Reset()

重置任务状态。

**内部流程**:
1. 设置IsCompleted = false
2. 设置IsRunning = false
3. 清除LastReport

```csharp
SXTask task = GetCompletedTask();
task.Reset();
Console.WriteLine($"可重新执行: {!task.IsCompleted}");
```

### ToString()

返回字符串表示。

**输出**: String - 格式 `Task[Name] ID8位`

```csharp
Console.WriteLine(task.ToString());
// 输出: Task[LoadPE] a1b2c3d4
```

## 使用示例

### 基本任务创建

```csharp
// 创建任务
SXTask task = new("CalculateHash", SXType.ByteArray, SXType.Report);

// 设置参数
task.SetParameter("algorithm", "SHA256");

// 设置执行函数
task.ExecuteFunc = (t, ct) =>
{
    SXReport report = new(t.ID, t.Name);
    
    String? algo = t.GetParameter<String>("algorithm");
    SXData? input = t.Input;
    
    if (input is null)
    {
        report.SetFailure("输入数据为空");
        return Task.FromResult(report);
    }
    
    Byte[] data = input.GetData();
    using System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create();
    Byte[] hash = sha.ComputeHash(data);
    
    report.AddResult("Hash", Convert.ToHexString(hash));
    report.SetSuccess();
    return Task.FromResult(report);
};

// 执行
SXReport result = await task.ExecuteAsync();
Console.WriteLine(result.GetResult<String>("Hash"));
```

### 带依赖的任务链

```csharp
// 创建任务链
SXTask loadTask = new("Load", SXType.FilePath, SXType.ByteArray);
SXTask processTask = new("Process", SXType.ByteArray, SXType.Report);
SXTask saveTask = new("Save", SXType.Report, SXType.Report);

// 设置依赖
processTask.AddDependency(loadTask);
saveTask.AddDependency(processTask);

// 配置执行函数
loadTask.ExecuteFunc = LoadFileAsync;
processTask.ExecuteFunc = ProcessDataAsync;
saveTask.ExecuteFunc = SaveResultAsync;

// 设置初始参数
loadTask.SetParameter("filePath", @"C:\data.bin");

// 按顺序执行
await loadTask.ExecuteAsync();
await processTask.ExecuteAsync(); // 自动获取loadTask.Output
await saveTask.ExecuteAsync();
```

### 使用回调

```csharp
SXTask task = new("LongRunning", SXType.Object, SXType.Report);

task.OnStart = t => Console.WriteLine($"任务开始: {t.Name}");
task.OnComplete = (t, r) => Console.WriteLine($"任务完成: {r.IsSuccess}, 耗时: {r.Duration.TotalMilliseconds}ms");

task.ExecuteFunc = async (t, ct) =>
{
    SXReport report = new(t.ID, t.Name);
    await Task.Delay(1000, ct);
    report.SetSuccess();
    return report;
};

await task.ExecuteAsync();
// 输出:
// 任务开始: LongRunning
// 任务完成: True, 耗时: 1002.35ms
```

## 注意事项

- 任务执行是幂等的，完成后重复执行返回缓存报告
- 依赖检查在执行前进行，未完成依赖返回失败报告
- 参数字典支持任意类型，但取值时需确保类型匹配
- Output数据会自动传递给下一个任务的Input
- 使用Reset()可重新执行已完成的任务
