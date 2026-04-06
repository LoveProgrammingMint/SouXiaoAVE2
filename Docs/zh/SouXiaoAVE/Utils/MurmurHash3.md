# MurmurHash3 静态类

**命名空间**: `SouXiaoAVE.Utils`  
**文件**: `SouXiaoAVE/Utils/MurmurHash3.cs`  
**类型**: 静态类 (Static Class)

## 概述

MurmurHash3算法实现，用于快速计算数据的32位哈希值。在特征提取中用于将节名称等字符串转换为数值特征。

## 方法

### Hash32(Byte[] data, UInt32 seed = 0)

计算字节数组的32位哈希值。

**参数**:
- `data`: Byte[] - 输入字节数组
- `seed`: UInt32 - 种子值（默认0）

**输出**: UInt32 - 32位哈希值

**内部流程**:
1. 初始化哈希值为种子
2. 按4字节块处理数据
3. 对每个块应用混合常量c1、c2
4. 旋转和混合操作
5. 处理剩余尾部字节
6. 最终混合（fmix）

**算法常量**:
- c1 = 0xcc9e2d51
- c2 = 0x1b873593

```csharp
Byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
UInt32 hash = MurmurHash3.Hash32(data);
Console.WriteLine($"Hash: {hash:X8}");
// 输出: Hash: XXXXXXXX
```

### HashString(String str, UInt32 seed = 0)

计算字符串的32位哈希值。

**参数**:
- `str`: String - 输入字符串
- `seed`: UInt32 - 种子值（默认0）

**输出**: UInt32 - 32位哈希值

**内部流程**:
1. 将字符串转换为UTF-8字节数组
2. 调用Hash32计算哈希

```csharp
UInt32 hash = MurmurHash3.HashString(".text");
Console.WriteLine($".text hash: {hash}");
```

## 内部方法

### RotateLeft(UInt32 x, Byte r)

32位循环左移。

**参数**:
- `x`: UInt32 - 要旋转的值
- `r`: Byte - 旋转位数

**输出**: UInt32 - 旋转后的值

```csharp
// 实现: (x << r) | (x >> (32 - r))
```

### FMix(UInt32 h)

最终混合函数。

**参数**:
- `h`: UInt32 - 哈希值

**输出**: UInt32 - 混合后的哈希值

**内部流程**:
1. h ^= h >> 16
2. h *= 0x85ebca6b
3. h ^= h >> 13
4. h *= 0xc2b2ae35
5. h ^= h >> 16

## 使用示例

### 节名称哈希特征

```csharp
// 在PeFeatureExtractor中使用
String sectionName = ".text";
UInt32 hash = MurmurHash3.HashString(sectionName);
Double normalizedHash = hash / (Double)UInt32.MaxValue; // 归一化到[0,1]

features[offset] = normalizedHash;
Console.WriteLine($".text -> {hash} -> {normalizedHash:F6}");
```

### 批量哈希计算

```csharp
String[] sectionNames = [".text", ".data", ".rdata", ".rsrc", ".reloc"];

foreach (String name in sectionNames)
{
    UInt32 hash = MurmurHash3.HashString(name);
    Console.WriteLine($"{name,-10} -> {hash,10} (0x{hash:X8})");
}

// 输出:
// .text      -> XXXXXXXX (0xXXXXXXXX)
// .data      -> XXXXXXXX (0xXXXXXXXX)
// ...
```

### 使用种子值

```csharp
String data = "test";
UInt32 hash1 = MurmurHash3.HashString(data, 0);
UInt32 hash2 = MurmurHash3.HashString(data, 12345);

Console.WriteLine($"Seed 0: {hash1:X8}");
Console.WriteLine($"Seed 12345: {hash2:X8}");
// 不同种子产生不同哈希值
```

### 哈希冲突检测

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
        Console.WriteLine($"冲突: {string.Join(", ", kvp.Value)} -> {kvp.Key:X8}");
    }
}
```

## 算法特点

| 特性 | 描述 |
|------|------|
| 速度 | 极快，适合大数据量 |
| 分布 | 均匀分布，低碰撞率 |
| 确定性 | 相同输入相同输出 |
| 种子 | 支持种子值改变输出 |
| 非加密 | 不适合安全用途 |

## 与其他哈希对比

| 算法 | 速度 | 碰撞率 | 加密安全 |
|------|------|--------|----------|
| MurmurHash3 | 极快 | 低 | 否 |
| SHA256 | 慢 | 极低 | 是 |
| MD5 | 中等 | 中 | 否 |
| CRC32 | 极快 | 高 | 否 |

## 在特征提取中的应用

```csharp
// PeFeatureExtractor.cs 中的实际使用
private Int32 ExtractSectionFeatures(FeatureVector features, Int32 offset)
{
    foreach (ImageSectionHeader sec in sections)
    {
        // 将节名称哈希归一化为特征值
        features[offset++] = MurmurHash3.HashString(sec.Name) / (Double)UInt32.MaxValue;
        // ... 其他特征
    }
}
```

## 注意事项

- 非加密哈希，不适合安全验证
- 相同字符串始终产生相同哈希（确定性）
- 种子值可用于创建不同哈希空间
- 归一化时使用 `UInt32.MaxValue` 作为除数
- 字符串使用UTF-8编码转换为字节
