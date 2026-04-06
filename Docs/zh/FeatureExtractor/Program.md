# Program

**命名空间**: FeatureExtractor
**文件**: Program.cs
**类型**: 类
**描述**: PE特征批量提取工具，支持多线程处理

---

## 概述

`Program` 是 FeatureExtractor 项目的核心类，提供 PE 文件特征批量提取功能。支持从单个文件、目录和压缩包中提取 512 维特征向量，并将结果存储到 SQLite 数据库。同时提供压缩包清理功能，可移除非 PE 文件。

---

## 常量

| 常量 | 类型 | 值 | 描述 |
|------|------|-----|------|
| DefaultDbPath | String | D:\Dataset\IceZero\ZeroflowDataset.db | 默认数据库路径 |
| FeatureCount | Int32 | 512 | 特征维度数量 |
| MaxDegreeOfParallelism | Int32 | 8 | 最大并行线程数 |

---

## 静态字段

| 字段 | 类型 | 描述 |
|------|------|------|
| _extractor | PeFeatureExtractor | PE 特征提取器实例 |
| _consoleLock | Object | 控制台输出锁 |
| _dbPath | String | 当前使用的数据库路径 |

---

## 主要方法

### Main

程序入口点。

**参数**:
- `args`: 命令行参数

**返回**: 退出代码 (0=成功, 1=失败)

**代码示例**:
```bash
dotnet run --project FeatureExtractor
```

---

### RunAsync

运行主程序逻辑，选择模式。

**内部流程**:
1. 显示模式选择菜单
2. 根据用户输入调用相应处理方法

---

### RunExtractModeAsync

运行特征提取模式。

**内部流程**:
1. 获取输入路径（文件/目录/压缩包）
2. 如果是压缩包，提示输入密码
3. 获取标签（malicious/benign/unknown）
4. 获取数据库路径
5. 初始化数据库表结构
6. 根据输入类型调用相应处理方法
7. 显示处理统计结果

**代码示例**:
```
Select mode:
  1. Extract - Extract features to database
  2. Clean   - Clean archive (remove non-PE files)

Mode (1/2): 1
Input path (PE file / directory / 7z archive): C:\Samples
Input label (malicious/benign/unknown): malicious
Database path (default: D:\Dataset\IceZero\ZeroflowDataset.db): [回车]

[INFO] Processing: C:\Samples
[INFO] Label: malicious
[INFO] Database: D:\Dataset\IceZero\ZeroflowDataset.db

[PROGRESS] 100/500 - Success: 85, Skipped: 10, Errors: 5
...

=== Processing Complete ===
Total files scanned: 500
PE files found: 450
Successfully extracted: 440
Skipped (non-PE): 50
Errors: 10
```

---

### RunCleanModeAsync

运行压缩包清理模式。

**内部流程**:
1. 获取压缩包路径
2. 验证压缩包格式（7z/zip/rar/tar/gz）
3. 提示输入密码（如有）
4. 获取输出路径
5. 调用清理方法
6. 显示清理统计结果

**代码示例**:
```
Mode (1/2): 2
Input archive path (7z/zip/rar/tar/gz): C:\samples.7z
Password (leave empty if no password): [回车]
Output path (leave empty to auto-generate): [回车]

[INFO] Input: C:\samples.7z
[INFO] Output: C:\samples_cleaned.zip
[INFO] Password: None

[INFO] Found 250 PE files, removing 150 non-PE files
[INFO] Creating cleaned archive: C:\samples_cleaned.zip
[INFO] Archive created successfully (no password)

=== Clean Complete ===
Total files scanned: 400
PE files kept: 250
Non-PE removed: 150
Errors: 0

Original size: 125.3 MB
Cleaned size:  78.5 MB
Saved: 46.8 MB (62.6% of original)
```

---

### ProcessDirectoryParallelAsync

并行处理目录中的所有文件。

