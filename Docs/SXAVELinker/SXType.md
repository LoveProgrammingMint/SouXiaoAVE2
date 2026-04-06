# SXType Enum

**Namespace**: `SXAVELinker`  
**File**: `SXAVELinker/SXType.cs`  
**Type**: Enum  
**Base Type**: `Byte`

## Overview

Defines the data type enumeration supported in the system, used to identify task input/output data types and object types.

## Enum Values

| Value | Name | Numeric | Description |
|---|------|------|------|
| Unknown | Unknown Type | 0 | Default value, indicates unrecognized data type |
| PEFile | PE File | 1 | Windows executable file format |
| ByteArray | Byte Array | 2 | Raw byte data |
| FilePath | File Path | 3 | File system path string |
| Directory | Directory | 4 | Folder path |
| Url | URL | 5 | URL address |
| FeatureVector | Feature Vector | 6 | Machine learning feature vector |
| Report | Report | 7 | Task execution report |
| Task | Task | 8 | Task object |
| TaskID | Task ID | 9 | Task identifier |
| Object | Object | 10 | Generic object container |

## Usage Examples

```csharp
// Specify input and output types when creating a task
SXTask task = new("AnalyzePE", SXType.PEFile, SXType.Report);

// Check data type
if (task.InputType == SXType.PEFile)
{
    Console.WriteLine("Task accepts PE file input");
}

// Create object container
SXObject obj = new(SXType.FeatureVector);
obj.Name = "features_512d";
```

## Notes

- Enum values use `Byte` as the underlying type to save memory
- `Unknown` serves as the default value for error detection
- Type checking should be performed before task execution
