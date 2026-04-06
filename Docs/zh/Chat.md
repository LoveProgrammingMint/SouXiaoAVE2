# 开发者文档索引

欢迎！这是为您准备的API文档索引。所有详细文档已编写完成，请根据以下指引查找所需数据。

---

## 快速开始

### 1. 分析单个PE文件

```bash
cd d:\SouXiaoAVE
dotnet run --project SouXiaoAVE -- --analyze "C:\test.exe"
```

输出示例：
```
=== SouXiaoAVE PE Analysis ===
File: C:\test.exe

=== Analysis Results ===
=== Task Report: PEAnalysis_PredictMalware_2 ===
Results:
  Score: -7.70
  Probability: 0.0005
  Label: Benign
  IsMalicious: False
```

### 2. 启动TCP服务

```bash
dotnet run --project SouXiaoAVE
```

通过TCP客户端连接：
```
telnet localhost 9527
> PING
< PONG
> ANALYZE|C:\test.exe
< OK|Prediction=-7.70|Label=Benign
```

### 3. 批量特征提取

```bash
dotnet run --project FeatureExtractor
```

```
Select mode:
  1. Extract - Extract features to database
  2. Clean   - Clean archive (remove non-PE files)

Mode (1/2): 1
Input path (PE file / directory / 7z archive): C:\Samples
Input label (malicious/benign/unknown): malicious
Database path (default: D:\Dataset\IceZero\ZeroflowDataset.db): [回车]

[INFO] Processing: C:\Samples
[EXTRACT] sample1.exe (245.8 KB) ... [OK]
[EXTRACT] sample2.exe (128.5 KB) ... [OK]
...
```

### 4. 清理压缩包（移除非PE文件）

```bash
dotnet run --project FeatureExtractor
```

```
Mode (1/2): 2
Input archive path (7z/zip/rar/tar/gz): C:\samples.7z
Password (leave empty if no password): [回车]
Output path (leave empty to auto-generate): [回车]

[INFO] Found 250 PE files, removing 150 non-PE files
[OK] Archive created successfully (no password)

=== Clean Complete ===
Total files scanned: 400
PE files kept: 250
Non-PE removed: 150
Original size: 125.3 MB
Cleaned size:  78.5 MB
```

### 5. 训练LightGBM模型

```bash
cd AIModel
python train_model.py
```

```
=== Model Performance ===
Accuracy:  0.9985
Precision: 0.9982
Recall:    0.9994
F1 Score:  0.9988
AUC-ROC:   1.0000

[INFO] Model saved successfully!
```

### 6. 在代码中使用SXAVELinker

```csharp
using SXAVELinker;

// 创建工作流引擎
await using SXLinker linker = new();

// 注册自定义函数
linker.RegisterFunction("CustomAnalyze", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    // 您的分析逻辑
    report.SetSuccess();
    return report;
});

// 创建并执行工作流
String workflowId = linker.CreateWorkflow("MyWorkflow", "LoadPE", "ExtractFeatures", "PredictMalware");
List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);
```

---

## 文档结构

```
Docs/zh/
├── Chat.md                    # 本文件（索引）
├── SXAVELinker/               # SXAVELinker项目文档
│   ├── SXType.md
│   ├── SXData.md
│   ├── SXTaskID.md
│   ├── SXReport.md
│   ├── SXTask.md
│   ├── SXObject.md
│   └── SXLinker.md
├── SouXiaoAVE/                # SouXiaoAVE项目文档
│   ├── Program.md
│   ├── Worker.md
│   ├── Models/
│   │   └── FeatureVector.md
│   ├── Services/
│   │   ├── TcpServer.md
│   │   ├── WorkflowEngine.md
│   │   ├── LightGbmPredictor.md
│   │   ├── PeFeatureExtractor.md
│   │   └── AnalysisReportContext.md
│   └── Utils/
│       ├── EntropyCalculator.md
│       └── MurmurHash3.md
├── FeatureExtractor/          # FeatureExtractor项目文档
│   └── Program.md
└── Example/                   # Example项目文档
    └── Program.md
```

---

## 文档格式

每个类型文档包含以下章节：

### 1. 头部信息
```
**命名空间**: xxx
**文件**: xxx.cs
**类型**: 类/枚举/静态类
**基类/接口**: xxx
```

### 2. 概述
类型用途和职责描述。

### 3. 属性表
| 属性 | 类型 | 只读 | 描述 |
|------|------|------|------|

### 4. 方法详情
每个方法包含：
- **参数**: 输入参数描述
- **内部流程**: 执行步骤
- **输出**: 返回值描述
- **异常**: 可能的异常
- **代码示例**: 使用示例

### 5. 使用示例
完整代码示例。

