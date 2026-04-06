# TcpServer Class

**Namespace**: `SouXiaoAVE.Services`  
**File**: `SouXiaoAVE/Services/TcpServer.cs`  
**Type**: Sealed Class  
**Implements**: `IAsyncDisposable`

## Overview

TCP server implementation supporting multiple client connections, message reception, and broadcast sending. Used to receive client analysis requests and return results.

## Properties

| Property Name | Type | Read-only | Description |
|---------------|------|-----------|-------------|
| IsRunning | Boolean | Yes | Whether the server is running |
| ConnectedClientCount | Int32 | Yes | Current number of connected clients |

## Events

| Event Name | Type | Description |
|------------|------|-------------|
| MessageReceived | `EventHandler<String>` | Triggered when a message is received |
| ClientConnected | `EventHandler<String>` | Triggered when a client connects |
| ClientDisconnected | `EventHandler<String>` | Triggered when a client disconnects |
| ErrorOccurred | `EventHandler<Exception>` | Triggered when an error occurs |

## Constructor

### TcpServer(Int32 port, IPAddress? bindAddress = null)

Create a TCP server instance.

**Parameters**:
- `port`: Int32 - Listening port
- `bindAddress`: IPAddress? - Bind address, defaults to IPAddress.Any

```csharp
TcpServer server = new(9527);
TcpServer serverLocal = new(9527, IPAddress.Loopback);
```

## Methods

### StartAsync(CancellationToken cancellationToken = default)

Start the server.

**Parameters**:
- `cancellationToken`: CancellationToken - Cancellation token

**Internal Flow**:
1. Check if object is disposed
2. Check if already running
3. Create TcpListener and start listening
4. Start background task to accept client connections

**Returns**: Task - Async task

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed

```csharp
TcpServer server = new(9527);
await server.StartAsync();
Console.WriteLine($"Server running: {server.IsRunning}");
```

### StopAsync()

Stop the server.

**Internal Flow**:
1. Cancel accepting new connections
2. Stop listener
3. Close all connected clients
4. Clear client list

**Returns**: Task - Async task

```csharp
await server.StopAsync();
Console.WriteLine($"Server stopped");
```

### BroadcastAsync(String message)

Broadcast message to all clients.

**Parameters**:
- `message`: String - Message to send

**Internal Flow**:
1. Check object state
2. Copy client list
3. Iterate and send messages (UTF-8 encoded, append newline)
4. Ignore failed sends

**Returns**: Task - Async task

**Exceptions**:
- `ObjectDisposedException`: Object has been disposed

```csharp
await server.BroadcastAsync("ANALYZE|C:\\test.exe");
await server.BroadcastAsync("STATUS|OK");
```

### DisposeAsync()

Release resources.

**Internal Flow**:
1. Call StopAsync()
2. Dispose CancellationTokenSource
3. Mark as disposed

```csharp
await using TcpServer server = new(9527);
// Automatically disposed
```

## Internal Methods

### AcceptClientsAsync(CancellationToken cancellationToken)

Background task that loops to accept client connections.

**Internal Flow**:
1. Loop waiting for new connections
2. Add to client list after accepting connection
3. Trigger ClientConnected event
4. Start handler task for each client

### HandleClientAsync(TcpClient client, CancellationToken cancellationToken)

Handle single client connection.

**Internal Flow**:
1. Get network stream
2. Create StreamReader to read messages
3. Loop reading line messages
4. Trigger MessageReceived event
5. Trigger ClientDisconnected event after client disconnects

## Usage Examples

### Basic Server

```csharp
await using TcpServer server = new(9527);

// Subscribe to events
server.ClientConnected += (s, endpoint) => 
    Console.WriteLine($"Client connected: {endpoint}");
server.ClientDisconnected += (s, endpoint) => 
    Console.WriteLine($"Client disconnected: {endpoint}");
server.MessageReceived += (s, message) => 
    Console.WriteLine($"Message received: {message}");
server.ErrorOccurred += (s, ex) => 
    Console.WriteLine($"Error: {ex.Message}");

// Start server
await server.StartAsync();
Console.WriteLine("Server started, press any key to stop...");
Console.ReadKey();
```

### Message Handling Server

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

### Integration with WorkflowEngine

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

## Protocol Format

### Request Format

```
COMMAND|PARAM1|PARAM2|...
```

### Supported Commands

| Command | Parameters | Description |
|---------|------------|-------------|
| PING | None | Heartbeat detection |
| STATUS | None | Get service status |
| ANALYZE | File path | Analyze PE file |

### Response Format

```
OK|Result data
ERROR|Error message
```

## Notes

- Messages are separated by newlines
- Uses UTF-8 encoding
- Ignores disconnected clients during broadcast
- Use `await using` to ensure resource disposal
- Background tasks use CancellationToken for lifecycle control
