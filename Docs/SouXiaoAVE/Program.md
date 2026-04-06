# Program Class

**Namespace**: `SouXiaoAVE`  
**File**: `SouXiaoAVE/Program.cs`  
**Type**: Class

## Overview

Application entry point, supporting two running modes: command-line analysis mode and service mode.

## Methods

### Main(String[] args)

Application main entry point.

**Parameters**:
- `args`: String[] - Command line arguments

**Internal Flow**:
1. Check if it's analysis mode (`--analyze` argument)
2. Analysis mode: Call `RunAnalysisAsync` to execute a single analysis
3. Service mode: Create and run Worker Service host

```csharp
// Analysis mode
dotnet run --analyze C:\test.exe

// Service mode
dotnet run
```

### RunAnalysisAsync(String filePath)

Execute a single PE file analysis.

**Parameters**:
- `filePath`: String - File path to analyze

**Internal Flow**:
1. Create WorkflowEngine instance
2. Execute analysis workflow
3. Output report for each task

**Returns**: Task - Async task

```csharp
// Command line output example:
// === SouXiaoAVE PE Analysis ===
// File: C:\test.exe
//
// === Analysis Results ===
// === Task Report: LoadPE ===
// ID: a1b2c3d4...
// Status: Success
// Duration: 15.23ms
// Results:
//   FileName: test.exe
//   FileSize: 245760
// ...
```

## Usage Examples

### Command Line Analysis

```bash
# Analyze a single file
dotnet run --analyze "C:\Windows\System32\notepad.exe"

# Analyze malware sample
dotnet run --analyze "C:\Samples\suspicious.exe"
```

### Service Mode

```bash
# Start TCP service (default port 9527)
dotnet run

# Run as Windows service
sc create SouXiaoAVE binPath="C:\path\to\SouXiaoAVE.exe"
```

### Programmatic Invocation

```csharp
// Directly call Main method (not recommended, use WorkflowEngine instead)
String[] args = ["--analyze", "C:\\test.exe"];
await Program.Main(args);
```

## Dependency Injection Configuration

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Register singleton services
builder.Services.AddSingleton<PeFeatureExtractor>();
builder.Services.AddSingleton<WorkflowEngine>();

// Register background service
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
await host.RunAsync();
```

## Running Mode Comparison

| Feature | Analysis Mode | Service Mode |
|---------|---------------|--------------|
| Startup argument | `--analyze <path>` | None |
| Execution method | Single analysis then exit | Continuous running |
| Output target | Console | TCP clients |
| Use case | Debugging, script invocation | Production environment |
| Concurrency support | None | Multiple clients |

## Notes

- Analysis mode is suitable for debugging and script integration
- Service mode uses .NET Worker Service framework
- Dependency injection ensures component lifecycle management
- Use `await using` to ensure resource disposal
