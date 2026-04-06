# FeatureVector Class

**Namespace**: `SouXiaoAVE.Models`  
**File**: `SouXiaoAVE/Models/FeatureVector.cs`  
**Type**: Sealed Class

## Overview

512-dimensional feature vector container for storing PE file static analysis features. Fixed dimension design ensures compatibility with LightGBM model input.

## Constants

| Constant Name | Value | Description |
|---------------|-------|-------------|
| TotalDimensions | 512 | Total number of feature vector dimensions |

## Properties

| Property Name | Type | Read-only | Description |
|---------------|------|-----------|-------------|
| Features | Double[] | Yes | Feature value array (fixed length 512) |

## Indexer

### this[Int32 index]

Access feature value by index.

**Parameters**:
- `index`: Int32 - Feature index (0-511)

**Returns**: Double - Feature value

```csharp
FeatureVector fv = new();
fv[0] = 1024.0;  // File size
Double entropy = fv[1];  // Entropy value
```

## Constructors

### FeatureVector()

Create a zero-initialized feature vector.

```csharp
FeatureVector fv = new();
Console.WriteLine($"Dimensions: {fv.Features.Length}"); // 512
```

### FeatureVector(Double[] features)

Create feature vector from existing array.

**Parameters**:
- `features`: Double[] - Feature array (must be 512 dimensions)

**Exceptions**:
- `ArgumentException`: Array length is not 512

```csharp
Double[] data = new Double[512];
data[0] = 1024.0;
FeatureVector fv = new(data);
```

## Operators

### Implicit conversion to Double[]

```csharp
FeatureVector fv = new();
Double[] array = fv; // Implicit conversion
Console.WriteLine(array.Length); // 512
```

## Usage Examples

### Feature Extraction and Storage

```csharp
FeatureVector features = new();

// Set global statistical features
features[0] = fileSize;
features[1] = entropy;
features[2] = compressionRatio;

// Set section features (starting at offset 57)
Int32 sectionOffset = 57;
for (Int32 i = 0; i < sections.Length && i < 20; i++)
{
    features[sectionOffset + i * 12 + 0] = HashSectionName(sections[i].Name);
    features[sectionOffset + i * 12 + 1] = sections[i].VirtualSize;
    features[sectionOffset + i * 12 + 2] = sections[i].Entropy;
    // ...
}

// Convert to model input
Double[] modelInput = features;
```

### Feature Statistical Analysis

```csharp
FeatureVector features = extractor.Extract(fileBytes);

// Calculate sparsity
Int32 zeroCount = features.Features.Count(f => f == 0);
Double sparsity = (Double)zeroCount / FeatureVector.TotalDimensions;

Console.WriteLine($"Non-zero features: {FeatureVector.TotalDimensions - zeroCount}");
Console.WriteLine($"Sparsity: {sparsity:P2}");

// Feature range statistics
Double min = features.Features.Min();
Double max = features.Features.Max();
Double mean = features.Features.Average();

Console.WriteLine($"Range: [{min:F4}, {max:F4}]");
Console.WriteLine($"Mean: {mean:F4}");
```

## Feature Dimension Distribution

| Dimension Range | Feature Group | Dimensions |
|-----------------|---------------|------------|
| 0-36 | Global Statistics | 37 |
| 37-56 | Optional Header | 20 |
| 57-296 | Section Table (20×12) | 240 |
| 297-376 | Import Table | 80 |
| 377-381 | Export Table | 5 |
| 382-386 | Resources | 5 |
| 387-421 | Strings | 35 |
| 422-431 | Entry Point | 10 |
| 432-441 | Relocation/TLS | 10 |
| 442-461 | Byte Statistics | 20 |
| 462-511 | Supplementary | 50 |

## Notes

- Fixed 512 dimensions, not expandable
- Uses Double precision for feature value storage
- Supports implicit conversion to Double[] for convenient model invocation
- Validates dimensions on construction to ensure data consistency
