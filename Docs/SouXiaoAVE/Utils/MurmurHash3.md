# MurmurHash3 Static Class

**Namespace**: `SouXiaoAVE.Utils`  
**File**: `SouXiaoAVE/Utils/MurmurHash3.cs`  
**Type**: Static Class

## Overview

MurmurHash3 algorithm implementation for fast computation of 32-bit hash values. Used in feature extraction to convert strings like section names into numerical features.

## Methods

### Hash32(Byte[] data, UInt32 seed = 0)

Calculate 32-bit hash value of byte array.

**Parameters**:
- `data`: Byte[] - Input byte array
- `seed`: UInt32 - Seed value (default 0)

**Returns**: UInt32 - 32-bit hash value

**Internal Flow**:
1. Initialize hash value to seed
2. Process data in 4-byte blocks
3. Apply mix constants c1, c2 to each block
4. Rotate and mix operations
5. Process remaining tail bytes
6. Final mix (fmix)

**Algorithm Constants**:
- c1 = 0xcc9e2d51
- c2 = 0x1b873593

```csharp
Byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
UInt32 hash = MurmurHash3.Hash32(data);
Console.WriteLine($"Hash: {hash:X8}");
// Output: Hash: XXXXXXXX
```

### HashString(String str, UInt32 seed = 0)

Calculate 32-bit hash value of string.

**Parameters**:
- `str`: String - Input string
- `seed`: UInt32 - Seed value (default 0)

**Returns**: UInt32 - 32-bit hash value

**Internal Flow**:
1. Convert string to UTF-8 byte array
2. Call Hash32 to compute hash

```csharp
UInt32 hash = MurmurHash3.HashString(".text");
Console.WriteLine($".text hash: {hash}");
```

## Internal Methods

### RotateLeft(UInt32 x, Byte r)

32-bit rotate left.

**Parameters**:
- `x`: UInt32 - Value to rotate
- `r`: Byte - Number of bits to rotate

**Returns**: UInt32 - Rotated value

```csharp
// Implementation: (x << r) | (x >> (32 - r))
```

### FMix(UInt32 h)

Final mix function.

**Parameters**:
- `h`: UInt32 - Hash value

**Returns**: UInt32 - Mixed hash value

**Internal Flow**:
1. h ^= h >> 16
2. h *= 0x85ebca6b
3. h ^= h >> 13
4. h *= 0xc2b2ae35
5. h ^= h >> 16

## Usage Examples

### Section Name Hash Feature

```csharp
// Used in PeFeatureExtractor
String sectionName = ".text";
UInt32 hash = MurmurHash3.HashString(sectionName);
Double normalizedHash = hash / (Double)UInt32.MaxValue; // Normalize to [0,1]

features[offset] = normalizedHash;
Console.WriteLine($".text -> {hash} -> {normalizedHash:F6}");
```

### Batch Hash Computation

```csharp
String[] sectionNames = [".text", ".data", ".rdata", ".rsrc", ".reloc"];

foreach (String name in sectionNames)
{
    UInt32 hash = MurmurHash3.HashString(name);
    Console.WriteLine($"{name,-10} -> {hash,10} (0x{hash:X8})");
}

// Output:
// .text      -> XXXXXXXX (0xXXXXXXXX)
// .data      -> XXXXXXXX (0xXXXXXXXX)
// ...
```

### Using Seed Value

```csharp
String data = "test";
UInt32 hash1 = MurmurHash3.HashString(data, 0);
UInt32 hash2 = MurmurHash3.HashString(data, 12345);

Console.WriteLine($"Seed 0: {hash1:X8}");
Console.WriteLine($"Seed 12345: {hash2:X8}");
// Different seeds produce different hash values
```

### Hash Collision Detection

```csharp
Dictionary<UInt32, List<String>> hashGroups = [];

String[] names = [".text", ".data", ".rdata", ".TEXT", ".DATA"];

foreach (String name in names)
{
    UInt32 hash = MurmurHash3.HashString(name);
    
    if (!hashGroups.TryGetValue(hash, out List<String>? group))
    {
        group = [];
        hashGroups[hash] = group;
    }
    group.Add(name);
}

foreach (KeyValuePair<UInt32, List<String>> kvp in hashGroups)
{
    if (kvp.Value.Count > 1)
    {
        Console.WriteLine($"Collision: {string.Join(", ", kvp.Value)} -> {kvp.Key:X8}");
    }
}
```

## Algorithm Characteristics

| Feature | Description |
|---------|-------------|
| Speed | Very fast, suitable for large data volumes |
| Distribution | Uniform distribution, low collision rate |
| Deterministic | Same input produces same output |
| Seed | Supports seed value to change output |
| Non-cryptographic | Not suitable for security purposes |

## Comparison with Other Hashes

| Algorithm | Speed | Collision Rate | Cryptographically Secure |
|-----------|-------|----------------|-------------------------|
| MurmurHash3 | Very fast | Low | No |
| SHA256 | Slow | Very low | Yes |
| MD5 | Medium | Medium | No |
| CRC32 | Very fast | High | No |

## Application in Feature Extraction

```csharp
// Actual usage in PeFeatureExtractor.cs
private Int32 ExtractSectionFeatures(FeatureVector features, Int32 offset)
{
    foreach (ImageSectionHeader sec in sections)
    {
        // Hash and normalize section name as feature value
        features[offset++] = MurmurHash3.HashString(sec.Name) / (Double)UInt32.MaxValue;
        // ... other features
    }
}
```

## Notes

- Non-cryptographic hash, not suitable for security verification
- Same string always produces same hash (deterministic)
- Seed value can be used to create different hash spaces
- Use `UInt32.MaxValue` as divisor for normalization
- Strings are converted to bytes using UTF-8 encoding
