# PeFeatureExtractor Class

**Namespace**: `SouXiaoAVE.Services`  
**File**: `SouXiaoAVE/Services/PeFeatureExtractor.cs`  
**Type**: Sealed Class

## Overview

PE file static feature extractor that extracts 512-dimensional feature vectors from Windows executable files for malware detection machine learning models.

## Constants

| Constant Name | Value | Description |
|---------------|-------|-------------|
| MaxSections | 20 | Maximum number of sections to process |
| SegmentCount | 32 | Number of file segments (for entropy calculation) |

## Static Fields

### KeyDlls

List of key DLLs (30) for import table feature extraction.

```csharp
["kernel32.dll", "ntdll.dll", "user32.dll", "advapi32.dll", ...]
```

### KeyApis

List of key APIs (50) for detecting suspicious function calls.

```csharp
["CreateRemoteThread", "WriteProcessMemory", "VirtualAllocEx", ...]
```

### KeySubstrings

List of key substrings (30) for string feature detection.

```csharp
["cmd.exe", "powershell", "rundll32", "-enc", "Invoke-", ...]
```

### SuspiciousSectionNames

List of suspicious section names (15) for packer detection.

```csharp
["UPX0", "UPX1", ".aspack", ".themida", ".vmp0", ...]
```

## Methods

### Extract(String filePath)

Extract features from file path.

**Parameters**:
- `filePath`: String - PE file path

**Returns**: FeatureVector - 512-dimensional feature vector

**Internal Flow**:
1. Read file bytes
2. Create PeFile instance
3. Call 11 feature extraction methods in sequence
4. Return filled feature vector

**Possible Exceptions**:
- `FileNotFoundException`: File does not exist
- `PeNet` parsing exceptions
- Out of memory

```csharp
PeFeatureExtractor extractor = new();
FeatureVector features = extractor.Extract(@"C:\Windows\System32\notepad.exe");
Console.WriteLine($"Feature dimensions: {features.Features.Length}"); // 512
```

### Extract(Byte[] fileBytes)

Extract features from byte array.

**Parameters**:
- `fileBytes`: Byte[] - PE file byte data

**Returns**: FeatureVector - 512-dimensional feature vector

```csharp
Byte[] bytes = File.ReadAllBytes("sample.exe");
FeatureVector features = extractor.Extract(bytes);
```

## Feature Extraction Methods

### ExtractGlobalStatistics

Extract global statistical features (37 dimensions).

**Output Dimensions**: 0-36

| Index | Feature | Description |
|-------|---------|-------------|
| 0 | FileSize | File size |
| 1 | Entropy | Global entropy |
| 2 | CompressionRatio | Compression ratio |
| 3-34 | SegmentEntropy[0-31] | Segment entropy values |
| 35-36 | Reserved | - |

```csharp
// Internal implementation
features[offset++] = _fileBytes.Length;
features[offset++] = EntropyCalculator.Calculate(_fileBytes);
features[offset++] = CalculateCompressionRatio(_fileBytes);
// 32 segment entropies...
```

### ExtractOptionalHeaderFeatures

Extract optional header features (20 dimensions).

**Output Dimensions**: 37-56

| Index | Feature | Description |
|-------|---------|-------------|
| 37 | MajorLinkerVersion | Linker major version |
| 38 | MinorLinkerVersion | Linker minor version |
| 39 | SizeOfCode | Code section size |
| 40 | SizeOfInitializedData | Initialized data size |
| 41 | SizeOfUninitializedData | Uninitialized data size |
| 42 | AddressOfEntryPoint | Entry point address |
| 43 | BaseOfCode | Code base address |
| 44 | ImageBase | Image base address |
| 45 | SectionAlignment | Section alignment |
| 46 | FileAlignment | File alignment |
| 47-50 | OS/Subsystem version | Operating system version info |
| 51 | SizeOfImage | Image size |
| 52 | SizeOfHeaders | Headers size |
| 53 | Subsystem | Subsystem type |
| 54 | DllCharacteristics | DLL characteristics |
| 55 | SizeOfStackReserve | Stack reserve size |

