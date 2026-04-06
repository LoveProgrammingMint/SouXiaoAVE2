# SXObject Class

**Namespace**: `SXAVELinker`  
**File**: `SXAVELinker/SXObject.cs`  
**Type**: Sealed Class

## Overview

Hierarchical object container that supports tree structure organization, metadata storage, and data binding. Used for building complex analysis result object trees.

## Properties

| Property | Type | Read-only | Description |
|--------|------|------|------|
| ID | Guid | Yes | Object unique identifier |
| Type | SXType | Yes | Object type |
| Name | String? | No | Object name |
| Data | SXData? | No | Associated data |
| Metadata | `Dictionary<String, Object>` | Yes | Metadata dictionary |
| Children | `Dictionary<String, SXObject>` | Yes | Child object dictionary |
| Parent | SXObject? | Yes | Parent object reference |
| CreatedAt | DateTime | Yes | Creation time (UTC) |
| ModifiedAt | DateTime? | Yes | Last modified time (UTC) |

## Constructors

### SXObject(SXType type)

Creates an object of specified type.

**Parameters**:
- `type`: SXType - Object type

```csharp
SXObject obj = new(SXType.Report);
```

### SXObject(SXType type, String name)

Creates an object of specified type and name.

**Parameters**:
- `type`: SXType - Object type
- `name`: String - Object name

```csharp
SXObject obj = new(SXType.Report, "AnalysisResult");
```

## Methods

### SetMetadata(String key, Object value)

Sets metadata.

**Parameters**:
- `key`: String - Metadata key
- `value`: Object - Metadata value

**Internal Flow**:
1. Store in Metadata dictionary
2. Update ModifiedAt timestamp

**Output**: No return value

```csharp
obj.SetMetadata("Author", "SouXiaoAVE");
obj.SetMetadata("Version", "1.0.0");
obj.SetMetadata("Confidence", 0.95);
```

### `GetMetadata<T>`(String key)

Gets metadata.

**Parameters**:
- `key`: String - Metadata key

**Returns**: `T?` - Typed metadata value, returns default if not exists or type mismatch

```csharp
String? author = obj.GetMetadata<String>("Author");
Double confidence = obj.GetMetadata<Double>("Confidence");
```

### AddChild(String name, SXObject child)

Adds a child object.

**Parameters**:
- `name`: String - Child object name (key)
- `child`: SXObject - Child object instance

**Internal Flow**:
1. Set child's Parent to current object
2. Add to Children dictionary
3. Update ModifiedAt timestamp

**Output**: No return value

```csharp
SXObject root = new(SXType.Object, "Root");
SXObject child1 = new(SXType.Report, "Report1");
SXObject child2 = new(SXType.Report, "Report2");

root.AddChild("first", child1);
root.AddChild("second", child2);
```

### GetChild(String name)

Gets a child object.

**Parameters**:
- `name`: String - Child object name

**Output**: SXObject? - Child object, returns null if not exists

```csharp
SXObject? child = root.GetChild("first");
if (child is not null)
{
    Console.WriteLine($"Found child object: {child.Name}");
}
```

### RemoveChild(String name)

Removes a child object.

**Parameters**:
- `name`: String - Child object name

**Internal Flow**:
1. Remove from Children dictionary
2. Clear child's Parent reference
3. Update ModifiedAt timestamp

**Output**: Boolean - Whether successfully removed

```csharp
Boolean removed = root.RemoveChild("first");
Console.WriteLine($"Removal successful: {removed}");
```

### GetAllChildren()

Recursively gets all descendant objects.

**Output**: `IEnumerable<SXObject>` - Enumeration of all descendant objects

**Internal Flow**:
1. Iterate through direct children
2. Recursively get each child's descendants
3. Use yield return for lazy evaluation

```csharp
SXObject root = new(SXType.Object, "Root");
SXObject child = new(SXType.Report, "Child");
SXObject grandchild = new(SXType.Report, "Grandchild");

child.AddChild("nested", grandchild);
root.AddChild("sub", child);

Int32 count = root.GetAllChildren().Count();
Console.WriteLine($"Total descendants: {count}"); // 2
```

### GetPath()

