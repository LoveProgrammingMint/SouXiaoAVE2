# SXData Class

**Namespace**: `SXAVELinker`  
**File**: `SXAVELinker/SXData.cs`  
**Type**: Sealed Class  
**Implements**: `IDisposable`

## Overview

Byte data container that supports compressed storage and file path references. Automatically enables GZip compression when data exceeds 1KB to optimize memory usage.

## Properties

| Property | Type | Read-only | Description |
|--------|------|------|------|
| OriginalSize | Int64 | Yes | Original data size (bytes) |
| CompressedSize | Int64 | Yes | Compressed data size (bytes) |
| IsFilePath | Boolean | Yes | Whether in file path reference mode |
| HasData | Boolean | Yes | Whether data is contained |

## Constructors

### SXData()

Parameterless constructor, creates an empty data container.

```csharp
SXData data = new();
```

### SXData(Byte[] data)

Creates a data container from a byte array.

**Parameters**:
- `data`: Byte[] - Input byte array

**Internal Flow**:
1. Check if the object has been disposed
2. Clear file path reference
3. Record original size
4. If data > 1KB, enable GZip compression
5. Otherwise store directly

```csharp
Byte[] bytes = File.ReadAllBytes("sample.exe");
SXData data = new(bytes);
Console.WriteLine($"Original: {data.OriginalSize}, Compressed: {data.CompressedSize}");
```

### SXData(String filePath)

Creates a data container from a file path (lazy loading mode).

**Parameters**:
- `filePath`: String - File path

**Exceptions**:
- `FileNotFoundException`: Thrown when file does not exist

```csharp
SXData data = new(@"C:\Windows\System32\notepad.exe");
Console.WriteLine($"File size: {data.OriginalSize} bytes");
```

## Methods

### SetData(Byte[] data)

Sets byte data.

**Input**:
- `data`: Byte[] - Byte array to store

**Internal Flow**:
1. Call `ThrowIfDisposed()` to check object state
2. Clear file path reference
3. Decide whether to compress based on size

**Output**: No return value

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed

```csharp
SXData data = new();
data.SetData(new Byte[] { 0x4D, 0x5A, 0x90, 0x00 });
```

### SetFilePath(String filePath)

Sets file path reference.

**Input**:
- `filePath`: String - File path

**Internal Flow**:
1. Check object state
2. Verify file existence
3. Record file information

**Output**: No return value

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed
- `FileNotFoundException`: File does not exist

```csharp
SXData data = new();
data.SetFilePath(@"C:\test\sample.dll");
```

### GetData()

Gets the data byte array.

**Input**: None

**Internal Flow**:
1. Check object state
2. If in file path mode, read file contents
3. If compressed data, decompress and return
4. Otherwise return raw data directly

**Output**: Byte[] - Data byte array

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed

```csharp
SXData data = new(@"C:\Windows\System32\kernel32.dll");
Byte[] bytes = data.GetData();
Console.WriteLine($"Read {bytes.Length} bytes");
```

### GetFilePath()

Gets the file path.

**Input**: None

**Output**: String? - File path, returns null if not in file mode

```csharp
SXData data = new(@"C:\test\file.exe");
String? path = data.GetFilePath();
if (path is not null)
{
    Console.WriteLine($"Referenced file: {path}");
}
```

### GetDataAsync()

Asynchronously gets the data byte array.

**Input**: None

**Internal Flow**: Same as `GetData()`, but uses asynchronous file reading

**Output**: `Task<Byte[]>` - Async task of data byte array

```csharp
SXData data = new(@"C:\large_file.bin");
Byte[] bytes = await data.GetDataAsync();
```

### Clear()

Clears all data.

**Input**: None

**Output**: No return value

```csharp
SXData data = new(someBytes);
data.Clear();
Console.WriteLine($"After clearing: {data.HasData}"); // False
```

### Dispose()

Releases resources.

```csharp
SXData data = new(someBytes);
data.Dispose();
// data.GetData(); // Throws ObjectDisposedException
```

## Usage Examples

### Complete Workflow Example

```csharp
// Create data container
SXData data = new();

// Set data (auto compression)
Byte[] largeData = new Byte[10000];
Random.Shared.NextBytes(largeData);
data.SetData(largeData);

Console.WriteLine($"Original size: {data.OriginalSize}");
Console.WriteLine($"Compressed size: {data.CompressedSize}");
Console.WriteLine($"Compression ratio: {data.CompressedSize * 100.0 / data.OriginalSize:F2}%");

// Get data (auto decompression)
Byte[] retrieved = data.GetData();
Console.WriteLine($"Data integrity: {retrieved.Length == largeData.Length}");

// Cleanup
data.Dispose();
```

## Notes

- Data larger than 1KB automatically enables GZip compression
- File path mode does not immediately load contents, saving memory
- Use `using` statement or manually call `Dispose()` to release resources
- Calling any method after object disposal will throw `ObjectDisposedException`