**参数**:
- `directoryPath`: 目录路径
- `label`: 样本标签
- `stats`: 处理统计对象

**内部流程**:
1. 枚举目录下所有文件
2. 使用 Parallel.ForEach 并行处理
3. 每个文件调用 ProcessSingleFileSync
4. 每 100 个文件输出进度

---

### ProcessArchiveParallelAsync

并行处理压缩包中的所有文件。

**参数**:
- `archivePath`: 压缩包路径
- `label`: 样本标签
- `password`: 压缩包密码（可选）
- `stats`: 处理统计对象

**内部流程**:
1. 打开压缩包
2. 使用 Parallel.ForEach 并行提取
3. 过滤非 PE 文件
4. 批量保存到数据库

---

### CleanArchiveAsync

清理压缩包，移除非 PE 文件。

**参数**:
- `inputPath`: 输入压缩包路径
- `outputPath`: 输出压缩包路径
- `password`: 压缩包密码（可选）
- `stats`: 清理统计对象

**内部流程**:
1. 打开源压缩包
2. 并行提取所有条目
3. 过滤保留 PE 文件
4. 创建新的无密码 ZIP 压缩包

---

### 辅助方法

| 方法 | 描述 |
|------|------|
| ReadPassword | 安全读取密码（显示*号） |
| InitializeDatabaseAsync | 初始化数据库表结构 |
| ProcessSingleFileSync | 同步处理单个文件 |
| ProcessSingleFileAsync | 异步处理单个文件 |
| IsValidPeFile | 验证是否为有效 PE 文件 |
| ComputeSha256 | 计算 SHA256 哈希 |
| ExtractFeatures | 提取 512 维特征 |
| IsDuplicateSync | 检查是否重复样本 |
| SaveToDatabaseSync | 保存到数据库 |
| LogInfo | 输出信息日志 |
| LogError | 输出错误日志 |
| LogProgress | 输出进度日志 |
| FormatSize | 格式化文件大小 |

---

## 数据库结构

### samples 表

| 字段 | 类型 | 描述 |
|------|------|------|
| id | INTEGER | 主键（自增） |
| sha256 | TEXT | SHA256 哈希（唯一） |
| file_name | TEXT | 文件名 |
| file_size | INTEGER | 文件大小 |
| label | TEXT | 标签（malicious/benign/unknown） |
| extracted_at | TEXT | 提取时间 |
| source_path | TEXT | 源路径 |

### features 表

| 字段 | 类型 | 描述 |
|------|------|------|
| sample_id | INTEGER | 样本ID（外键） |
| feature_0 ~ feature_511 | REAL | 512 维特征值 |

---

## 支持的压缩格式

| 格式 | 扩展名 | 密码支持 |
|------|--------|----------|
| 7-Zip | .7z | 是 |
| ZIP | .zip | 是 |
| RAR | .rar | 是 |
| TAR | .tar | 否 |
| GZIP | .gz | 否 |

---

## 注意事项

1. **多线程安全**: 使用 ConcurrentBag 和锁保证线程安全
2. **重复检测**: 通过 SHA256 哈希检测重复样本
3. **内存管理**: 大文件处理时注意内存使用
4. **错误处理**: 单个文件错误不影响整体处理
5. **AOT 兼容**: 支持 AOT 编译发布

---

## 使用示例

### 批量提取目录特征

```bash
dotnet run --project FeatureExtractor
```

```
Mode (1/2): 1
Input path: C:\MalwareSamples
Input label: malicious
Database path: [回车使用默认]
```

### 处理加密压缩包

```bash
dotnet run --project FeatureExtractor
```

```
Mode (1/2): 1
Input path: C:\protected.7z
Password: ********
Input label: malicious
```

### 清理压缩包

```bash
dotnet run --project FeatureExtractor
```

```
Mode (1/2): 2
Input archive path: C:\samples.7z
Password: [回车]
Output path: [回车自动生成]
```
