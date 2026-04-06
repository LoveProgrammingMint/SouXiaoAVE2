# PeFeatureExtractor 类

**命名空间**: `SouXiaoAVE.Services`  
**文件**: `SouXiaoAVE/Services/PeFeatureExtractor.cs`  
**类型**: 密封类 (Sealed Class)

## 概述

PE文件静态特征提取器，从Windows可执行文件中提取512维特征向量，用于恶意软件检测机器学习模型。

## 常量

| 常量名 | 值 | 描述 |
|--------|-----|------|
| MaxSections | 20 | 最大处理节数量 |
| SegmentCount | 32 | 文件分段数量（用于熵计算） |

## 静态字段

### KeyDlls

关键DLL列表（30个），用于导入表特征提取。

```csharp
["kernel32.dll", "ntdll.dll", "user32.dll", "advapi32.dll", ...]
```

### KeyApis

关键API列表（50个），用于检测可疑函数调用。

```csharp
["CreateRemoteThread", "WriteProcessMemory", "VirtualAllocEx", ...]
```

### KeySubstrings

关键子串列表（30个），用于字符串特征检测。

```csharp
["cmd.exe", "powershell", "rundll32", "-enc", "Invoke-", ...]
```

### SuspiciousSectionNames

可疑节名称列表（15个），用于加壳检测。

```csharp
["UPX0", "UPX1", ".aspack", ".themida", ".vmp0", ...]
```

## 方法

### Extract(String filePath)

从文件路径提取特征。

**参数**:
- `filePath`: String - PE文件路径

**输出**: FeatureVector - 512维特征向量

**内部流程**:
1. 读取文件字节
2. 创建PeFile实例
3. 按顺序调用11个特征提取方法
4. 返回填充完成的特征向量

**可能的异常**:
- `FileNotFoundException`: 文件不存在
- `PeNet`解析异常
- 内存不足

```csharp
PeFeatureExtractor extractor = new();
FeatureVector features = extractor.Extract(@"C:\Windows\System32\notepad.exe");
Console.WriteLine($"特征维度: {features.Features.Length}"); // 512
```

### Extract(Byte[] fileBytes)

从字节数组提取特征。

**参数**:
- `fileBytes`: Byte[] - PE文件字节数据

**输出**: FeatureVector - 512维特征向量

```csharp
Byte[] bytes = File.ReadAllBytes("sample.exe");
FeatureVector features = extractor.Extract(bytes);
```

## 特征提取方法

### ExtractGlobalStatistics

提取全局统计特征（37维）。

**输出维度**: 0-36

| 索引 | 特征 | 描述 |
|------|------|------|
| 0 | FileSize | 文件大小 |
| 1 | Entropy | 全局熵值 |
| 2 | CompressionRatio | 压缩比 |
| 3-34 | SegmentEntropy[0-31] | 分段熵值 |
| 35-36 | 保留 | - |

```csharp
// 内部实现
features[offset++] = _fileBytes.Length;
features[offset++] = EntropyCalculator.Calculate(_fileBytes);
features[offset++] = CalculateCompressionRatio(_fileBytes);
// 32个分段熵...
```

### ExtractOptionalHeaderFeatures

提取可选头特征（20维）。

**输出维度**: 37-56

| 索引 | 特征 | 描述 |
|------|------|------|
| 37 | MajorLinkerVersion | 链接器主版本 |
| 38 | MinorLinkerVersion | 链接器次版本 |
| 39 | SizeOfCode | 代码节大小 |
| 40 | SizeOfInitializedData | 已初始化数据大小 |
| 41 | SizeOfUninitializedData | 未初始化数据大小 |
| 42 | AddressOfEntryPoint | 入口点地址 |
| 43 | BaseOfCode | 代码基址 |
| 44 | ImageBase | 映像基址 |
| 45 | SectionAlignment | 节对齐 |
| 46 | FileAlignment | 文件对齐 |
| 47-50 | OS/Subsystem版本 | 操作系统版本信息 |
| 51 | SizeOfImage | 映像大小 |
| 52 | SizeOfHeaders | 头大小 |
| 53 | Subsystem | 子系统类型 |
| 54 | DllCharacteristics | DLL特征 |
| 55 | SizeOfStackReserve | 栈保留大小 |

