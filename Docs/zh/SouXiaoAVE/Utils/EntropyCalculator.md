# EntropyCalculator 静态类

**命名空间**: `SouXiaoAVE.Utils`  
**文件**: `SouXiaoAVE/Utils/EntropyCalculator.cs`  
**类型**: 静态类 (Static Class)

## 概述

信息熵计算工具，用于计算数据的香农熵。熵值反映数据的随机性程度，是恶意软件检测的重要特征。

## 方法

### Calculate(Byte[] data)

计算字节数组的信息熵。

**参数**:
- `data`: Byte[] - 输入字节数组

**输出**: Double - 熵值（0-8范围）

**内部流程**:
1. 检查空数据，返回0
2. 统计每个字节值的出现频率
3. 计算每个频率的概率
4. 应用香农熵公式：`H = -Σ p * log2(p)`
5. 返回熵值

**数学公式**:
```
H(X) = -Σ p(x) * log2(p(x))
```

```csharp
Byte[] data = File.ReadAllBytes("test.exe");
Double entropy = EntropyCalculator.Calculate(data);
Console.WriteLine($"熵值: {entropy:F4}");
// 典型值: 5.0-8.0（高熵表示压缩/加密）
```

### Calculate(`ReadOnlySpan<Byte>` data)

计算字节跨度的信息熵。

**参数**:
- `data`: `ReadOnlySpan<Byte>` - 输入字节跨度

**输出**: Double - 熵值（0-8范围）

**优势**: 避免数组分配，适合处理大文件的片段。

```csharp
Byte[] fileBytes = File.ReadAllBytes("large.exe");
ReadOnlySpan<Byte> section = new(fileBytes, 1000, 500);
Double sectionEntropy = EntropyCalculator.Calculate(section);
```

### CalculateFromString(String data)

计算字符串的信息熵。

**参数**:
- `data`: String - 输入字符串

**输出**: Double - 熵值

**内部流程**:
1. 检查空字符串，返回0
2. 统计每个字符的出现频率
3. 计算概率和熵值

```csharp
String text = "Hello, World!";
Double entropy = EntropyCalculator.CalculateFromString(text);
Console.WriteLine($"字符串熵: {entropy:F4}");
```

## 熵值解读

| 熵值范围 | 数据特征 | 典型示例 |
|----------|----------|----------|
| 0.0 - 1.0 | 极低随机性 | 全零、重复数据 |
| 1.0 - 3.0 | 低随机性 | 文本、简单数据 |
| 3.0 - 5.0 | 中等随机性 | 混合数据 |
| 5.0 - 7.0 | 较高随机性 | 压缩数据、代码 |
| 7.0 - 8.0 | 极高随机性 | 加密数据、加壳 |

## 使用示例

### 文件熵分析

```csharp
String[] files = Directory.GetFiles(@"C:\Samples", "*.exe");

foreach (String file in files)
{
    Byte[] bytes = File.ReadAllBytes(file);
    Double entropy = EntropyCalculator.Calculate(bytes);
    
    String assessment = entropy switch
    {
        < 3.0 => "低熵（可能包含文本）",
        < 5.0 => "中等熵（正常程序）",
        < 7.0 => "较高熵（可能压缩）",
        _ => "高熵（可能加密/加壳）"
    };
    
    Console.WriteLine($"{Path.GetFileName(file)}: {entropy:F2} - {assessment}");
}
```

### 节熵计算

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

### 检测加壳

```csharp
Boolean IsLikelyPacked(Byte[] fileBytes)
{
    Double entropy = EntropyCalculator.Calculate(fileBytes);
    
    // 高熵可能表示加壳
    if (entropy > 7.0)
        return true;
    
    // 检查节熵
    PeFile pe = new(fileBytes);
    Int32 highEntropySections = 0;
    
    foreach (var section in pe.ImageSectionHeaders)
    {
        // 计算节熵...
        if (sectionEntropy > 7.5)
            highEntropySections++;
    }
    
    return highEntropySections >= 2;
}
```

### 字符串随机性分析

```csharp
String[] strings = ["password", "xK9#mP2$vL", "aaaaaaaaaa"];

foreach (String s in strings)
{
    Double entropy = EntropyCalculator.CalculateFromString(s);
    Console.WriteLine($"\"{s}\": {entropy:F2}");
}

// 输出:
// "password": 2.75
// "xK9#mP2$vL": 3.32
// "aaaaaaaaaa": 0.00
```

## 性能考虑

- 使用固定大小数组（256）统计频率，避免字典开销
- 单次遍历计算频率，O(n)时间复杂度
- 支持ReadOnlySpan避免内存分配
- 对于大文件，建议分段计算

## 注意事项

- 最大熵值为8（256种字节值的log2(256)）
- 空数据返回0而非抛出异常
- 字符串熵基于Unicode字符，非字节
- 高熵不一定表示恶意，需结合其他特征
