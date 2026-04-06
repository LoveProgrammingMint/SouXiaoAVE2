# SXTaskID Class

**Namespace**: `SXAVELinker`  
**File**: `SXAVELinker/SXTaskID.cs`  
**Type**: Sealed Class

## Overview

Unique task identifier containing GUID, source name, creation timestamp, and optional parent task ID. Used for tracking task execution chains and log correlation.

## Properties

| Property | Type | Read-only | Description |
|--------|------|------|------|
| ID | Guid | Yes | Unique identifier |
| CreatedAt | DateTime | Yes | Creation time (UTC) |
| Source | String | Yes | Source name |
| ParentID | String? | No | Parent task ID (nullable) |

## Constructors

### SXTaskID()

Parameterless constructor, sets source to "Unknown".

```csharp
SXTaskID id = new();
Console.WriteLine(id.Source); // "Unknown"
```

### SXTaskID(String source)

Specifies the source name.

**Parameters**:
- `source`: String - Source identifier

```csharp
SXTaskID id = new("LoadPE");
Console.WriteLine(id.Source); // "LoadPE"
```

### SXTaskID(String source, String? parentID)

Specifies source and parent task ID.

**Parameters**:
- `source`: String - Source identifier
- `parentID`: String? - Parent task ID

**Internal Flow**:
1. Generate new GUID
2. Record current UTC time
3. Set source and parent ID

```csharp
SXTaskID parentId = new("Workflow");
SXTaskID childId = new("SubTask", parentId.ID.ToString("N"));
```

## Methods

### ToString()

Returns short string representation.

**Output**: String - Format `[GUID] Source`

```csharp
SXTaskID id = new("Analyze");
Console.WriteLine(id.ToString());
// Output: [a1b2c3d4e5f6...] Analyze
```

### ToFullString()

Returns full string representation.

**Output**: String - Contains ID, source, creation time, and parent ID

```csharp
SXTaskID id = new("Test", "parent123");
Console.WriteLine(id.ToFullString());
// Output: ID: a1b2c3d4..., Source: Test, Created: 2026-01-01T12:00:00.0000000Z, Parent: parent123
```

### Parse(String str)

Parses SXTaskID from string.

**Input**:
- `str`: String - GUID string

**Output**: SXTaskID - Parsed task ID

**Exceptions**:
- `FormatException`: Unable to parse string

**Internal Flow**:
1. Attempt to parse string as GUID
2. If successful, create new SXTaskID with source set to "Parsed"

```csharp
String guidStr = "a1b2c3d4e5f67890a1b2c3d4e5f67890";
SXTaskID id = SXTaskID.Parse(guidStr);
Console.WriteLine(id.Source); // "Parsed"
```

### Equals(Object? obj)

Compares two SXTaskIDs for equality.

**Input**:
- `obj`: Object? - Object to compare

**Output**: Boolean - Whether equal (based on ID comparison)

```csharp
SXTaskID id1 = new("Test");
SXTaskID id2 = SXTaskID.Parse(id1.ID.ToString("N"));
Console.WriteLine(id1.Equals(id2)); // True
```

### GetHashCode()

Gets hash code.

**Output**: Int32 - Hash code based on ID

## Operators

### Equality Operator (==)

```csharp
SXTaskID id1 = new("Test");
SXTaskID id2 = SXTaskID.Parse(id1.ID.ToString("N"));
Console.WriteLine(id1 == id2); // True
```

### Inequality Operator (!=)

```csharp
SXTaskID id1 = new("Test");
SXTaskID id2 = new("Test");
Console.WriteLine(id1 != id2); // True (different GUIDs)
```

## Usage Examples

### Task Chain Tracking

```csharp
// Create main task ID
SXTaskID mainTaskId = new("MainWorkflow");

// Create subtask IDs
SXTaskID subTask1 = new("LoadFile", mainTaskId.ID.ToString("N"));
SXTaskID subTask2 = new("Analyze", mainTaskId.ID.ToString("N"));

Console.WriteLine($"Main task: {mainTaskId}");
Console.WriteLine($"Subtask 1: {subTask1.ToFullString()}");
Console.WriteLine($"Subtask 2: {subTask2.ToFullString()}");
Console.WriteLine($"Subtasks share parent ID: {subTask1.ParentID == subTask2.ParentID}");
```

### Log Correlation

```csharp
void LogTask(SXTaskID taskId, String message)
{
    Console.WriteLine($"[{taskId.ID:N8}] [{taskId.Source}] {message}");
}

SXTaskID id = new("FeatureExtraction");
LogTask(id, "Starting feature extraction");
LogTask(id, "Extraction complete, 512 dimensions");
```

## Notes

- GUID is automatically generated during construction, ensuring global uniqueness
- Creation time uses UTC to avoid timezone issues
- ParentID is optional, used for building task dependency trees
- Overrides Equals and GetHashCode, can be used as dictionary keys