Gets the object path.

**Output**: String - Path from root to current object, format `parent/child/name`

**Internal Flow**:
1. Start from current object
2. Traverse up the Parent chain
3. Collect all names
4. Join with "/"

```csharp
SXObject root = new(SXType.Object, "Root");
SXObject child = new(SXType.Report, "Child");
SXObject grandchild = new(SXType.Report, "Grandchild");

root.AddChild("sub", child);
child.AddChild("nested", grandchild);

Console.WriteLine(grandchild.GetPath());
// Output: Root/sub/nested
```

### ToString()

Returns string representation.

**Output**: String - Format `[Type] Name`

```csharp
Console.WriteLine(obj.ToString());
// Output: [Report] AnalysisResult
```

## Usage Examples

### Building Analysis Result Tree

```csharp
// Create root object
SXObject analysisRoot = new(SXType.Object, "PEAnalysis");

// Set metadata
analysisRoot.SetMetadata("TargetFile", "sample.exe");
analysisRoot.SetMetadata("AnalysisTime", DateTime.UtcNow.ToString("O"));

// Create feature child object
SXObject features = new(SXType.FeatureVector, "Features");
features.SetMetadata("Dimensions", 512);
features.SetMetadata("Sparsity", 0.66);
analysisRoot.AddChild("features", features);

// Create prediction child object
SXObject prediction = new(SXType.Report, "Prediction");
prediction.SetMetadata("Label", "Benign");
prediction.SetMetadata("Score", -7.70);
prediction.SetMetadata("Probability", 0.0005);
analysisRoot.AddChild("prediction", prediction);

// Create section table child object
SXObject sections = new(SXType.Object, "Sections");
sections.SetMetadata("Count", 5);
analysisRoot.AddChild("sections", sections);

// Add information for each section
String[] sectionNames = [".text", ".data", ".rdata", ".rsrc", ".reloc"];
for (Int32 i = 0; i < sectionNames.Length; i++)
{
    SXObject section = new(SXType.Object, $"Section_{i}");
    section.SetMetadata("Name", sectionNames[i]);
    section.SetMetadata("Entropy", 7.2 + i * 0.1);
    sections.AddChild($"section_{i}", section);
}

// Traverse tree structure
Console.WriteLine("=== Analysis Result Tree ===");
PrintTree(analysisRoot, 0);

void PrintTree(SXObject obj, Int32 level)
{
    String indent = new String(' ', level * 2);
    Console.WriteLine($"{indent}[{obj.Type}] {obj.Name ?? obj.ID.ToString("N8")}");
    
    foreach (KeyValuePair<String, SXObject> kvp in obj.Children)
    {
        PrintTree(kvp.Value, level + 1);
    }
}
```

### Finding Specific Objects

```csharp
// Find all report type objects
IEnumerable<SXObject> reports = analysisRoot.GetAllChildren()
    .Where(o => o.Type == SXType.Report);

foreach (SXObject report in reports)
{
    Console.WriteLine($"Report: {report.Name}");
    Console.WriteLine($"  Label: {report.GetMetadata<String>("Label")}");
}

// Find by path
SXObject? prediction = analysisRoot.GetChild("prediction");
if (prediction is not null)
{
    Double score = prediction.GetMetadata<Double>("Score");
    Console.WriteLine($"Prediction score: {score}");
}
```

### Binding Data

```csharp
// Create object with data
SXObject dataObj = new(SXType.ByteArray, "RawData");
Byte[] fileBytes = File.ReadAllBytes("sample.exe");
dataObj.Data = new SXData(fileBytes);
dataObj.SetMetadata("Size", fileBytes.Length);

// Retrieve data later
if (dataObj.Data is not null)
{
    Byte[] retrieved = dataObj.Data.GetData();
    Console.WriteLine($"Data size: {retrieved.Length}");
}
```

## Notes

- Tree structure is maintained through Parent/Children references
- Parent reference is automatically set when adding child objects
- Parent reference is automatically cleared when removing child objects
- Metadata supports any type of value, ensure type matching when retrieving
- ModifiedAt is automatically updated during modification operations
- GetPath() depends on Name property, uses ID if Name is empty