### ExtractSectionFeatures

提取节表特征（240维 = 20节 × 12特征）。

**输出维度**: 57-296

每节12个特征：

| 子索引 | 特征 | 描述 |
|--------|------|------|
| 0 | NameHash | 节名哈希（归一化） |
| 1 | VirtualSize | 虚拟大小 |
| 2 | SizeOfRawData | 原始数据大小 |
| 3 | Entropy | 节熵值 |
| 4 | IsExecutable | 可执行标志 |
| 5 | IsWritable | 可写标志 |
| 6 | ContainsCode | 包含代码标志 |
| 7 | ContainsInitData | 包含已初始化数据 |
| 8 | IsStandardName | 标准节名标志 |
| 9 | ValidPointer | 有效指针标志 |
| 10 | CharacteristicsLow | 特征低字节 |
| 11 | StringDensity | 字符串密度 |

### ExtractImportFeatures

提取导入表特征（80维 = 30 DLL + 50 API）。

**输出维度**: 297-376

| 范围 | 特征 | 描述 |
|------|------|------|
| 297-326 | DllCounts[0-29] | 关键DLL导入计数 |
| 327-376 | ApiFlags[0-49] | 关键API存在标志 |

### ExtractExportFeatures

提取导出表特征（5维）。

**输出维度**: 377-381

| 索引 | 特征 | 描述 |
|------|------|------|
| 377 | HasExports | 是否有导出表 |
| 378 | ExportCount | 导出函数数量 |
| 379 | NamedExportCount | 命名导出数量 |
| 380 | HasSuspiciousExport | 可疑导出标志 |
| 381 | ExportNameEntropy | 导出名熵值 |

### ExtractResourceFeatures

提取资源特征（5维）。

**输出维度**: 382-386

| 索引 | 特征 | 描述 |
|------|------|------|
| 382 | ResourceCount | 资源条目数 |
| 383 | ResourceTypeCount | 资源类型数 |
| 384 | AvgResourceEntropy | 平均资源熵 |
| 385 | MaxResourceEntropy | 最大资源熵 |
| 386 | HasPeResource | 嵌入PE标志 |

### ExtractStringFeatures

提取字符串特征（35维 = 5统计 + 30子串）。

**输出维度**: 387-421

| 范围 | 特征 | 描述 |
|------|------|------|
| 387 | StringCount | 字符串数量 |
| 388 | AvgStringLength | 平均字符串长度 |
| 389 | StringEntropy | 字符串熵值 |
| 390 | UrlCount | URL数量 |
| 391 | IpCount | IP地址数量 |
| 392-421 | SubstringFlags[0-29] | 关键子串存在标志 |

### ExtractEntryPointFeatures

提取入口点特征（10维）。

**输出维度**: 422-431

| 索引 | 特征 | 描述 |
|------|------|------|
| 422 | EntryPointSectionIndex | 入口点所在节索引 |
| 423 | EntryPointSectionEntropy | 入口节熵值 |
| 424 | EntryPointOffset | 入口点偏移 |
| 425 | EntryPointBytesEntropy | 入口字节熵 |
| 426 | PushCount | PUSH指令计数 |
| 427 | CallCount | CALL指令计数 |
| 428 | JmpCount | 跳转指令计数 |
| 429 | IsExecutableSection | 可执行节标志 |
| 430 | IsTextSection | .text节标志 |
| 431 | HasLegacyProlog | 传统序言标志 |

### ExtractRelocationTlsFeatures

提取重定位和TLS特征（10维）。

**输出维度**: 432-441

