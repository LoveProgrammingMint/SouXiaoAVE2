# EntropyCalculator Static Class

**Namespace**: `SouXiaoAVE.Utils`  
**File**: `SouXiaoAVE/Utils/EntropyCalculator.cs`  
**Type**: Static Class

## Overview

Information entropy calculation utility for computing Shannon entropy of data. Entropy reflects the randomness of data and is an important feature for malware detection.

## Methods

### Calculate(Byte[] data)

Calculate information entropy of byte array.

**Parameters**:
- `data`: Byte[] - Input byte array

**Returns**: Double - Entropy value (0-8 range)

**Internal Flow**:
1. Check for empty data, return 0
2. Count frequency of each byte value
3. Calculate probability for each frequency
4. Apply Shannon entropy formula: `H = -Σ p * log2(p)`
5. Return entropy value

**Mathematical Formula**:
```
H(X) = -Σ p(x) * log2(p(x))
```

```csharp
Byte[] data = File.ReadAllBytes("test.exe");
Double entropy = EntropyCalculator.Calculate(data);
Console.WriteLine($"Entropy: {entropy:F4}");
// Typical values: 5.0-8.0 (high entropy indicates compression/encryption)
```

### Calculate(`ReadOnlySpan<Byte>` data)

Calculate information entropy of byte span.

**Parameters**:
- `data`: `ReadOnlySpan<Byte>` - Input byte span

**Returns**: Double - Entropy value (0-8 range)

**Advantage**: Avoids array allocation, suitable for processing large file segments.

```csharp
Byte[] fileBytes = File.ReadAllBytes("large.exe");
ReadOnlySpan<Byte> section = new(fileBytes, 1000, 500);
Double sectionEntropy = EntropyCalculator.Calculate(section);
```

### CalculateFromString(String data)

Calculate information entropy of string.

**Parameters**:
- `data`: String - Input string

**Returns**: Double - Entropy value

**Internal Flow**:
1. Check for empty string, return 0
2. Count frequency of each character
3. Calculate probability and entropy

```csharp
String text = "Hello, World!";
Double entropy = EntropyCalculator.CalculateFromString(text);
Console.WriteLine($"String entropy: {entropy:F4}");
```

## Entropy Interpretation

| Entropy Range | Data Characteristics | Typical Examples |
|---------------|---------------------|------------------|
| 0.0 - 1.0 | Very low randomness | All zeros, repeated data |
| 1.0 - 3.0 | Low randomness | Text, simple data |
| 3.0 - 5.0 | Medium randomness | Mixed data |
| 5.0 - 7.0 | High randomness | Compressed data, code |
| 7.0 - 8.0 | Very high randomness | Encrypted data, packed |

## Usage Examples

### File Entropy Analysis

```csharp
String[] files = Directory.GetFiles(@"C:\Samples", "*.exe");

foreach (String file in files)
{
    Byte[] bytes = File.ReadAllBytes(file);
    Double entropy = EntropyCalculator.Calculate(bytes);
    
    String assessment = entropy switch
    {
        < 3.0 => "Low entropy (may contain text)",
        < 5.0 => "Medium entropy (normal program)",
        < 7.0 => "High entropy (possibly compressed)",
        _ => "Very high entropy (possibly encrypted/packed)"
    };
    
    Console.WriteLine($"{Path.GetFileName(file)}: {entropy:F2} - {assessment}");
}
```

### Section Entropy Calculation

```csharp
PeFile pe = new("sample.exe");

foreach (ImageSectionHeader section in pe.ImageSectionHeaders)
{
    if (section.PointerToRawData < fileBytes.Length && section.SizeOfRawData > 0)
    {
        Int32 start = (Int32)section.PointerToRawData;
        Int32 length = (Int32)Math.Min(section.SizeOfRawData, fileBytes.Length - start);
        ReadOnlySpan<Byte> sectionData = new(fileBytes, start, length);
        
        Double entropy = EntropyCalculator.Calculate(sectionData);
        Console.WriteLine($"{section.Name}: {entropy:F2}");
    }
}
```

### Packer Detection

```csharp
Boolean IsLikelyPacked(Byte[] fileBytes)
{
    Double entropy = EntropyCalculator.Calculate(fileBytes);
    
    // High entropy may indicate packing
    if (entropy > 7.0)
        return true;
    
    // Check section entropy
    PeFile pe = new(fileBytes);
    Int32 highEntropySections = 0;
    
    foreach (var section in pe.ImageSectionHeaders)
    {
        // Calculate section entropy...
        if (sectionEntropy > 7.5)
            highEntropySections++;
    }
    
    return highEntropySections >= 2;
}
```

### String Randomness Analysis

```csharp
String[] strings = ["password", "xK9#mP2$vL", "aaaaaaaaaa"];

foreach (String s in strings)
{
    Double entropy = EntropyCalculator.CalculateFromString(s);
    Console.WriteLine($"\"{s}\": {entropy:F2}");
}

// Output:
// "password": 2.75
// "xK9#mP2$vL": 3.32
// "aaaaaaaaaa": 0.00
```

## Performance Considerations

- Uses fixed-size array (256) for frequency counting, avoiding dictionary overhead
- Single-pass frequency calculation, O(n) time complexity
- Supports ReadOnlySpan to avoid memory allocation
- For large files, segment-based calculation is recommended

## Notes

- Maximum entropy value is 8 (log2(256) for 256 byte values)
- Empty data returns 0 instead of throwing exception
- String entropy is based on Unicode characters, not bytes
- High entropy doesn't necessarily indicate maliciousness, combine with other features
