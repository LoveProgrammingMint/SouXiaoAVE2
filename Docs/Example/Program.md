# Example Program Class

**Namespace**: `Example`  
**File**: `Example/Program.cs`  
**Type**: Class

## Overview

SXAVELinker usage example program demonstrating core features such as workflow creation, task execution, and event subscription.

## Methods

### Main(String[] args)

Program entry point.

**Parameters**:
- `args`: String[] - Command line arguments (optional file path)

**Internal Flow**:
1. Determine test file path
2. Create SXLinker instance
3. Register custom functions
4. Create and execute workflow
5. Run custom task example
6. Run event subscription example

```csharp
// Default test file
dotnet run

// Specified test file
dotnet run "C:\test.exe"
```

### RegisterFunctions(SXLinker linker)

Register example functions.

**Parameters**:
- `linker`: SXLinker - Workflow engine instance

**Registered Functions**:

#### LoadFile

Load file into memory.

**Input Parameters**:
- `filePath`: String - File path

**Output**:
- `FileName`: String - File name
- `FileSize`: Int32 - File size
- `Extension`: String - Extension

**Task Output**: SXData - File bytes

#### ProcessData

Process data and calculate statistics.

**Input**: SXData - File bytes

**Output**:
- `ZeroRatio`: Double - Zero byte ratio
- `Entropy`: Double - Information entropy
- `DataSize`: Int32 - Data size

#### GenerateOutput

Generate output report.

**Output**:
- `OutputPath`: String - Output path
- `GeneratedAt`: String - Generation time

### RunCustomTaskExample(SXLinker linker)

Demonstrate custom task creation and execution.

**Internal Flow**:
1. Create custom task
2. Set parameters
3. Define execution function
4. Execute task
5. Get results

```csharp
// Output example:
// Custom task result: 84 (42 * 2)
```

### RunEventExample(SXLinker linker)

Demonstrate event subscription mechanism.

**Internal Flow**:
1. Subscribe to TaskAdded event
2. Subscribe to TaskCompleted event
3. Create and execute workflow
4. Display event trigger results

```csharp
// Output example:
//   Task added: EventTest_LoadFile_0
//   Task added: EventTest_ProcessData_1
//   Task added: EventTest_GenerateOutput_2
//   Task completed: EventTest_LoadFile_0 (15.23ms)
//   Task completed: EventTest_ProcessData_1 (5.12ms)
//   Task completed: EventTest_GenerateOutput_2 (0.45ms)
//   Total tasks: 3, Completed: 3
```

### CalculateEntropy(Byte[] data)

Calculate data entropy value.

**Parameters**:
- `data`: Byte[] - Input data

**Output**: Double - Entropy value

## Usage Examples

### Running the Example

```bash
cd Example
dotnet run
```

### Example Output

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

## Core Code Analysis

### Function Registration Pattern

```csharp
linker.RegisterFunction("LoadFile", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    
    // 1. Get parameters
    String? filePath = task.GetParameter<String>("filePath");
    
    // 2. Validate input
    if (String.IsNullOrEmpty(filePath) || !File.Exists(filePath))
    {
        report.SetFailure("Invalid file path");
        return report;
    }
    
    // 3. Execute logic
    Byte[] bytes = await File.ReadAllBytesAsync(filePath, ct);
    task.Output = new SXData(bytes);
    
    // 4. Set results
    report.AddResult("FileName", Path.GetFileName(filePath));
    report.AddResult("FileSize", bytes.Length);
    report.SetSuccess();
    
    return report;
});
```

### Workflow Creation Pattern

```csharp
// Create workflow (automatically creates tasks and dependencies)
String workflowId = linker.CreateWorkflow("ExampleAnalysis", "LoadFile", "ProcessData", "GenerateOutput");

// Set initial parameters
SXTask? loadTask = linker.GetTask("ExampleAnalysis_LoadFile_0");
loadTask?.SetParameter("filePath", testFile);

// Execute workflow
List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);
```

### Custom Task Pattern

```csharp
// Create task
SXTask customTask = linker.CreateTask("CustomTask", SXType.Object, SXType.Report);

// Set parameters
customTask.SetParameter("value", 42);

// Define execution function
customTask.ExecuteFunc = (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    Int32 value = task.GetParameter<Int32>("value");
    report.AddResult("DoubledValue", value * 2);
    report.SetSuccess();
    return Task.FromResult(report);
};

// Execute task
SXReport result = await linker.ExecuteTaskAsync(customTask.ID.ID);
```

### Event Subscription Pattern

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

// Events are automatically triggered after workflow execution
```

## Extended Examples

### Adding New Functions

```csharp
linker.RegisterFunction("HashFile", (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    SXData? input = task.Input;
    
    if (input is null)
    {
        report.SetFailure("No input data");
        return Task.FromResult(report);
    }
    
    Byte[] data = input.GetData();
    using var sha = System.Security.Cryptography.SHA256.Create();
    Byte[] hash = sha.ComputeHash(data);
    
    report.AddResult("SHA256", Convert.ToHexString(hash));
    report.SetSuccess();
    return Task.FromResult(report);
});

// Use new function
String wf = linker.CreateWorkflow("HashWorkflow", "LoadFile", "HashFile");
```

### Conditional Task Execution

```csharp
linker.RegisterFunction("ConditionalProcess", (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    Int64 fileSize = task.GetParameter<Int64>("FileSize");
    
    if (fileSize > 10 * 1024 * 1024) // 10MB
    {
        report.AddWarning("File is large, skipping deep analysis");
    }
    else
    {
        // Execute deep analysis
        report.AddResult("DeepAnalysis", true);
    }
    
    report.SetSuccess();
    return Task.FromResult(report);
});
```

## Notes

- Use `await using` to ensure resource disposal
- Task name format: `{WorkflowName}_{FunctionName}_{Index}`
- Parameters are set via `SetParameter` and retrieved via `GetParameter`
- Data is passed to the next task's `task.Input` via `task.Output`
- Events are triggered during task execution