### 6. 注意事项
使用时的重要注意事项。

---

## 数据展示建议

### 导航树
使用 `namespace_tree.json` 构建左侧导航树：
```
SouXiaoAVE (解决方案)
├── SXAVELinker (命名空间)
│   ├── SXType (枚举)
│   ├── SXData (类)
│   └── ...
├── SouXiaoAVE (命名空间)
│   ├── Models/
│   ├── Services/
│   └── Utils/
├── FeatureExtractor (命名空间)
│   └── Program
└── Example (命名空间)
```

### 类型详情页
每个类型有详情页展示：
1. 类型信息卡片（命名空间、类型、继承）
2. 属性列表（表格格式）
3. 方法列表（可折叠）
4. 代码示例（语法高亮）

### 搜索功能
可基于以下构建索引：
- 类型名称
- 方法名称
- 属性名称
- 描述文本

---

## 类型快速索引

### SXAVELinker（核心库）

| 类型 | 文件 | 描述 |
|------|------|------|
| [SXType](SXAVELinker/SXType.md) | 枚举 | 数据类型枚举 |
| [SXData](SXAVELinker/SXData.md) | 类 | 字节数据容器（支持压缩） |
| [SXTaskID](SXAVELinker/SXTaskID.md) | 类 | 任务唯一标识 |
| [SXReport](SXAVELinker/SXReport.md) | 类 | 任务执行报告 |
| [SXTask](SXAVELinker/SXTask.md) | 类 | 可执行任务单元 |
| [SXObject](SXAVELinker/SXObject.md) | 类 | 层级对象容器 |
| [SXLinker](SXAVELinker/SXLinker.md) | 类 | 核心工作流引擎 |

### SouXiaoAVE（服务）

| 类型 | 文件 | 描述 |
|------|------|------|
| [Program](SouXiaoAVE/Program.md) | 类 | 应用程序入口 |
| [Worker](SouXiaoAVE/Worker.md) | 类 | 后台服务 |
| [FeatureVector](SouXiaoAVE/Models/FeatureVector.md) | 类 | 512维特征向量 |
| [TcpServer](SouXiaoAVE/Services/TcpServer.md) | 类 | TCP服务器 |
| [WorkflowEngine](SouXiaoAVE/Services/WorkflowEngine.md) | 类 | 工作流引擎封装 |
| [LightGbmPredictor](SouXiaoAVE/Services/LightGbmPredictor.md) | 类 | LightGBM预测器 |
| [PeFeatureExtractor](SouXiaoAVE/Services/PeFeatureExtractor.md) | 类 | PE特征提取器 |
| [AnalysisReportContext](SouXiaoAVE/Services/AnalysisReportContext.md) | 类 | JSON序列化上下文 |
| [EntropyCalculator](SouXiaoAVE/Utils/EntropyCalculator.md) | 静态类 | 熵计算工具 |
| [MurmurHash3](SouXiaoAVE/Utils/MurmurHash3.md) | 静态类 | 哈希算法 |

### FeatureExtractor（特征提取工具）

| 类型 | 文件 | 描述 |
|------|------|------|
| [Program](FeatureExtractor/Program.md) | 类 | 特征提取工具（支持多线程） |
| ProcessingStats | 类 | 提取处理统计信息 |
| CleanStats | 类 | 压缩包清理统计信息 |
| ArchiveEntryData | 类 | 压缩包条目数据容器 |

### Example（示例）

| 类型 | 文件 | 描述 |
|------|------|------|
| [Program](Example/Program.md) | 类 | 示例程序 |

---

## 特殊数据

### 特征维度分布

PeFeatureExtractor提取的512维特征分布：

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

### 工作流数据流

```
LoadPE → ExtractFeatures → PredictMalware → GenerateReport
   │            │                │                │
   ▼            ▼                ▼                ▼
SXData      features[]       Label/Score      JSON报告
FileName    FeatureCount     Probability
FileSize    Sparsity
```

### TCP协议格式

请求格式: `命令|参数1|参数2|...`

| 命令 | 参数 | 响应 |
|------|------|------|
| PING | 无 | PONG |
| STATUS | 无 | OK\|Service running |
| ANALYZE | 文件路径 | OK\|Prediction=X\|Label=Y |

### 模型性能

| 指标 | 值 |
|------|-----|
| 准确率 | 99.85% |
| 精确率 | 99.82% |
| 召回率 | 99.94% |
| F1分数 | 99.88% |
| AUC-ROC | 1.0000 |

训练数据：36,266个样本（22,648恶意，13,618良性）

---

## 联系方式

如有文档问题或需要补充信息，请联系：
- 开发者：LinduCMint
- 邮箱：3327867352@qq.com
- 协议：MINT License

---