### ExtractSectionFeatures

Extract section table features (240 dimensions = 20 sections × 12 features).

**Output Dimensions**: 57-296

12 features per section:

| Sub-index | Feature | Description |
|-----------|---------|-------------|
| 0 | NameHash | Section name hash (normalized) |
| 1 | VirtualSize | Virtual size |
| 2 | SizeOfRawData | Raw data size |
| 3 | Entropy | Section entropy |
| 4 | IsExecutable | Executable flag |
| 5 | IsWritable | Writable flag |
| 6 | ContainsCode | Contains code flag |
| 7 | ContainsInitData | Contains initialized data |
| 8 | IsStandardName | Standard section name flag |
| 9 | ValidPointer | Valid pointer flag |
| 10 | CharacteristicsLow | Characteristics low byte |
| 11 | StringDensity | String density |

### ExtractImportFeatures

Extract import table features (80 dimensions = 30 DLLs + 50 APIs).

**Output Dimensions**: 297-376

| Range | Feature | Description |
|-------|---------|-------------|
| 297-326 | DllCounts[0-29] | Key DLL import counts |
| 327-376 | ApiFlags[0-49] | Key API presence flags |

### ExtractExportFeatures

Extract export table features (5 dimensions).

**Output Dimensions**: 377-381

| Index | Feature | Description |
|-------|---------|-------------|
| 377 | HasExports | Whether has export table |
| 378 | ExportCount | Export function count |
| 379 | NamedExportCount | Named export count |
| 380 | HasSuspiciousExport | Suspicious export flag |
| 381 | ExportNameEntropy | Export name entropy |

### ExtractResourceFeatures

Extract resource features (5 dimensions).

**Output Dimensions**: 382-386

| Index | Feature | Description |
|-------|---------|-------------|
| 382 | ResourceCount | Resource entry count |
| 383 | ResourceTypeCount | Resource type count |
| 384 | AvgResourceEntropy | Average resource entropy |
| 385 | MaxResourceEntropy | Maximum resource entropy |
| 386 | HasPeResource | Embedded PE flag |

### ExtractStringFeatures

Extract string features (35 dimensions = 5 statistics + 30 substrings).

**Output Dimensions**: 387-421

| Range | Feature | Description |
|-------|---------|-------------|
| 387 | StringCount | String count |
| 388 | AvgStringLength | Average string length |
| 389 | StringEntropy | String entropy |
| 390 | UrlCount | URL count |
| 391 | IpCount | IP address count |
| 392-421 | SubstringFlags[0-29] | Key substring presence flags |

### ExtractEntryPointFeatures

Extract entry point features (10 dimensions).

**Output Dimensions**: 422-431

| Index | Feature | Description |
|-------|---------|-------------|
| 422 | EntryPointSectionIndex | Entry point section index |
| 423 | EntryPointSectionEntropy | Entry section entropy |
| 424 | EntryPointOffset | Entry point offset |
| 425 | EntryPointBytesEntropy | Entry bytes entropy |
| 426 | PushCount | PUSH instruction count |
| 427 | CallCount | CALL instruction count |
| 428 | JmpCount | Jump instruction count |
| 429 | IsExecutableSection | Executable section flag |
| 430 | IsTextSection | .text section flag |
| 431 | HasLegacyProlog | Legacy prologue flag |

### ExtractRelocationTlsFeatures

Extract relocation and TLS features (10 dimensions).

**Output Dimensions**: 432-441

| Index | Feature | Description |
|-------|---------|-------------|
| 432 | HasRelocations | Whether has relocations |
| 433 | RelocationSize | Relocation table size |
| 434 | RelocationCount | Relocation entry count |
| 435 | HasTls | Whether has TLS |
| 436 | TlsCallbackCount | TLS callback count |
| 437 | HasExceptionDir | Whether has exception directory |
| 438 | HasProcessInjection | Process injection API flag |
| 439 | HasNetworkApi | Network API flag |
| 440 | AlignmentMismatch | Alignment mismatch flag |
| 441 | HasEmptySection | Empty section flag |

