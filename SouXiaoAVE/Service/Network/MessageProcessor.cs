// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using SouXiaoAVE.Linker.Enums;
using SouXiaoAVE.Linker.Models;
using SouXiaoAVE.Service.Handlers;
using SouXiaoAVE.Service.Network.Protocol;
using SouXiaoAVE.Service.Network.Protocol.Messages;

namespace SouXiaoAVE.Service.Network;

public sealed class MessageProcessor : IDisposable
{
    private readonly TaskHandlerFactory _handlerFactory;
    private readonly ConcurrentDictionary<Guid, TaskContext> _taskContexts = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _taskCancellations = new();
    private readonly Boolean _quickMode;
    private Boolean _disposed = false;

    public event EventHandler<TaskProgressEventArgs>? TaskProgressUpdated;

    public MessageProcessor(TaskHandlerFactory handlerFactory, Boolean quickMode = true)
    {
        _handlerFactory = handlerFactory;
        _quickMode = quickMode;
    }

    public async Task<Byte[]> ProcessAsync(Guid clientId, Byte[] messageData, NetworkStream stream)
    {
        if (messageData.Length < NetworkConstants.HeaderSize)
        {
            return CreateErrorResponse(0, "Invalid message format", Guid.Empty);
        }

        Int32 jsonLength = BitConverter.ToInt32(messageData, 0);
        String json = NetworkConstants.Encoding.GetString(messageData, NetworkConstants.HeaderSize, jsonLength);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("type", out JsonElement typeElement))
        {
            return CreateErrorResponse(0, "Missing message type", Guid.Empty);
        }

        MessageType messageType = (MessageType)typeElement.GetUInt16();
        Guid messageId = root.TryGetProperty("messageId", out JsonElement idElement) ? idElement.GetGuid() : Guid.Empty;

