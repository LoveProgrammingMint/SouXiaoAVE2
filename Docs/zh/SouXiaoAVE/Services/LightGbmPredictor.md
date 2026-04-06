# LightGbmPredictor 类

**命名空间**: `SouXiaoAVE.Services`  
**文件**: `SouXiaoAVE/Services/LightGbmPredictor.cs`  
**类型**: 密封类 (Sealed Class)  
**实现接口**: `IDisposable`

## 概述

LightGBM模型推理器，使用P/Invoke调用原生lib_lightgbm动态库进行高效预测。支持从模型文件加载和字符串加载两种方式。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| IsModelLoaded | Boolean | 是 | 模型是否加载成功 |

## 常量

| 常量名 | 值 | 描述 |
|--------|-----|------|
| ModelPath | `D:\SouXiaoAVE\AIModel\lightgbm_model.txt` | 模型文件路径 |

## 构造函数

### LightGbmPredictor(Int32 featureCount = 512)

创建预测器实例。

**参数**:
- `featureCount`: Int32 - 特征维度数，默认512

**内部流程**:
1. 存储特征维度
2. 初始化booster句柄为空
3. 调用LoadModel加载模型

```csharp
LightGbmPredictor predictor = new(512);
Console.WriteLine($"模型加载: {predictor.IsModelLoaded}");
```

## 方法

### Predict(Single[] features)

执行预测，返回原始分数。

**参数**:
- `features`: Single[] - 特征数组（必须为512维）

**输出**: Single[] - 预测结果数组（通常为单值）

**内部流程**:
1. 检查模型是否加载
2. 检查特征维度是否匹配
3. 调用LGBM_BoosterPredictForMat
4. 返回预测结果

**异常情况**:
- 模型未加载时返回 [0.5f]
- 特征维度不匹配时返回 [0.5f]
- 预测失败时返回 [0.5f]

```csharp
Single[] features = new Single[512];
// ... 填充特征 ...
Single[] result = predictor.Predict(features);
Console.WriteLine($"原始分数: {result[0]}");
```

### PredictWithLabel(Single[] features)

执行预测并返回带标签的结果。

**参数**:
- `features`: Single[] - 特征数组

**输出**: (Double Score, Double Probability, String Label) - 元组包含：
- `Score`: 原始预测分数
- `Probability`: Sigmoid转换后的概率值
- `Label`: "Malicious"（概率>=0.5）或 "Benign"

**内部流程**:
1. 调用Predict获取原始分数
2. 使用Sigmoid函数转换为概率
3. 根据概率阈值确定标签

```csharp
(Double score, Double probability, String label) = predictor.PredictWithLabel(features);

Console.WriteLine($"分数: {score:F4}");
Console.WriteLine($"概率: {probability:P2}");
Console.WriteLine($"标签: {label}");
```

### Dispose()

释放资源。

**内部流程**:
1. 检查是否已释放
2. 调用LGBM_BoosterFree释放booster
3. 标记已释放

```csharp
predictor.Dispose();
```

## P/Invoke 声明

### LGBM_BoosterCreateFromModelfile

从模型文件创建Booster。

```csharp
[DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
private static extern Int32 LGBM_BoosterCreateFromModelfile(
    [MarshalAs(UnmanagedType.LPStr)] String filename,
    out Int32 outNumIterations,
    out IntPtr outBooster);
```

### LGBM_BoosterLoadModelFromString

从模型字符串创建Booster。

```csharp
[DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
private static extern Int32 LGBM_BoosterLoadModelFromString(
    [MarshalAs(UnmanagedType.LPStr)] String modelStr,
    out Int32 outNumIterations,
    out IntPtr outBooster);
```

### LGBM_BoosterPredictForMat

执行矩阵预测。

```csharp
[DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
private static extern Int32 LGBM_BoosterPredictForMat(
    IntPtr booster,
    Single[] data,
    Int32 data_type,
    Int32 nrow,
    Int32 ncol,
    Int32 is_row_major,
    Int32 predict_type,
    Int32 start_iteration,
    Int32 num_iteration,
    [MarshalAs(UnmanagedType.LPStr)] String parameter,
    ref Int64 out_len,
    Single[] out_result);
```

### LGBM_BoosterFree

释放Booster。

```csharp
[DllImport("lib_lightgbm", CallingConvention = CallingConvention.Cdecl)]
private static extern Int32 LGBM_BoosterFree(IntPtr booster);
```

## 使用示例

### 基本预测

```csharp
using LightGbmPredictor predictor = new(512);

if (!predictor.IsModelLoaded)
{
    Console.WriteLine("模型加载失败");
    return;
}

// 准备特征
Single[] features = new Single[512];
for (Int32 i = 0; i < 512; i++)
{
    features[i] = (Single)Random.Shared.NextDouble();
}

// 预测
(Double score, Double probability, String label) = predictor.PredictWithLabel(features);

Console.WriteLine($"预测结果: {label}");
Console.WriteLine($"恶意概率: {probability:P2}");
```

### 与PeFeatureExtractor集成

```csharp
using LightGbmPredictor predictor = new(512);
PeFeatureExtractor extractor = new();

Byte[] fileBytes = File.ReadAllBytes(@"C:\test.exe");
FeatureVector features = extractor.Extract(fileBytes);

// 转换为Single数组
Single[] featureArray = new Single[FeatureVector.TotalDimensions];
for (Int32 i = 0; i < features.Features.Length; i++)
{
    featureArray[i] = (Single)features.Features[i];
}

// 预测
var result = predictor.PredictWithLabel(featureArray);
Console.WriteLine($"文件分析结果: {result.Label} ({result.Probability:P2})");
```

### 批量预测

```csharp
using LightGbmPredictor predictor = new(512);
PeFeatureExtractor extractor = new();

String[] files = Directory.GetFiles(@"C:\Samples", "*.exe");

foreach (String file in files)
{
    try
    {
        FeatureVector features = extractor.Extract(file);
        Single[] arr = features.Features.Select(f => (Single)f).ToArray();
        
        var result = predictor.PredictWithLabel(arr);
        
        Console.WriteLine($"{Path.GetFileName(file)}: {result.Label} ({result.Probability:P2})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{Path.GetFileName(file)}: 错误 - {ex.Message}");
    }
}
```

## 错误处理

```csharp
LightGbmPredictor predictor = new(512);

// 检查模型加载
if (!predictor.IsModelLoaded)
{
    // 可能原因：
    // 1. 模型文件不存在
    // 2. 模型文件格式错误
    // 3. lib_lightgbm.dll 未找到
    Console.WriteLine("警告: 模型未加载，预测将返回默认值");
}

// 预测时检查维度
Single[] wrongFeatures = new Single[256]; // 错误维度
Single[] result = predictor.Predict(wrongFeatures);
// 返回 [0.5f] 而不是抛出异常
```

## 注意事项

- 需要lib_lightgbm.dll在系统PATH或应用程序目录
- 模型文件路径硬编码，生产环境应配置化
- 预测失败时返回默认值而非抛出异常
- 使用 `using` 语句确保资源释放
- 特征维度必须在构造时指定且与模型匹配
- Sigmoid函数用于将原始分数转换为概率
