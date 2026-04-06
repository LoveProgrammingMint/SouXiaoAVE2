# Program 类

**命名空间**: `SouXiaoAVE`  
**文件**: `SouXiaoAVE/Program.cs`  
**类型**: 类 (Class)

## 概述

应用程序入口点，支持两种运行模式：命令行分析模式和服务模式。

## 方法

### Main(String[] args)

应用程序主入口。

**参数**:
- `args`: String[] - 命令行参数

**内部流程**:
1. 检查是否为分析模式（`--analyze` 参数）
2. 分析模式：调用 `RunAnalysisAsync` 执行单次分析
3. 服务模式：创建并运行Worker Service主机

```csharp
// 分析模式
dotnet run --analyze C:\test.exe

// 服务模式
dotnet run
```

### RunAnalysisAsync(String filePath)

执行单次PE文件分析。

**参数**:
- `filePath`: String - 要分析的文件路径

**内部流程**:
1. 创建WorkflowEngine实例
2. 执行分析工作流
3. 输出每个任务的报告

**输出**: Task - 异步任务

```csharp
// 命令行输出示例:
// === SouXiaoAVE PE Analysis ===
// File: C:\test.exe
//
// === Analysis Results ===
// === Task Report: LoadPE ===
// ID: a1b2c3d4...
// Status: Success
// Duration: 15.23ms
// Results:
//   FileName: test.exe
//   FileSize: 245760
// ...
```

## 使用示例

### 命令行分析

```bash
# 分析单个文件
dotnet run --analyze "C:\Windows\System32\notepad.exe"

# 分析恶意软件样本
dotnet run --analyze "C:\Samples\suspicious.exe"
```

### 服务模式

```bash
# 启动TCP服务（默认端口9527）
dotnet run

# 作为Windows服务运行
sc create SouXiaoAVE binPath="C:\path\to\SouXiaoAVE.exe"
```

### 程序化调用

```csharp
// 直接调用Main方法（不推荐，建议使用WorkflowEngine）
String[] args = ["--analyze", "C:\\test.exe"];
await Program.Main(args);
```

## 依赖注入配置

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// 注册单例服务
builder.Services.AddSingleton<PeFeatureExtractor>();
builder.Services.AddSingleton<WorkflowEngine>();

// 注册后台服务
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
await host.RunAsync();
```

## 运行模式对比

| 特性 | 分析模式 | 服务模式 |
|------|----------|----------|
| 启动参数 | `--analyze <path>` | 无 |
| 执行方式 | 单次分析后退出 | 持续运行 |
| 输出目标 | 控制台 | TCP客户端 |
| 适用场景 | 调试、脚本调用 | 生产环境 |
| 并发支持 | 无 | 多客户端 |

## 注意事项

- 分析模式适合调试和脚本集成
- 服务模式使用.NET Worker Service框架
- 依赖注入确保组件生命周期管理
- 使用 `await using` 确保资源释放
