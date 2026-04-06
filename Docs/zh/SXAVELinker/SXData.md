# SXData 类

**命名空间**: `SXAVELinker`  
**文件**: `SXAVELinker/SXData.cs`  
**类型**: 密封类 (Sealed Class)  
**实现接口**: `IDisposable`

## 概述

字节数据容器，支持压缩存储和文件路径引用。当数据大于1KB时自动启用GZip压缩，优化内存使用。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| OriginalSize | Int64 | 是 | 原始数据大小（字节） |
| CompressedSize | Int64 | 是 | 压缩后数据大小（字节） |
| IsFilePath | Boolean | 是 | 是否为文件路径引用模式 |
| HasData | Boolean | 是 | 是否包含数据 |

## 构造函数

### SXData()

无参构造函数，创建空数据容器。

```csharp
SXData data = new();
```

### SXData(Byte[] data)

从字节数组创建数据容器。

**参数**:
- `data`: Byte[] - 输入字节数组

**内部流程**:
1. 检查对象是否已释放
2. 清除文件路径引用
3. 记录原始大小
4. 若数据 > 1KB，启用GZip压缩
5. 否则直接存储

```csharp
Byte[] bytes = File.ReadAllBytes("sample.exe");
SXData data = new(bytes);
Console.WriteLine($"原始: {data.OriginalSize}, 压缩后: {data.CompressedSize}");
```

### SXData(String filePath)

从文件路径创建数据容器（延迟加载模式）。

**参数**:
- `filePath`: String - 文件路径

**异常**:
- `FileNotFoundException`: 文件不存在时抛出

```csharp
SXData data = new(@"C:\Windows\System32\notepad.exe");
Console.WriteLine($"文件大小: {data.OriginalSize} 字节");
```

## 方法

### SetData(Byte[] data)

设置字节数据。

**输入**:
- `data`: Byte[] - 要存储的字节数组

**内部流程**:
1. 调用 `ThrowIfDisposed()` 检查对象状态
2. 清除文件路径引用
3. 根据大小决定是否压缩

**输出**: 无返回值

**异常**:
- `ObjectDisposedException`: 对象已释放

```csharp
SXData data = new();
data.SetData(new Byte[] { 0x4D, 0x5A, 0x90, 0x00 });
```

### SetFilePath(String filePath)

设置文件路径引用。

**输入**:
- `filePath`: String - 文件路径

**内部流程**:
1. 检查对象状态
2. 验证文件存在性
3. 记录文件信息

**输出**: 无返回值

**异常**:
- `ObjectDisposedException`: 对象已释放
- `FileNotFoundException`: 文件不存在

```csharp
SXData data = new();
data.SetFilePath(@"C:\test\sample.dll");
```

### GetData()

获取数据字节数组。

**输入**: 无

**内部流程**:
1. 检查对象状态
2. 若为文件路径模式，读取文件内容
3. 若为压缩数据，解压后返回
4. 否则直接返回原始数据

**输出**: Byte[] - 数据字节数组

**异常**:
- `ObjectDisposedException`: 对象已释放

```csharp
SXData data = new(@"C:\Windows\System32\kernel32.dll");
Byte[] bytes = data.GetData();
Console.WriteLine($"读取 {bytes.Length} 字节");
```

### GetFilePath()

获取文件路径。

**输入**: 无

**输出**: String? - 文件路径，若非文件模式则返回 null

```csharp
SXData data = new(@"C:\test\file.exe");
String? path = data.GetFilePath();
if (path is not null)
{
    Console.WriteLine($"引用文件: {path}");
}
```

### GetDataAsync()

异步获取数据字节数组。

**输入**: 无

**内部流程**: 同 `GetData()`，但使用异步文件读取

**输出**: `Task<Byte[]>` - 数据字节数组的异步任务

```csharp
SXData data = new(@"C:\large_file.bin");
Byte[] bytes = await data.GetDataAsync();
```

### Clear()

清除所有数据。

**输入**: 无

**输出**: 无返回值

```csharp
SXData data = new(someBytes);
data.Clear();
Console.WriteLine($"清除后: {data.HasData}"); // False
```

### Dispose()

释放资源。

```csharp
SXData data = new(someBytes);
data.Dispose();
// data.GetData(); // 抛出 ObjectDisposedException
```

## 使用示例

### 完整工作流示例

```csharp
// 创建数据容器
SXData data = new();

// 设置数据（自动压缩）
Byte[] largeData = new Byte[10000];
Random.Shared.NextBytes(largeData);
data.SetData(largeData);

Console.WriteLine($"原始大小: {data.OriginalSize}");
Console.WriteLine($"压缩大小: {data.CompressedSize}");
Console.WriteLine($"压缩比: {data.CompressedSize * 100.0 / data.OriginalSize:F2}%");

// 获取数据（自动解压）
Byte[] retrieved = data.GetData();
Console.WriteLine($"数据完整: {retrieved.Length == largeData.Length}");

// 清理
data.Dispose();
```

## 注意事项

- 大于1KB的数据自动启用GZip压缩
- 文件路径模式不立即加载内容，节省内存
- 使用 `using` 语句或手动调用 `Dispose()` 释放资源
- 对象释放后调用任何方法都会抛出 `ObjectDisposedException`
