# SXLinker Class

**Namespace**: `SXAVELinker`  
**File**: `SXAVELinker/SXLinker.cs`  
**Type**: Sealed Class  
**Implements**: `IAsyncDisposable`

## Overview

Core workflow engine responsible for function registration, task creation, workflow management, and execution scheduling. This is the main entry point for the SXAVELinker library.

## Properties

| Property | Type | Read-only | Description |
|--------|------|------|------|
| TaskCount | Int32 | Yes | Number of created tasks |
| WorkflowCount | Int32 | Yes | Number of created workflows |
| RegisteredFunctionCount | Int32 | Yes | Number of registered functions |

## Events

| Event | Type | Description |
|--------|------|------|
| TaskAdded | `EventHandler<SXTask>` | Triggered when a task is added |
| TaskStarted | `EventHandler<SXTask>` | Triggered when a task starts execution |
| TaskCompleted | `EventHandler<(SXTask, SXReport)>` | Triggered when a task completes successfully |
| TaskFailed | `EventHandler<(SXTask, Exception)>` | Triggered when a task fails |
| WorkflowStarted | `EventHandler<String>` | Triggered when a workflow starts |
| WorkflowCompleted | `EventHandler<String>` | Triggered when a workflow completes |

## Constructors

### SXLinker()

Creates a workflow engine instance.

**Internal Flow**:
1. Initialize task dictionary
2. Initialize registered function dictionary
3. Initialize workflow dictionary
4. Create thread lock object
5. Create cancellation token source

```csharp
SXLinker linker = new();
Console.WriteLine($"Initial function count: {linker.RegisteredFunctionCount}"); // 0
```

## Methods

### RegisterFunction(String name, `Func<SXTask, CancellationToken, Task<SXReport>>` func)

Registers an asynchronous task function.

**Parameters**:
- `name`: String - Function name
- `func`: `Func<SXTask, CancellationToken, Task<SXReport>>` - Async execution function

**Internal Flow**:
1. Check if object has been disposed
2. Store in registered function dictionary

**Output**: No return value

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed

```csharp
linker.RegisterFunction("LoadFile", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    String? path = task.GetParameter<String>("path");
    
    if (String.IsNullOrEmpty(path))
    {
        report.SetFailure("Path cannot be empty");
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

Registers a synchronous task function.

**Parameters**:
- `name`: String - Function name
- `action`: `Action<SXTask>` - Synchronous execution action

**Internal Flow**:
1. Wrap synchronous action as async function
2. Create success report

```csharp
linker.RegisterFunction("Log", task =>
{
    String? message = task.GetParameter<String>("message");
    Console.WriteLine($"[LOG] {message}");
});
```

### `RegisterFunction<TInput, TOutput>`(String name, `Func<TInput, CancellationToken, Task<TOutput>>` func)

Registers a generic conversion function.

**Parameters**:
- `name`: String - Function name
- `func`: `Func<TInput, CancellationToken, Task<TOutput>>` - Conversion function

**Internal Flow**:
1. Get input from task parameters
2. Call conversion function
3. Store output in report results

```csharp
linker.RegisterFunction<String, Int32>("GetLength", async (path, ct) =>
{
    Byte[] bytes = await File.ReadAllBytesAsync(path, ct);
    return bytes.Length;
});
```

### UnregisterFunction(String name)

Unregisters a function.

**Parameters**:
- `name`: String - Function name

**Output**: Boolean - Whether successfully unregistered

```csharp
Boolean removed = linker.UnregisterFunction("OldFunction");
```

### CreateTask(String name, SXType inputType, SXType outputType)

Creates a custom task.

**Parameters**:
- `name`: String - Task name
- `inputType`: SXType - Input type
- `outputType`: SXType - Output type

**Output**: SXTask - Created task instance

**Internal Flow**:
1. Check object state
2. Create SXTask instance
3. Bind start and completion callbacks
4. Store in task dictionary
5. Trigger TaskAdded event

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed

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

Creates a task bound to a registered function.

**Parameters**:
- `name`: String - Task name
- `functionName`: String - Registered function name

**Output**: SXTask - Created task instance

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed
- `KeyNotFoundException`: Function not registered

```csharp
linker.RegisterFunction("LoadFile", LoadFileAsync);
SXTask task = linker.CreateTask("MyLoadTask", "LoadFile");
task.SetParameter("path", @"C:\test.bin");
```

### GetTask(Guid id)

Gets a task by ID.

**Parameters**:
- `id`: Guid - Task ID

**Output**: SXTask? - Task instance, returns null if not exists

```csharp
SXTask? task = linker.GetTask(someGuid);
if (task is not null)
{
    Console.WriteLine($"Found task: {task.Name}");
}
```

### GetTask(String name)

Gets a task by name.

**Parameters**:
- `name`: String - Task name

**Output**: SXTask? - First matching task, returns null if not exists

```csharp
SXTask? task = linker.GetTask("LoadPE");
```

### RemoveTask(Guid id)

Removes a task.

**Parameters**:
- `id`: Guid - Task ID

**Output**: Boolean - Whether successfully removed

```csharp
Boolean removed = linker.RemoveTask(taskId);
```

### GetAllTasks()

Gets all tasks.

**Output**: `IEnumerable<SXTask>` - Copy of task list

```csharp
foreach (SXTask task in linker.GetAllTasks())
{
    Console.WriteLine($"{task.Name}: {(task.IsCompleted ? "Completed" : "Pending")}");
}
```

### GetPendingTasks()

Gets pending tasks.

**Output**: `IEnumerable<SXTask>` - List of incomplete and non-executing tasks

```csharp
IEnumerable<SXTask> pending = linker.GetPendingTasks();
Console.WriteLine($"Pending task count: {pending.Count()}");
```

### CreateWorkflow(String name, params SXTask[] tasks)

Creates a workflow from task array.

**Parameters**:
- `name`: String - Workflow name
- `tasks`: SXTask[] - Task array

**Output**: String - Workflow ID

```csharp
SXTask task1 = linker.CreateTask("Step1", SXType.Object, SXType.Report);
SXTask task2 = linker.CreateTask("Step2", SXType.Report, SXType.Report);

