# Worker Class

**Namespace**: `SouXiaoAVE`  
**File**: `SouXiaoAVE/Worker.cs`  
**Type**: Sealed Class  
**Base Class**: `BackgroundService`

## Overview

Background service worker that manages TCP server lifecycle, handles client messages, and executes PE file analysis.

## Dependencies

Injected through constructor:

| Parameter | Type | Description |
|------------|------|-------------|
| logger | `ILogger<Worker>` | Logger |
| workflowEngine | WorkflowEngine | Workflow engine |
| featureExtractor | PeFeatureExtractor | Feature extractor |

## Properties

| Property Name | Type | Description |
|---------------|------|-------------|
| _tcpServer | TcpServer? | TCP server instance |

## Methods

### ExecuteAsync(CancellationToken stoppingToken)

Background service main execution method.

**Parameters**:
- `stoppingToken`: CancellationToken - Stop token

**Internal Flow**:
1. Log service startup
2. Create TCP server (port 9527)
3. Subscribe to server events
4. Start TCP server
5. Enter maintenance loop (log status every minute)

**Returns**: Task - Async task

```csharp
// Service startup log:
// SouXiaoAVE Service started at: 2026-01-01 12:00:00
// Workflow engine initialized with 4 functions
// TCP server started on port 9527
```

### OnMessageReceived(Object? sender, String message)

Handle received messages.

**Parameters**:
- `sender`: Object? - Event sender
- `message`: String - Received message

**Internal Flow**:
1. Log received message
2. Process message asynchronously
3. Broadcast processing result

### ProcessMessageAsync(String message)

Process message and return response.

**Parameters**:
- `message`: String - Client message

**Returns**: `Task<String>` - Response message

**Internal Flow**:
1. Parse command (format: `COMMAND|PARAM...`)
2. Dispatch processing based on command type

**Supported Commands**:

| Command | Response | Description |
|---------|----------|-------------|
| ANALYZE | `OK|Prediction=X\|Label=Y` | Analyze file |
| STATUS | `OK|Service running` | Service status |
| PING | `PONG` | Heartbeat detection |
| Other | `ERROR|Unknown command` | Unknown command |

```csharp
// Request: ANALYZE|C:\test.exe
// Response: OK|Prediction=-7.70|Label=Benign

// Request: STATUS
// Response: OK|Service running

// Request: PING
// Response: PONG
```

### AnalyzeFileAsync(String[] parts)

Execute file analysis.

**Parameters**:
- `parts`: String[] - Parsed command parts

**Returns**: `Task<String>` - Analysis result response

**Internal Flow**:
1. Extract file path
2. Verify file exists
3. Call WorkflowEngine to execute analysis
4. Extract prediction result
5. Format response

```csharp
// Success response
// OK|Prediction=-7.70|Label=Benign

// Failure response
// ERROR|File not found: C:\missing.exe
// ERROR|Analysis failed
```

### OnClientConnected(Object? sender, String endpoint)

Handle client connection event.

```csharp
// Log: Client connected: 192.168.1.100:54321
```

### OnClientDisconnected(Object? sender, String endpoint)

Handle client disconnection event.

```csharp
// Log: Client disconnected: 192.168.1.100:54321
```

### OnErrorOccurred(Object? sender, Exception ex)

Handle error event.

```csharp
// Log: TCP server error: [Exception details]
```

### StopAsync(CancellationToken cancellationToken)

Stop the service.

**Internal Flow**:
1. Log stop
2. Dispose TCP server
3. Call base class StopAsync

## Usage Examples

### Client Connection (Telnet)

```bash
telnet localhost 9527

> PING
< PONG

> STATUS
< OK|Service running

> ANALYZE|C:\Windows\System32\notepad.exe
< OK|Prediction=-7.70|Label=Benign
```

### Client Connection (Python)

```python
import socket

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('localhost', 9527))

# Send analysis request
sock.sendall(b'ANALYZE|C:\\test.exe\n')
response = sock.recv(1024).decode()
print(response)  # OK|Prediction=-7.70|Label=Benign

sock.close()
```

### Client Connection (C#)

```csharp
using TcpClient client = new();
await client.ConnectAsync("localhost", 9527);

NetworkStream stream = client.GetStream();
StreamReader reader = new(stream);
StreamWriter writer = new(stream) { AutoFlush = true };

// Send request
await writer.WriteLineAsync("ANALYZE|C:\\test.exe");

// Receive response
String? response = await reader.ReadLineAsync();
Console.WriteLine($"Response: {response}");
```

## Service Architecture

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

## Log Output Example

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

## Notes

- TCP port is fixed at 9527
- Messages are separated by newlines
- Uses UTF-8 encoding
- Analysis requests are processed asynchronously without blocking the receiving thread
- All resources are automatically released when service stops
