# SouXiaoAVE - PE Malware Detection Engine

A high-performance PE file malware detection engine based on static analysis and LightGBM machine learning.

## Features

- **512-Dimension Feature Extraction**: Comprehensive static analysis of PE files
- **LightGBM Classification**: 99.85% accuracy malware detection
- **Workflow Engine**: Flexible task-based processing pipeline
- **TCP Service**: Multi-client network analysis service
- **Batch Processing**: Directory and archive (7z/zip/rar) support

## Project Structure

```
SouXiaoAVE/
├── SXAVELinker/          # Core workflow engine library
├── SouXiaoAVE/           # TCP service + analysis functions
├── FeatureExtractor/     # Batch feature extraction tool
├── Example/              # Usage example project
├── AIModel/              # Model training scripts
└── Docs/                 # API documentation
```

## Requirements

- .NET 10.0 SDK
- LightGBM native library (lib_lightgbm.dll)
- Python 3.10+ (for model training)

## Quick Start

### 1. Analyze Single File

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

Connect via telnet:
```
telnet localhost 9527
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
Input path: C:\Samples
Input label: malicious
Database path: D:\Dataset\IceZero\ZeroflowDataset.db
```

### 4. Train Model

```bash
cd AIModel
python train_model.py
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        SouXiaoAVE                           │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────────────────────────────┐ │
│  │  TcpServer  │    │          WorkflowEngine             │ │
│  │  (Port 9527)│    │  ┌─────────┐ ┌─────────┐ ┌────────┐ │ │
│  └─────────────┘    │  │ LoadPE  │→│Extract  │→│Predict │ │ │
│         │           │  │         │ │Features │ │Malware │ │ │
│         ▼           │  └─────────┘ └─────────┘ └────────┘ │ │
│  ┌─────────────┐    │       │           │           │     │ │
│  │   Worker    │    │       ▼           ▼           ▼     │ │
│  │  Background │    │  Byte[]    Double[512]   Label/Score│ │
│  │   Service   │    └─────────────────────────────────────┘ │
│  └─────────────┘                                            │
└─────────────────────────────────────────────────────────────┘
```

## Feature Extraction (512 Dimensions)

| Group | Dimensions | Description |
|-------|------------|-------------|
| Global Statistics | 37 | File size, entropy, compression ratio |
| Optional Header | 20 | PE header fields |
| Sections | 240 | 20 sections × 12 features each |
| Imports | 80 | 30 DLLs + 50 APIs detection |
| Exports | 5 | Export table analysis |
| Resources | 5 | Resource section analysis |
| Strings | 35 | String patterns and statistics |
| Entry Point | 10 | Entry point disassembly |
| Relocations/TLS | 10 | Relocation and TLS analysis |
| Byte Statistics | 20 | Byte distribution |
| Supplementary | 50 | Additional features |

## Model Performance

| Metric | Value |
|--------|-------|
| Accuracy | 99.85% |
| Precision | 99.82% |
| Recall | 99.94% |
| F1 Score | 99.88% |
| AUC-ROC | 1.0000 |

Training data: 36,266 samples (22,648 malicious, 13,618 benign)

## TCP Protocol

| Command | Response | Description |
|---------|----------|-------------|
| `PING` | `PONG` | Heartbeat |
| `STATUS` | `OK\|Service running` | Service status |
| `ANALYZE\|path` | `OK\|Prediction=X\|Label=Y` | Analyze PE file |

## API Documentation

See [Docs/Chat.md](Docs/Chat.md) for complete API documentation.

### SXAVELinker Types

| Type | Description |
|------|-------------|
| SXType | Data type enumeration |
| SXData | Byte data container (with compression) |
| SXTaskID | Unique task identifier |
| SXReport | Task execution report |
| SXTask | Executable task unit |
| SXObject | Hierarchical object container |
| SXLinker | Core workflow engine |

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| PeNet | 6.0.* | PE parsing |
| Gee.External.Capstone | 2.3.0 | Disassembly |
| LightGBM | 4.6.0 | Model inference |
| Microsoft.Data.Sqlite | 10.0.5 | SQLite storage |
| SharpCompress | 0.39.0 | Archive handling |

## License

MINT License - Contact <3327867352@qq.com> for authorization.

## Author

LinduCMint - 2026