String workflowId = linker.CreateWorkflow("MyWorkflow", task1, task2);
```

### CreateWorkflow(String name, params String[] functionNames)

Creates a workflow from function names.

**Parameters**:
- `name`: String - Workflow name
- `functionNames`: String[] - Function name array

**Output**: String - Workflow ID

**Internal Flow**:
1. Generate workflow ID
2. Iterate through function names
3. Create task for each function (naming format: `{name}_{funcName}_{index}`)
4. Set task dependency chain
5. Store in workflow dictionary

```csharp
linker.RegisterFunction("LoadPE", LoadPEAsync);
linker.RegisterFunction("Analyze", AnalyzeAsync);
linker.RegisterFunction("Report", ReportAsync);

String workflowId = linker.CreateWorkflow("Analysis", "LoadPE", "Analyze", "Report");
// Creates tasks: Analysis_LoadPE_0, Analysis_Analyze_1, Analysis_Report_2
```

### ExecuteWorkflowAsync(String workflowId, CancellationToken cancellationToken = default)

Executes a workflow.

**Parameters**:
- `workflowId`: String - Workflow ID
- `cancellationToken`: CancellationToken - Cancellation token

**Output**: `Task<List<SXReport>>` - Execution reports of all tasks

**Internal Flow**:
1. Check object state
2. Find workflow
3. Trigger WorkflowStarted event
4. Iterate through task list
5. Pass previous task's Output and Parameters to current task
6. Execute each task
7. Collect reports
8. Trigger WorkflowCompleted event

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed
- `KeyNotFoundException`: Workflow does not exist

```csharp
String workflowId = linker.CreateWorkflow("PEAnalysis", "LoadPE", "ExtractFeatures", "PredictMalware", "GenerateReport");

SXTask? loadTask = linker.GetTask("PEAnalysis_LoadPE_0");
loadTask?.SetParameter("filePath", @"C:\test.exe");

List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);

