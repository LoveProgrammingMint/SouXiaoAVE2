# FeatureVector 类

**命名空间**: `SouXiaoAVE.Models`  
**文件**: `SouXiaoAVE/Models/FeatureVector.cs`  
**类型**: 密封类 (Sealed Class)

## 概述

512维特征向量容器，用于存储PE文件静态分析特征。固定维度设计确保与LightGBM模型输入兼容。

## 常量

| 常量名 | 值 | 描述 |
|--------|-----|------|
| TotalDimensions | 512 | 特征向量总维度数 |

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| Features | Double[] | 是 | 特征值数组（长度固定512） |

## 索引器

### this[Int32 index]

通过索引访问特征值。

**参数**:
- `index`: Int32 - 特征索引（0-511）

**输出**: Double - 特征值

```csharp
FeatureVector fv = new();
fv[0] = 1024.0;  // 文件大小
Double entropy = fv[1];  // 熵值
```

## 构造函数

### FeatureVector()

创建全零特征向量。

```csharp
FeatureVector fv = new();
Console.WriteLine($"维度: {fv.Features.Length}"); // 512
```

### FeatureVector(Double[] features)

从现有数组创建特征向量。

**参数**:
- `features`: Double[] - 特征数组（必须为512维）

**异常**:
- `ArgumentException`: 数组长度不为512

```csharp
Double[] data = new Double[512];
data[0] = 1024.0;
FeatureVector fv = new(data);
```

## 运算符

### 隐式转换为 Double[]

```csharp
FeatureVector fv = new();
Double[] array = fv; // 隐式转换
Console.WriteLine(array.Length); // 512
```

## 使用示例

### 特征提取与存储

```csharp
FeatureVector features = new();

// 设置全局统计特征
features[0] = fileSize;
features[1] = entropy;
features[2] = compressionRatio;

// 设置节特征（offset 57开始）
Int32 sectionOffset = 57;
for (Int32 i = 0; i < sections.Length && i < 20; i++)
{
    features[sectionOffset + i * 12 + 0] = HashSectionName(sections[i].Name);
    features[sectionOffset + i * 12 + 1] = sections[i].VirtualSize;
    features[sectionOffset + i * 12 + 2] = sections[i].Entropy;
    // ...
}

// 转换为模型输入
Double[] modelInput = features;
```

### 特征统计分析

```csharp
FeatureVector features = extractor.Extract(fileBytes);

// 计算稀疏度
Int32 zeroCount = features.Features.Count(f => f == 0);
Double sparsity = (Double)zeroCount / FeatureVector.TotalDimensions;

Console.WriteLine($"非零特征: {FeatureVector.TotalDimensions - zeroCount}");
Console.WriteLine($"稀疏度: {sparsity:P2}");

// 统计特征范围
Double min = features.Features.Min();
Double max = features.Features.Max();
Double mean = features.Features.Average();

Console.WriteLine($"范围: [{min:F4}, {max:F4}]");
Console.WriteLine($"均值: {mean:F4}");
```

## 特征维度分布

| 维度范围 | 特征组 | 维数 |
|----------|--------|------|
| 0-36 | 全局统计 | 37 |
| 37-56 | 可选头 | 20 |
| 57-296 | 节表(20×12) | 240 |
| 297-376 | 导入表 | 80 |
| 377-381 | 导出表 | 5 |
| 382-386 | 资源 | 5 |
| 387-421 | 字符串 | 35 |
| 422-431 | 入口点 | 10 |
| 432-441 | 重定位/TLS | 10 |
| 442-461 | 字节统计 | 20 |
| 462-511 | 补充 | 50 |

## 注意事项

- 固定512维，不可扩展
- 使用Double精度存储特征值
- 支持隐式转换为Double[]，方便模型调用
- 构造时检查维度，保证数据一致性
