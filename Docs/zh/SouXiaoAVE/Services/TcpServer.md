# TcpServer 类

**命名空间**: `SouXiaoAVE.Services`  
**文件**: `SouXiaoAVE/Services/TcpServer.cs`  
**类型**: 密封类 (Sealed Class)  
**实现接口**: `IAsyncDisposable`

## 概述

TCP服务器实现，支持多客户端连接、消息接收和广播发送。用于接收客户端分析请求并返回结果。

## 属性

| 属性名 | 类型 | 只读 | 描述 |
|--------|------|------|------|
| IsRunning | Boolean | 是 | 服务器是否运行中 |
| ConnectedClientCount | Int32 | 是 | 当前连接客户端数量 |

## 事件

| 事件名 | 类型 | 描述 |
|--------|------|------|
| MessageReceived | `EventHandler<String>` | 收到消息时触发 |
| ClientConnected | `EventHandler<String>` | 客户端连接时触发 |
| ClientDisconnected | `EventHandler<String>` | 客户端断开时触发 |
| ErrorOccurred | `EventHandler<Exception>` | 发生错误时触发 |

## 构造函数

### TcpServer(Int32 port, IPAddress? bindAddress = null)

创建TCP服务器实例。

**参数**:
- `port`: Int32 - 监听端口
- `bindAddress`: IPAddress? - 绑定地址，默认为IPAddress.Any

```csharp
TcpServer server = new(9527);
TcpServer serverLocal = new(9527, IPAddress.Loopback);
```

## 方法

### StartAsync(CancellationToken cancellationToken = default)

启动服务器。

**参数**:
- `cancellationToken`: CancellationToken - 取消令牌

**内部流程**:
1. 检查对象是否已释放
2. 检查是否已在运行
3. 创建TcpListener并开始监听
4. 启动后台任务接受客户端连接

**输出**: Task - 异步任务

**异常**:
- `ObjectDisposedException`: 对象已释放

```csharp
TcpServer server = new(9527);
await server.StartAsync();
Console.WriteLine($"服务器运行: {server.IsRunning}");
```

### StopAsync()

停止服务器。

**内部流程**:
1. 取消接受新连接
2. 停止监听器
3. 关闭所有已连接客户端
4. 清空客户端列表

**输出**: Task - 异步任务

```csharp
await server.StopAsync();
Console.WriteLine($"服务器已停止");
```

### BroadcastAsync(String message)

向所有客户端广播消息。

**参数**:
- `message`: String - 要发送的消息

**内部流程**:
1. 检查对象状态
2. 复制客户端列表
3. 遍历发送消息（UTF-8编码，追加换行符）
4. 忽略发送失败的客户端

**输出**: Task - 异步任务

**异常**:
- `ObjectDisposedException`: 对象已释放

```csharp
await server.BroadcastAsync("ANALYZE|C:\\test.exe");
await server.BroadcastAsync("STATUS|OK");
```

### DisposeAsync()

释放资源。

**内部流程**:
1. 调用StopAsync()
2. 释放CancellationTokenSource
3. 标记已释放

```csharp
await using TcpServer server = new(9527);
// 自动释放
```

## 内部方法

### AcceptClientsAsync(CancellationToken cancellationToken)

后台任务，循环接受客户端连接。

**内部流程**:
1. 循环等待新连接
2. 接受连接后添加到客户端列表
3. 触发ClientConnected事件
4. 为每个客户端启动处理任务

### HandleClientAsync(TcpClient client, CancellationToken cancellationToken)

处理单个客户端连接。

**内部流程**:
1. 获取网络流
2. 创建StreamReader读取消息
3. 循环读取行消息
4. 触发MessageReceived事件
5. 客户端断开后触发ClientDisconnected事件

## 使用示例

### 基本服务器

```csharp
await using TcpServer server = new(9527);

// 订阅事件
server.ClientConnected += (s, endpoint) => 
    Console.WriteLine($"客户端连接: {endpoint}");
server.ClientDisconnected += (s, endpoint) => 
    Console.WriteLine($"客户端断开: {endpoint}");
server.MessageReceived += (s, message) => 
    Console.WriteLine($"收到消息: {message}");
server.ErrorOccurred += (s, ex) => 
    Console.WriteLine($"错误: {ex.Message}");

// 启动服务器
await server.StartAsync();
Console.WriteLine("服务器已启动，按任意键停止...");
Console.ReadKey();
```

### 消息处理服务器

```csharp
TcpServer server = new(9527);

server.MessageReceived += async (sender, message) =>
{
    String[] parts = message.Split('|');
    String command = parts[0].ToUpperInvariant();
    
    String response = command switch
    {
        "PING" => "PONG",
        "STATUS" => "OK|Running",
        "ANALYZE" => await AnalyzeFileAsync(parts[1]),
        _ => $"ERROR|Unknown command: {command}"
    };
    
    if (sender is TcpServer srv)
    {
        await srv.BroadcastAsync(response);
    }
};

await server.StartAsync();
```

### 与WorkflowEngine集成

```csharp
await using TcpServer server = new(9527);
await using WorkflowEngine engine = new();

server.MessageReceived += async (sender, message) =>
{
    if (!message.StartsWith("ANALYZE|"))
        return;
    
    String filePath = message[8..];
    
    if (!File.Exists(filePath))
    {
        await server.BroadcastAsync($"ERROR|File not found: {filePath}");
        return;
    }
    
    try
    {
        List<SXReport> reports = await engine.ExecuteAnalysisAsync(filePath);
        SXReport? last = reports.LastOrDefault();
        
        if (last?.IsSuccess == true)
        {
            String label = last.GetResult<String>("Label") ?? "Unknown";
            await server.BroadcastAsync($"OK|Label={label}");
        }
        else
        {
            await server.BroadcastAsync($"ERROR|Analysis failed");
        }
    }
    catch (Exception ex)
    {
        await server.BroadcastAsync($"ERROR|{ex.Message}");
    }
};

await server.StartAsync();
```

## 协议格式

### 请求格式

```
COMMAND|PARAM1|PARAM2|...
```

### 支持的命令

| 命令 | 参数 | 描述 |
|------|------|------|
| PING | 无 | 心跳检测 |
| STATUS | 无 | 获取服务状态 |
| ANALYZE | 文件路径 | 分析PE文件 |

### 响应格式

```
OK|结果数据
ERROR|错误信息
```

## 注意事项

- 消息以换行符分隔
- 使用UTF-8编码
- 广播发送时忽略断开的客户端
- 使用 `await using` 确保资源释放
- 后台任务使用CancellationToken控制生命周期