foreach (SXReport report in reports)
{
    Console.WriteLine($"{report.TaskName}: {(report.IsSuccess ? "Success" : "Failed")}");
}
```

### ExecuteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)

Executes a single task by ID.

**Parameters**:
- `taskId`: Guid - Task ID
- `cancellationToken`: CancellationToken - Cancellation token

**Output**: `Task<SXReport>` - Execution report

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed
- `KeyNotFoundException`: Task does not exist

```csharp
SXTask task = linker.CreateTask("Test", SXType.Object, SXType.Report);
SXReport report = await linker.ExecuteTaskAsync(task.ID.ID);
```

### ExecuteTaskAsync(String taskName, CancellationToken cancellationToken = default)

Executes a single task by name.

**Parameters**:
- `taskName`: String - Task name
- `cancellationToken`: CancellationToken - Cancellation token

**Output**: `Task<SXReport>` - Execution report

```csharp
SXReport report = await linker.ExecuteTaskAsync("MyTask");
```

### ExecuteAllPendingAsync(CancellationToken cancellationToken = default)

Executes all pending tasks.

**Parameters**:
- `cancellationToken`: CancellationToken - Cancellation token

**Output**: `Task<List<SXReport>>` - All execution reports

**Internal Flow**:
1. Get all pending tasks
2. Check if dependencies are satisfied
3. Execute executable tasks sequentially

```csharp
// Create multiple independent tasks
for (Int32 i = 0; i < 5; i++)
{
    SXTask task = linker.CreateTask($"Task_{i}", SXType.Object, SXType.Report);
    task.SetParameter("index", i);
    task.ExecuteFunc = ExecuteTaskAsync;
}

List<SXReport> reports = await linker.ExecuteAllPendingAsync();
Console.WriteLine($"Executed {reports.Count} tasks");
```

### Clear()

Clears all tasks and workflows.

```csharp
linker.Clear();
Console.WriteLine($"Task count after clearing: {linker.TaskCount}"); // 0
```

### DisposeAsync()

Asynchronously releases resources.

**Internal Flow**:
1. Cancel all executing tasks
2. Clear task dictionary
3. Clear workflow dictionary
4. Clear registered function dictionary

```csharp
await using SXLinker linker = new();
// Automatically released after use
```

## Usage Examples

### Complete Workflow Example

```csharp
await using SXLinker linker = new();

// Register functions
linker.RegisterFunction("LoadFile", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    String? path = task.GetParameter<String>("path");
    
    if (!File.Exists(path))
    {
        report.SetFailure($"File does not exist: {path}");
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
        report.SetFailure("No input data");
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

// Subscribe to events
linker.TaskStarted += (s, t) => Console.WriteLine($"  Started: {t.Name}");
linker.TaskCompleted += (s, t) => Console.WriteLine($"  Completed: {t.Task.Name} ({t.Report.Duration.TotalMilliseconds:F0}ms)");
linker.WorkflowStarted += (s, id) => Console.WriteLine($"Workflow started: {id}");
linker.WorkflowCompleted += (s, id) => Console.WriteLine($"Workflow completed: {id}");

// Create and execute workflow
String workflowId = linker.CreateWorkflow("FileHash", "LoadFile", "CalculateHash", "SaveResult");

SXTask? loadTask = linker.GetTask("FileHash_LoadFile_0");
loadTask?.SetParameter("path", @"C:\Windows\System32\notepad.exe");

List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);

// Output results
Console.WriteLine("\n=== Results ===");
foreach (SXReport report in reports)
{
    Console.WriteLine($"{report.TaskName}: {report.GetResult<String>("SHA256")?[..16]}...");
}
```

### Dynamic Task Creation Example

```csharp
await using SXLinker linker = new();

// Register base function
linker.RegisterFunction("Process", ProcessAsync);

// Dynamically create task network
List<SXTask> tasks = [];
for (Int32 i = 0; i < 10; i++)
{
    SXTask task = linker.CreateTask($"Process_{i}", SXType.Object, SXType.Report);
    task.SetParameter("taskId", i);
    task.ExecuteFunc = ProcessAsync;
    tasks.Add(task);
}

// Set dependency relationships (even tasks depend on previous odd task)
for (Int32 i = 1; i < tasks.Count; i += 2)
{
    if (i + 1 < tasks.Count)
    {
        tasks[i + 1].AddDependency(tasks[i]);
    }
}

// Execute all tasks
List<SXReport> reports = await linker.ExecuteAllPendingAsync();
```

## Notes

- Use `await using` or manually call `DisposeAsync()` to release resources
- Workflow execution automatically passes parameters and outputs
- Task naming format: `{workflowName}_{functionName}_{index}`
- Events are triggered during task execution, can be used for logging and monitoring
- Cancellation token can interrupt workflow execution
