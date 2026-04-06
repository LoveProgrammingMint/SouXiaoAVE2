# Worker 类

**命名空间**: `SouXiaoAVE`  
**文件**: `SouXiaoAVE/Worker.cs`  
**类型**: 密封类 (Sealed Class)  
**基类**: `BackgroundService`

## 概述

后台服务工作器，管理TCP服务器生命周期，处理客户端消息并执行PE文件分析。

## 依赖

通过构造函数注入：

| 参数 | 类型 | 描述 |
|------|------|------|
| logger | `ILogger<Worker>` | 日志记录器 |
| workflowEngine | WorkflowEngine | 工作流引擎 |
| featureExtractor | PeFeatureExtractor | 特征提取器 |

## 属性

| 属性名 | 类型 | 描述 |
|--------|------|------|
| _tcpServer | TcpServer? | TCP服务器实例 |

## 方法

### ExecuteAsync(CancellationToken stoppingToken)

后台服务主执行方法。

**参数**:
- `stoppingToken`: CancellationToken - 停止令牌

**内部流程**:
1. 记录服务启动日志
2. 创建TCP服务器（端口9527）
3. 订阅服务器事件
4. 启动TCP服务器
5. 进入维护循环（每分钟记录状态）

**输出**: Task - 异步任务

```csharp
// 服务启动日志:
// SouXiaoAVE Service started at: 2026-01-01 12:00:00
// Workflow engine initialized with 4 functions
// TCP server started on port 9527
```

### OnMessageReceived(Object? sender, String message)

处理收到的消息。

**参数**:
- `sender`: Object? - 事件发送者
- `message`: String - 收到的消息

**内部流程**:
1. 记录收到的消息
2. 异步处理消息
3. 广播处理结果

### ProcessMessageAsync(String message)

处理消息并返回响应。

**参数**:
- `message`: String - 客户端消息

**输出**: `Task<String>` - 响应消息

**内部流程**:
1. 解析命令（格式：`COMMAND|PARAM...`）
2. 根据命令类型分发处理

**支持的命令**:

| 命令 | 响应 | 描述 |
|------|------|------|
| ANALYZE | `OK|Prediction=X\|Label=Y` | 分析文件 |
| STATUS | `OK|Service running` | 服务状态 |
| PING | `PONG` | 心跳检测 |
| 其他 | `ERROR|Unknown command` | 未知命令 |

```csharp
// 请求: ANALYZE|C:\test.exe
// 响应: OK|Prediction=-7.70|Label=Benign

// 请求: STATUS
// 响应: OK|Service running

// 请求: PING
// 响应: PONG
```

### AnalyzeFileAsync(String[] parts)

执行文件分析。

**参数**:
- `parts`: String[] - 解析后的命令部分

**输出**: `Task<String>` - 分析结果响应

**内部流程**:
1. 提取文件路径
2. 验证文件存在
3. 调用WorkflowEngine执行分析
4. 提取预测结果
5. 格式化响应

```csharp
// 成功响应
// OK|Prediction=-7.70|Label=Benign

// 失败响应
// ERROR|File not found: C:\missing.exe
// ERROR|Analysis failed
```

### OnClientConnected(Object? sender, String endpoint)

处理客户端连接事件。

```csharp
// 日志: Client connected: 192.168.1.100:54321
```

### OnClientDisconnected(Object? sender, String endpoint)

处理客户端断开事件。

```csharp
// 日志: Client disconnected: 192.168.1.100:54321
```

### OnErrorOccurred(Object? sender, Exception ex)

处理错误事件。

```csharp
// 日志: TCP server error: [异常详情]
```

### StopAsync(CancellationToken cancellationToken)

停止服务。

**内部流程**:
1. 记录停止日志
2. 释放TCP服务器
3. 调用基类StopAsync

## 使用示例

### 客户端连接（Telnet）

```bash
telnet localhost 9527

> PING
< PONG

> STATUS
< OK|Service running

> ANALYZE|C:\Windows\System32\notepad.exe
< OK|Prediction=-7.70|Label=Benign
```

### 客户端连接（Python）

```python
import socket

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('localhost', 9527))

# 发送分析请求
sock.sendall(b'ANALYZE|C:\\test.exe\n')
response = sock.recv(1024).decode()
print(response)  # OK|Prediction=-7.70|Label=Benign

sock.close()
```

### 客户端连接（C#）

```csharp
using TcpClient client = new();
await client.ConnectAsync("localhost", 9527);

NetworkStream stream = client.GetStream();
StreamReader reader = new(stream);
StreamWriter writer = new(stream) { AutoFlush = true };

// 发送请求
await writer.WriteLineAsync("ANALYZE|C:\\test.exe");

// 接收响应
String? response = await reader.ReadLineAsync();
Console.WriteLine($"响应: {response}");
```

## 服务架构

```
┌─────────────────────────────────────────────────────┐
│                    Worker Service                    │
├─────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────────────────────┐ │
│  │  TcpServer  │◀──▶│      Message Handlers       │ │
│  │  (Port 9527)│    │  - ProcessMessageAsync      │ │
│  └─────────────┘    │  - AnalyzeFileAsync         │ │
│         │           └─────────────────────────────┘ │
│         │                      │                     │
│         ▼                      ▼                     │
│  ┌─────────────┐    ┌─────────────────────────────┐ │
│  │   Events    │    │      WorkflowEngine         │ │
│  │  - Connect  │    │  - LoadPE                   │ │
│  │  - Message  │    │  - ExtractFeatures          │ │
│  │  - Error    │    │  - PredictMalware           │ │
│  └─────────────┘    │  - GenerateReport           │ │
│                     └─────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

## 日志输出示例

```
info: SouXiaoAVE.Worker[0]
      SouXiaoAVE Service started at: 01/01/2026 12:00:00 +00:00
info: SouXiaoAVE.Worker[0]
      Workflow engine initialized with 4 functions
info: SouXiaoAVE.Worker[0]
      TCP server started on port 9527
info: SouXiaoAVE.Worker[0]
      Client connected: 192.168.1.100:54321
info: SouXiaoAVE.Worker[0]
      Received: ANALYZE|C:\test.exe
info: SouXiaoAVE.Worker[0]
      Service running. Connected clients: 1
```

## 注意事项

- TCP端口固定为9527
- 消息以换行符分隔
- 使用UTF-8编码
- 分析请求异步处理，不阻塞接收线程
- 服务停止时自动释放所有资源