| 索引 | 特征 | 描述 |
|------|------|------|
| 432 | HasRelocations | 是否有重定位 |
| 433 | RelocationSize | 重定位表大小 |
| 434 | RelocationCount | 重定位条目数 |
| 435 | HasTls | 是否有TLS |
| 436 | TlsCallbackCount | TLS回调数 |
| 437 | HasExceptionDir | 是否有异常目录 |
| 438 | HasProcessInjection | 进程注入API标志 |
| 439 | HasNetworkApi | 网络API标志 |
| 440 | AlignmentMismatch | 对齐不匹配标志 |
| 441 | HasEmptySection | 空节标志 |

### ExtractByteStatistics

提取字节统计特征（20维）。

**输出维度**: 442-461

| 索引 | 特征 | 描述 |
|------|------|------|
| 442 | Mean | 字节均值 |
| 443 | Variance | 字节方差 |
| 444 | Skewness | 偏度 |
| 445 | Kurtosis | 峰度 |
| 446 | Median | 中位数 |
| 447 | Mode | 众数 |
| 448-457 | Top10Frequency | 前10高频字节频率 |
| 458 | Autocorrelation | 自相关系数 |

### ExtractSupplementaryFeatures

提取补充特征（50维）。

**输出维度**: 462-511

| 范围 | 特征 | 描述 |
|------|------|------|
| 462-471 | ApiCategoryCounts[0-9] | API类别计数 |
| 472-486 | SuspiciousSectionFlags[0-14] | 可疑节标志 |
| 487-501 | InstructionNgrams[0-14] | 指令N-gram特征 |
| 502-511 | TailFeatures[0-9] | 文件尾部特征 |

## 辅助方法

### GetSectionEntropy(ImageSectionHeader section)

计算节的熵值。

### IsStandardSectionName(String name)

检查是否为标准节名。

### CalculateStringDensity(ImageSectionHeader section)

计算节内字符串密度。

### FindSectionByRva(UInt32 rva, ImageSectionHeader[] sections)

根据RVA查找所属节。

### GetEpBytes(UInt32 epRva, Int32 count)

获取入口点字节。

### CountInstructions(Byte[] code)

统计指令数量（使用Capstone反汇编）。

### ExtractStrings(Byte[] data, Int32 minLength = 4)

提取可打印字符串。

### CalculateAutocorrelation(Byte[] data, Int32 lag)

计算自相关系数。

### ExtractInstructionNgrams(Byte[] code)

提取指令N-gram特征。

### ExtractTailFeatures()

提取文件尾部特征。

## 使用示例

### 基本提取

```csharp
PeFeatureExtractor extractor = new();

FeatureVector features = extractor.Extract(@"C:\test.exe");

Console.WriteLine($"特征维度: {features.Features.Length}");
Console.WriteLine($"文件大小: {features[0]}");
Console.WriteLine($"全局熵: {features[1]:F4}");
Console.WriteLine($"压缩比: {features[2]:F4}");
```

### 特征分析

```csharp
FeatureVector features = extractor.Extract(fileBytes);

// 计算稀疏度
Int32 zeroCount = features.Features.Count(f => f == 0);
Double sparsity = (Double)zeroCount / 512;
Console.WriteLine($"稀疏度: {sparsity:P2}");

// 查看节熵
for (Int32 i = 0; i < 5; i++)
{
    Int32 offset = 57 + i * 12 + 3; // 节熵在第4个位置
    Console.WriteLine($"节{i}熵: {features[offset]:F4}");
}

// 检查可疑API
Boolean hasInjection = features[438] > 0;
Boolean hasNetwork = features[439] > 0;
Console.WriteLine($"进程注入API: {hasInjection}");
Console.WriteLine($"网络API: {hasNetwork}");
```

### 批量处理

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

Console.WriteLine($"成功提取: {allFeatures.Count}/{files.Length}");
```

## 注意事项

- 依赖PeNet库解析PE结构
- 依赖Capstone库进行反汇编
- 大文件可能消耗较多内存
- 特征维度固定为512，与模型匹配
- 部分特征对加壳/混淆敏感
