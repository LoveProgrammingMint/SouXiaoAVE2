# Developer Documentation Index

Welcome! This is the API documentation index prepared for you. All detailed documentation has been written. Please follow the guide below to find the data you need.

---

## Quick Start

### 1. Analyze Single PE File

```bash
cd d:\SouXiaoAVE
dotnet run --project SouXiaoAVE -- --analyze "C:\test.exe"
```

Output:
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

### 2. Start TCP Service

```bash
dotnet run --project SouXiaoAVE
```

Connect via TCP client:
```
telnet localhost 9527
> PING
< PONG
> ANALYZE|C:\test.exe
< OK|Prediction=-7.70|Label=Benign
```

### 3. Batch Feature Extraction

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
Database path (default: D:\Dataset\IceZero\ZeroflowDataset.db): [Enter]

[INFO] Processing: C:\Samples
[EXTRACT] sample1.exe (245.8 KB) ... [OK]
[EXTRACT] sample2.exe (128.5 KB) ... [OK]
...
```

### 4. Clean Archive (Remove Non-PE Files)

```bash
dotnet run --project FeatureExtractor
```

```
Mode (1/2): 2
Input archive path (7z/zip/rar/tar/gz): C:\samples.7z
Password (leave empty if no password): [Enter]
Output path (leave empty to auto-generate): [Enter]

[INFO] Found 250 PE files, removing 150 non-PE files
[OK] Archive created successfully (no password)

=== Clean Complete ===
Total files scanned: 400
PE files kept: 250
Non-PE removed: 150
Original size: 125.3 MB
Cleaned size:  78.5 MB
```

### 5. Train LightGBM Model

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

### 6. Use SXAVELinker in Your Code

```csharp
using SXAVELinker;

// Create workflow engine
await using SXLinker linker = new();

// Register custom function
linker.RegisterFunction("CustomAnalyze", async (task, ct) =>
{
    SXReport report = new(task.ID, task.Name);
    // Your analysis logic here
    report.SetSuccess();
    return report;
});

// Create and execute workflow
String workflowId = linker.CreateWorkflow("MyWorkflow", "LoadPE", "ExtractFeatures", "PredictMalware");
List<SXReport> reports = await linker.ExecuteWorkflowAsync(workflowId);
```

---

## Documentation Structure

```
Docs/
├── Chat.md                    # This file (Index)
├── namespace_tree.json        # Namespace tree data (JSON format)
├── SXAVELinker/               # SXAVELinker project documentation
│   ├── SXType.md
│   ├── SXData.md
│   ├── SXTaskID.md
│   ├── SXReport.md
│   ├── SXTask.md
│   ├── SXObject.md
│   └── SXLinker.md
├── SouXiaoAVE/                # SouXiaoAVE project documentation
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
└── Example/                   # Example project documentation
    └── Program.md
