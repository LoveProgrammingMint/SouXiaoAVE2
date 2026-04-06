# SXTask Class

**Namespace**: `SXAVELinker`  
**File**: `SXAVELinker/SXTask.cs`  
**Type**: Sealed Class

## Overview

Executable task unit, the basic building block of workflows. Supports dependency management, parameter passing, state tracking, and asynchronous execution.

## Properties

| Property | Type | Read-only | Description |
|--------|------|------|------|
| ID | SXTaskID | Yes | Task unique identifier |
| Name | String | Yes | Task name |
| InputType | SXType | Yes | Input data type |
| OutputType | SXType | Yes | Output data type |
| Input | SXData? | No | Input data |
| Output | SXData? | No | Output data |
| Parameters | `Dictionary<String, Object>` | Yes | Parameter dictionary |
| Dependencies | `List<SXTask>` | Yes | Dependency task list |
| ExecuteFunc | `Func<SXTask, CancellationToken, Task<SXReport>>`? | No | Execution function |
| OnStart | `Action<SXTask>`? | No | Start callback |
| OnComplete | `Action<SXTask, SXReport>`? | No | Completion callback |
| IsCompleted | Boolean | Yes | Whether completed |
| IsRunning | Boolean | Yes | Whether currently executing |
| LastReport | SXReport? | Yes | Last execution report |

## Constructors

### SXTask(String name, SXType inputType, SXType outputType)

Creates a task instance.

**Parameters**:
- `name`: String - Task name
- `inputType`: SXType - Input data type
- `outputType`: SXType - Output data type

**Internal Flow**:
1. Generate new SXTaskID
2. Set name and types
3. Initialize empty parameter dictionary and dependency list

```csharp
SXTask task = new("LoadPE", SXType.FilePath, SXType.ByteArray);
```

## Methods

### SetParameter(String key, Object value)

Sets a parameter.

**Parameters**:
- `key`: String - Parameter name
- `value`: Object - Parameter value

**Internal Flow**:
- Store directly in Parameters dictionary (overwrites same key)

**Output**: No return value

```csharp
task.SetParameter("filePath", @"C:\test.exe");
task.SetParameter("maxSize", 10485760L);
task.SetParameter("enableDebug", true);
```

### `GetParameter<T>`(String key)

Gets a parameter.

**Parameters**:
- `key`: String - Parameter name

**Returns**: `T?` - Typed parameter value, returns default if not exists or type mismatch

**Internal Flow**:
1. Try to get value from Parameters dictionary
2. Check if type matches
3. Return if matched, otherwise return default

```csharp
String? filePath = task.GetParameter<String>("filePath");
Int64 maxSize = task.GetParameter<Int64>("maxSize");
Boolean enableDebug = task.GetParameter<Boolean>("enableDebug");
```

### AddDependency(SXTask task)

Adds a dependency task.

**Parameters**:
- `task`: SXTask - Dependency task

**Internal Flow**:
1. Check if dependency already exists
2. If not, add to Dependencies list

**Output**: No return value

```csharp
SXTask loadTask = new("LoadPE", SXType.FilePath, SXType.ByteArray);
SXTask analyzeTask = new("Analyze", SXType.ByteArray, SXType.Report);
analyzeTask.AddDependency(loadTask);
```

### CanExecute()

Checks if the task can be executed.

**Output**: Boolean - Whether all dependencies are completed

**Internal Flow**:
- Check IsCompleted status of all tasks in Dependencies list

```csharp
if (task.CanExecute())
{
    Console.WriteLine("Task can be executed");
}
else
{
    Console.WriteLine("There are incomplete dependencies");
}
```

### ExecuteAsync(CancellationToken cancellationToken = default)

Asynchronously executes the task.

**Parameters**:
- `cancellationToken`: CancellationToken - Cancellation token

**Output**: `Task<SXReport>` - Execution report

**Internal Flow**:
1. Check if already completed or running (if so, return LastReport)
2. Check if dependencies are complete (if not, return failure report)
3. Set IsRunning = true
4. Trigger OnStart callback
5. Create new SXReport
6. Execute ExecuteFunc (if exists)
7. Set IsCompleted = true
8. Capture exceptions and set failure report
9. Set IsRunning = false
10. Save LastReport
11. Trigger OnComplete callback
12. Return report

**Exceptions**:
- Exceptions in execution function will be captured and stored in report

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
Console.WriteLine($"Success: {result.IsSuccess}");
```

### Reset()

Resets task state.

**Internal Flow**:
1. Set IsCompleted = false
2. Set IsRunning = false
3. Clear LastReport

```csharp
SXTask task = GetCompletedTask();
task.Reset();
Console.WriteLine($"Can re-execute: {!task.IsCompleted}");
```

### ToString()

Returns string representation.

**Output**: String - Format `Task[Name] ID8chars`

```csharp
Console.WriteLine(task.ToString());
// Output: Task[LoadPE] a1b2c3d4
```

## Usage Examples

### Basic Task Creation

```csharp
// Create task
SXTask task = new("CalculateHash", SXType.ByteArray, SXType.Report);

// Set parameters
task.SetParameter("algorithm", "SHA256");

// Set execution function
task.ExecuteFunc = (t, ct) =>
{
    SXReport report = new(t.ID, t.Name);
    
    String? algo = t.GetParameter<String>("algorithm");
    SXData? input = t.Input;
    
    if (input is null)
    {
        report.SetFailure("Input data is empty");
        return Task.FromResult(report);
    }
    
    Byte[] data = input.GetData();
    using System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create();
    Byte[] hash = sha.ComputeHash(data);
    
    report.AddResult("Hash", Convert.ToHexString(hash));
    report.SetSuccess();
    return Task.FromResult(report);
};

// Execute
SXReport result = await task.ExecuteAsync();
Console.WriteLine(result.GetResult<String>("Hash"));
```

### Task Chain with Dependencies

```csharp
// Create task chain
SXTask loadTask = new("Load", SXType.FilePath, SXType.ByteArray);
SXTask processTask = new("Process", SXType.ByteArray, SXType.Report);
SXTask saveTask = new("Save", SXType.Report, SXType.Report);

// Set dependencies
processTask.AddDependency(loadTask);
saveTask.AddDependency(processTask);

// Configure execution functions
loadTask.ExecuteFunc = LoadFileAsync;
processTask.ExecuteFunc = ProcessDataAsync;
saveTask.ExecuteFunc = SaveResultAsync;

// Set initial parameters
loadTask.SetParameter("filePath", @"C:\data.bin");

// Execute in order
await loadTask.ExecuteAsync();
await processTask.ExecuteAsync(); // Automatically gets loadTask.Output
await saveTask.ExecuteAsync();
```

### Using Callbacks

```csharp
SXTask task = new("LongRunning", SXType.Object, SXType.Report);

task.OnStart = t => Console.WriteLine($"Task started: {t.Name}");
task.OnComplete = (t, r) => Console.WriteLine($"Task completed: {r.IsSuccess}, Duration: {r.Duration.TotalMilliseconds}ms");

task.ExecuteFunc = async (t, ct) =>
{
    SXReport report = new(t.ID, t.Name);
    await Task.Delay(1000, ct);
    report.SetSuccess();
    return report;
};

await task.ExecuteAsync();
// Output:
// Task started: LongRunning
// Task completed: True, Duration: 1002.35ms
```

## Notes

- Task execution is idempotent, repeated execution after completion returns cached report
- Dependency check is performed before execution, incomplete dependencies return failure report
- Parameter dictionary supports any type, but ensure type matching when retrieving values
- Output data is automatically passed to the next task's Input
- Use Reset() to re-execute a completed task