        return messageType switch
        {
            MessageType.ConnectRequest => await HandleConnectAsync(json, clientId),
            MessageType.DisconnectRequest => HandleDisconnect(json, clientId),
            MessageType.SubmitTaskRequest => await HandleSubmitTaskAsync(json, clientId),
            MessageType.GetTaskStateRequest => HandleGetTaskState(json),
            MessageType.GetProgressRequest => HandleGetProgress(json),
            MessageType.GetReportRequest => HandleGetReport(json),
            MessageType.StopTaskRequest => HandleStopTask(json),
            MessageType.RestartTaskRequest => HandleRestartTask(json),
            MessageType.ReleaseTaskRequest => HandleReleaseTask(json),
            MessageType.StopAllRequest => HandleStopAll(json),
            MessageType.RestartAllRequest => HandleRestartAll(json),
            MessageType.ReleaseAllRequest => HandleReleaseAll(json),
            MessageType.HeartbeatRequest => HandleHeartbeat(json),
            _ => CreateErrorResponse((Int32)messageType, $"Unknown message type: {messageType}", messageId)
        };
    }

    private Task<Byte[]> HandleConnectAsync(String json, Guid clientId)
    {
        ConnectResponse response = new()
        {
            Success = true,
            ServerVersion = "1.0.0",
            SessionId = clientId
        };
        return Task.FromResult(response.Serialize());
    }

    private Byte[] HandleDisconnect(String json, Guid clientId)
    {
        DisconnectResponse response = new() { Success = true };
        return response.Serialize();
    }

    private async Task<Byte[]> HandleSubmitTaskAsync(String json, Guid clientId)
    {
        SubmitTaskRequest? request = JsonSerializer.Deserialize(json, MessageJsonContext.Default.SubmitTaskRequest);

        if (request is null || request.Tasks.Count == 0)
        {
            return CreateErrorResponse((Int32)MessageType.SubmitTaskRequest, "Invalid task request", Guid.Empty);
        }

        SubmitTaskResponse response = new()
        {
            Success = true,
            IsQuickMode = _quickMode
        };

        Boolean allFiles = true;
        foreach (TaskData taskData in request.Tasks)
        {
            if (taskData.Type != SXType.File)
            {
                allFiles = false;
            }
            response.TaskIds.Add(taskData.TaskId);
        }

        if (_quickMode && allFiles && request.Tasks.Count == 1)
        {
            TaskData taskData = request.Tasks[0];
            SXTask task = CreateSXTaskFromData(taskData);

            ITaskHandler? handler = _handlerFactory.GetHandler(task);
            if (handler is not null)
            {
                Progress<Double> progress = new(p => TaskProgressUpdated?.Invoke(this, new TaskProgressEventArgs(taskData.TaskId, p)));
                Report report = await handler.ProcessAsync(task, progress);

                response.QuickReport = new ReportData
                {
                    TaskId = report.TaskId.Id,
                    FilePath = report.FilePath,
                    Type = report.Type,
                    IsMalicious = report.IsMalicious,
                    Confidence = report.Confidence,
                    ThreatName = report.ThreatName,
                    ThreatType = report.ThreatType,
                    FileSize = report.FileSize,
                    FileHash = report.FileHash
                };
            }
        }
        else
        {
            foreach (TaskData taskData in request.Tasks)
            {
                SXTask task = CreateSXTaskFromData(taskData);
                _ = ProcessTaskAsync(task, taskData.TaskId);
            }
        }

        return response.Serialize();
    }

    private static SXTask CreateSXTaskFromData(TaskData data)
    {
        return data.Type switch
        {
            SXType.File => !String.IsNullOrEmpty(data.FilePath)
                ? SXTask.FromFile(data.FilePath)
                : SXTask.FromBytes(data.RawData ?? []),
            SXType.Folder => SXTask.FromFolder(data.FilePath ?? String.Empty),
            SXType.Zip => SXTask.FromZip(data.FilePath ?? String.Empty),
            _ => SXTask.FromFile(data.FilePath ?? String.Empty)
        };
    }

    private async Task ProcessTaskAsync(SXTask task, Guid taskId)
    {
        CancellationTokenSource cts = new();
        _taskCancellations[taskId] = cts;

        TaskContext context = new()
        {
            Task = task,
            State = SXTaskState.Running,
            Progress = 0.0
        };
        _taskContexts[taskId] = context;

        try
        {
            ITaskHandler? handler = _handlerFactory.GetHandler(task);

            if (handler is null)
            {
                context.State = SXTaskState.Failed;
                return;
            }

            Progress<Double> progress = new(p =>
            {
                context.Progress = p;
                TaskProgressUpdated?.Invoke(this, new TaskProgressEventArgs(taskId, p));
            });

            Report report = await handler.ProcessAsync(task, progress);

            context.Report = report;
            context.State = SXTaskState.Completed;
        }
        catch (OperationCanceledException)
        {
            context.State = SXTaskState.Cancelled;
        }
        catch (Exception)
        {
            context.State = SXTaskState.Failed;
        }
    }

    private Byte[] HandleGetTaskState(String json)
    {
        GetTaskStateRequest? request = JsonSerializer.Deserialize(json, MessageJsonContext.Default.GetTaskStateRequest);

        GetTaskStateResponse response = new();

        if (request is not null && _taskContexts.TryGetValue(request.TaskId, out TaskContext? context))
        {
            response.TaskId = request.TaskId;
            response.State = context.State;
        }
        else
        {
            response.State = SXTaskState.Failed;
        }

        return response.Serialize();
    }

    private Byte[] HandleGetProgress(String json)
    {
        GetProgressRequest? request = JsonSerializer.Deserialize(json, MessageJsonContext.Default.GetProgressRequest);

        GetProgressResponse response = new();

        if (request is not null && _taskContexts.TryGetValue(request.TaskId, out TaskContext? context))
        {
            response.TaskId = request.TaskId;
            response.Progress = context.Progress;
        }
        else
        {
            response.Progress = 0.0;
        }

        return response.Serialize();
    }

    private Byte[] HandleGetReport(String json)
    {
        GetReportRequest? request = JsonSerializer.Deserialize(json, MessageJsonContext.Default.GetReportRequest);

        GetReportResponse response = new();

        if (request is not null && _taskContexts.TryGetValue(request.TaskId, out TaskContext? context))
        {
            response.TaskId = request.TaskId;
            response.Found = context.State == SXTaskState.Completed && context.Report is not null;

            if (context.Report is not null)
            {
                response.Report = new ReportData
                {
                    TaskId = context.Report.TaskId.Id,
                    FilePath = context.Report.FilePath,
                    Type = context.Report.Type,
                    IsMalicious = context.Report.IsMalicious,
                    Confidence = context.Report.Confidence,
                    ThreatName = context.Report.ThreatName,
                    ThreatType = context.Report.ThreatType,
                    FileSize = context.Report.FileSize,
                    FileHash = context.Report.FileHash
                };
            }
        }
        else
        {
            response.Found = false;
        }

        return response.Serialize();
    }

    private Byte[] HandleStopTask(String json)
    {
        StopTaskRequest? request = JsonSerializer.Deserialize(json, MessageJsonContext.Default.StopTaskRequest);

        StopTaskResponse response = new();

        if (request is not null && _taskContexts.TryGetValue(request.TaskId, out TaskContext? context))
        {
            if (context.State == SXTaskState.Running)
            {
                context.State = SXTaskState.Paused;

                if (_taskCancellations.TryGetValue(request.TaskId, out CancellationTokenSource? cts))
                {
                    ITaskHandler? handler = _handlerFactory.GetHandler(context.Task);
                    handler?.Cancel();
                }
            }
            response.Success = true;
            response.TaskId = request.TaskId;
        }
        else
        {
            response.Success = false;
        }

        return response.Serialize();
    }

    private Byte[] HandleRestartTask(String json)
    {
        RestartTaskRequest? request = JsonSerializer.Deserialize(json, MessageJsonContext.Default.RestartTaskRequest);

        RestartTaskResponse response = new();

        if (request is not null && _taskContexts.TryGetValue(request.TaskId, out TaskContext? context))
        {
            if (context.State == SXTaskState.Paused)
            {
                context.State = SXTaskState.Running;
                _ = ProcessTaskAsync(context.Task, request.TaskId);
            }
            response.Success = true;
            response.TaskId = request.TaskId;
        }
        else
        {
            response.Success = false;
        }

        return response.Serialize();
    }

    private Byte[] HandleReleaseTask(String json)
    {
        ReleaseTaskRequest? request = JsonSerializer.Deserialize(json, MessageJsonContext.Default.ReleaseTaskRequest);

        ReleaseTaskResponse response = new();

        if (request is not null && _taskContexts.TryGetValue(request.TaskId, out TaskContext? context))
        {
            context.State = SXTaskState.Cancelled;

            if (_taskCancellations.TryRemove(request.TaskId, out CancellationTokenSource? cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            _taskContexts.TryRemove(request.TaskId, out _);
            response.Success = true;
            response.TaskId = request.TaskId;
        }
        else
        {
            response.Success = false;
        }

        return response.Serialize();
    }

    private Byte[] HandleStopAll(String json)
    {
        foreach (KeyValuePair<Guid, TaskContext> kvp in _taskContexts)
        {
            if (kvp.Value.State == SXTaskState.Running)
            {
                kvp.Value.State = SXTaskState.Paused;

                if (_taskCancellations.TryGetValue(kvp.Key, out CancellationTokenSource? cts))
                {
                    ITaskHandler? handler = _handlerFactory.GetHandler(kvp.Value.Task);
                    handler?.Cancel();
                }
            }
        }

        StopAllResponse response = new() { Success = true };
        return response.Serialize();
    }

    private Byte[] HandleRestartAll(String json)
    {
        foreach (KeyValuePair<Guid, TaskContext> kvp in _taskContexts)
        {
            if (kvp.Value.State == SXTaskState.Paused)
            {
                kvp.Value.State = SXTaskState.Running;
                _ = ProcessTaskAsync(kvp.Value.Task, kvp.Key);
            }
        }

        RestartAllResponse response = new() { Success = true };
        return response.Serialize();
    }

    private Byte[] HandleReleaseAll(String json)
    {
        foreach (KeyValuePair<Guid, TaskContext> kvp in _taskContexts)
        {
            kvp.Value.State = SXTaskState.Cancelled;
        }

        foreach (KeyValuePair<Guid, CancellationTokenSource> kvp in _taskCancellations)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }

        _taskCancellations.Clear();
        _taskContexts.Clear();

        ReleaseAllResponse response = new() { Success = true };
        return response.Serialize();
    }

    private Byte[] HandleHeartbeat(String json)
    {
        HeartbeatResponse response = new();
        return response.Serialize();
    }

    private static Byte[] CreateErrorResponse(Int32 errorCode, String errorMessage, Guid originalMessageId)
    {
        ErrorResponse response = new()
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            OriginalMessageId = originalMessageId
        };
        return response.Serialize();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (KeyValuePair<Guid, CancellationTokenSource> kvp in _taskCancellations)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }

        _taskCancellations.Clear();
        _taskContexts.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private sealed class TaskContext
    {
        public required SXTask Task { get; init; }
        public SXTaskState State { get; set; }
        public Double Progress { get; set; }
        public Report? Report { get; set; }
    }
}

public class TaskProgressEventArgs : EventArgs
{
    public Guid TaskId { get; }
    public Double Progress { get; }

    public TaskProgressEventArgs(Guid taskId, Double progress)
    {
        TaskId = taskId;
        Progress = progress;
    }
}