### ExtractByteStatistics

Extract byte statistics features (20 dimensions).

**Output Dimensions**: 442-461

| Index | Feature | Description |
|-------|---------|-------------|
| 442 | Mean | Byte mean |
| 443 | Variance | Byte variance |
| 444 | Skewness | Skewness |
| 445 | Kurtosis | Kurtosis |
| 446 | Median | Median |
| 447 | Mode | Mode |
| 448-457 | Top10Frequency | Top 10 high frequency byte frequencies |
| 458 | Autocorrelation | Autocorrelation coefficient |

### ExtractSupplementaryFeatures

Extract supplementary features (50 dimensions).

**Output Dimensions**: 462-511

| Range | Feature | Description |
|-------|---------|-------------|
| 462-471 | ApiCategoryCounts[0-9] | API category counts |
| 472-486 | SuspiciousSectionFlags[0-14] | Suspicious section flags |
| 487-501 | InstructionNgrams[0-14] | Instruction N-gram features |
| 502-511 | TailFeatures[0-9] | File tail features |

## Helper Methods

### GetSectionEntropy(ImageSectionHeader section)

Calculate section entropy.

### IsStandardSectionName(String name)

Check if it's a standard section name.

### CalculateStringDensity(ImageSectionHeader section)

Calculate string density within section.

### FindSectionByRva(UInt32 rva, ImageSectionHeader[] sections)

Find owning section by RVA.

### GetEpBytes(UInt32 epRva, Int32 count)

Get entry point bytes.

### CountInstructions(Byte[] code)

Count instructions (using Capstone disassembly).

### ExtractStrings(Byte[] data, Int32 minLength = 4)

Extract printable strings.

### CalculateAutocorrelation(Byte[] data, Int32 lag)

Calculate autocorrelation coefficient.

### ExtractInstructionNgrams(Byte[] code)

Extract instruction N-gram features.

### ExtractTailFeatures()

Extract file tail features.

## Usage Examples

### Basic Extraction

```csharp
PeFeatureExtractor extractor = new();

FeatureVector features = extractor.Extract(@"C:\test.exe");

Console.WriteLine($"Feature dimensions: {features.Features.Length}");
Console.WriteLine($"File size: {features[0]}");
Console.WriteLine($"Global entropy: {features[1]:F4}");
Console.WriteLine($"Compression ratio: {features[2]:F4}");
```

### Feature Analysis

```csharp
FeatureVector features = extractor.Extract(fileBytes);

// Calculate sparsity
Int32 zeroCount = features.Features.Count(f => f == 0);
Double sparsity = (Double)zeroCount / 512;
Console.WriteLine($"Sparsity: {sparsity:P2}");

// View section entropy
for (Int32 i = 0; i < 5; i++)
{
    Int32 offset = 57 + i * 12 + 3; // Section entropy at 4th position
    Console.WriteLine($"Section {i} entropy: {features[offset]:F4}");
}

// Check suspicious APIs
Boolean hasInjection = features[438] > 0;
Boolean hasNetwork = features[439] > 0;
Console.WriteLine($"Process injection API: {hasInjection}");
Console.WriteLine($"Network API: {hasNetwork}");
```

### Batch Processing

```csharp
PeFeatureExtractor extractor = new();
String[] files = Directory.GetFiles(@"C:\Samples", "*.exe");

List<FeatureVector> allFeatures = [];

foreach (String file in files)
{
    try
    {
        FeatureVector fv = extractor.Extract(file);
        allFeatures.Add(fv);
        Console.WriteLine($"{Path.GetFileName(file)}: OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{Path.GetFileName(file)}: {ex.Message}");
    }
}

Console.WriteLine($"Successfully extracted: {allFeatures.Count}/{files.Length}");
```

## Notes

- Depends on PeNet library for PE structure parsing
- Depends on Capstone library for disassembly
- Large files may consume significant memory
- Feature dimensions fixed at 512 to match model
- Some features are sensitive to packing/obfuscation