```

---

## Namespace Tree

**File**: `namespace_tree.json`

This is a JSON-formatted tree structure containing:
- Namespace hierarchy
- Type definitions (classes, enums, static classes)
- File locations
- Brief descriptions

---

## Documentation Format

Each type documentation contains the following sections:

### 1. Header Information
```
**Namespace**: xxx
**File**: xxx.cs
**Type**: Class/Enum/Static Class
**Base Class/Interface**: xxx
```

### 2. Overview
Description of the type's purpose and responsibilities.

### 3. Properties Table
| Property | Type | ReadOnly | Description |
|----------|------|----------|-------------|

### 4. Methods Details
Each method includes:
- **Parameters**: Input parameter descriptions
- **Internal Flow**: Execution steps
- **Output**: Return value description
- **Exceptions**: Possible exceptions
- **Code Example**: Usage example

### 5. Usage Examples
Complete code examples.

### 6. Notes
Important considerations when using.

---

## Type Quick Index

### SXAVELinker (Core Library)

| Type | File | Description |
|------|------|-------------|
| [SXType](SXAVELinker/SXType.md) | Enum | Data type enumeration |
| [SXData](SXAVELinker/SXData.md) | Class | Byte data container (supports compression) |
| [SXTaskID](SXAVELinker/SXTaskID.md) | Class | Unique task identifier |
| [SXReport](SXAVELinker/SXReport.md) | Class | Task execution report |
| [SXTask](SXAVELinker/SXTask.md) | Class | Executable task unit |
| [SXObject](SXAVELinker/SXObject.md) | Class | Hierarchical object container |
| [SXLinker](SXAVELinker/SXLinker.md) | Class | Core workflow engine |

### SouXiaoAVE (Services)

| Type | File | Description |
|------|------|-------------|
| [Program](SouXiaoAVE/Program.md) | Class | Application entry point |
| [Worker](SouXiaoAVE/Worker.md) | Class | Background service |
| [FeatureVector](SouXiaoAVE/Models/FeatureVector.md) | Class | 512-dimensional feature vector |
| [TcpServer](SouXiaoAVE/Services/TcpServer.md) | Class | TCP server |
| [WorkflowEngine](SouXiaoAVE/Services/WorkflowEngine.md) | Class | Workflow engine wrapper |
| [LightGbmPredictor](SouXiaoAVE/Services/LightGbmPredictor.md) | Class | LightGBM predictor |
| [PeFeatureExtractor](SouXiaoAVE/Services/PeFeatureExtractor.md) | Class | PE feature extractor |
| [AnalysisReportContext](SouXiaoAVE/Services/AnalysisReportContext.md) | Class | JSON serialization context |
| [EntropyCalculator](SouXiaoAVE/Utils/EntropyCalculator.md) | Static Class | Entropy calculator |
| [MurmurHash3](SouXiaoAVE/Utils/MurmurHash3.md) | Static Class | Hash algorithm |

### Example (Examples)

| Type | File | Description |
|------|------|-------------|
| [Program](Example/Program.md) | Class | Example program |

---

## Data Display Suggestions

### Navigation Tree
Use `namespace_tree.json` to build left navigation tree:
```
SouXiaoAVE (Solution)
├── SXAVELinker (Namespace)
│   ├── SXType (Enum)
│   ├── SXData (Class)
│   └── ...
├── SouXiaoAVE (Namespace)
│   ├── Models/
│   ├── Services/
│   └── Utils/
└── Example (Namespace)
```

### Type Detail Page
Each type has a detail page showing:
1. Type information card (namespace, type, inheritance)
2. Properties list (table format)
3. Methods list (collapsible)
4. Code examples (syntax highlighting)

### Search Functionality
Can build index based on:
- Type names
- Method names
- Property names
- Description text

---

## Special Data

### Feature Dimension Distribution

512-dimensional feature distribution extracted by PeFeatureExtractor:

| Dimension Range | Feature Group | Dimensions |
|-----------------|---------------|------------|
| 0-36 | Global Statistics | 37 |
| 37-56 | Optional Header | 20 |
| 57-296 | Section Table (20×12) | 240 |
| 297-376 | Import Table | 80 |
| 377-381 | Export Table | 5 |
| 382-386 | Resources | 5 |
| 387-421 | Strings | 35 |
| 422-431 | Entry Point | 10 |
| 432-441 | Relocations/TLS | 10 |
| 442-461 | Byte Statistics | 20 |
| 462-511 | Supplementary | 50 |

### Workflow Data Flow

```
LoadPE → ExtractFeatures → PredictMalware → GenerateReport
   │            │                │                │
   ▼            ▼                ▼                ▼
SXData      features[]       Label/Score      JSON Report
FileName    FeatureCount     Probability
FileSize    Sparsity
```

### TCP Protocol Format

Request: `COMMAND|PARAM1|PARAM2|...`

| Command | Parameters | Response |
|---------|------------|----------|
| PING | None | PONG |
| STATUS | None | OK\|Service running |
| ANALYZE | File path | OK\|Prediction=X\|Label=Y |

### Model Performance

| Metric | Value |
|--------|-------|
| Accuracy | 99.85% |
| Precision | 99.82% |
| Recall | 99.94% |
| F1 Score | 99.88% |
| AUC-ROC | 1.0000 |

Training data: 36,266 samples (22,648 malicious, 13,618 benign)

---

## Contact

For documentation issues or additional information, please contact:
- Developer: LinduCMint
- Email: 3327867352@qq.com
- License: MINT License

---
