// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using SouXiaoAVE.Linker.Enums;
using SouXiaoAVE.Linker.Models;
using SouXiaoAVE.Linker.Network;
using SouXiaoAVE.Service.Network.Protocol;
using SouXiaoAVE.Service.Network.Protocol.Messages;

namespace SouXiaoAVE.Linker;

public sealed class Linker : IDisposable
{
    private static Boolean _serviceRunning = false;
    private static readonly Lock _serviceLock = new();

    private readonly TcpClientConnection? _connection;
    private Boolean _disposed = false;

    private readonly Dictionary<TaskID, SXTask> _tasks = [];
    private readonly Dictionary<TaskID, Report?> _reports = [];
    private readonly Lock _taskLock = new();

    private readonly String _host;
    private readonly Int32 _port;

    public Boolean IsConnected => _connection?.IsConnected ?? false;

    public Boolean IsServiceRunning => _serviceRunning;

    private Linker(String host, Int32 port)
    {
        _host = host;
        _port = port;
        _connection = new TcpClientConnection(host, port);
    }

    public static ServiceResult StartService()
    {
        lock (_serviceLock)
        {
            if (_serviceRunning)
            {
                return ServiceResult.ServiceAlreadyRunning;
            }
            _serviceRunning = true;
            return ServiceResult.Success;
        }
    }

    public static ServiceResult StopService()
    {
        lock (_serviceLock)
        {
            if (!_serviceRunning)
            {
                return ServiceResult.ServiceNotRunning;
            }
            _serviceRunning = false;
            return ServiceResult.Success;
        }
    }

    public static async Task<Linker?> LinkServiceAsync(String host = "localhost", Int32 port = NetworkConstants.DefaultPort)
    {
        if (!_serviceRunning)
        {
            return null;
        }

        Linker linker = new(host, port);
        Boolean connected = await linker.ConnectAsync();

        return connected ? linker : null;
    }

    public static Linker? LinkService(String host = "localhost", Int32 port = NetworkConstants.DefaultPort)
    {
        return LinkServiceAsync(host, port).GetAwaiter().GetResult();
    }

    private async Task<Boolean> ConnectAsync()
    {
        if (_connection is null)
        {
            return false;
        }

        Boolean connected = await _connection.ConnectAsync();

        if (connected)
        {
            ConnectRequest request = new();
            Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

            if (response is not null)
            {
                ConnectResponse? connectResponse = BaseMessage.Deserialize<ConnectResponse>(response);
                return connectResponse?.Success ?? false;
            }
        }

        return false;
    }

    public async Task<ServiceResult> DisconnectAsync()
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return ServiceResult.ConnectionFailed;
        }

        DisconnectRequest request = new();
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is not null)
        {
            DisconnectResponse? disconnectResponse = BaseMessage.Deserialize<DisconnectResponse>(response);
            _connection.Disconnect();
            return disconnectResponse?.Success == true ? ServiceResult.Success : ServiceResult.Failed;
        }

        return ServiceResult.ConnectionFailed;
    }

    public ServiceResult Disconnect()
    {
        return DisconnectAsync().GetAwaiter().GetResult();
    }

    public SXTask CreateTask(String source, SXType type)
    {
        return type switch
        {
            SXType.File => SXTask.FromFile(source),
            SXType.Folder => SXTask.FromFolder(source),
            SXType.Zip => SXTask.FromZip(source),
            _ => SXTask.FromFile(source)
        };
    }

    public SXTask CreateTask(Byte[] data, SXType type)
    {
        return type switch
        {
            SXType.File => SXTask.FromBytes(data),
            _ => SXTask.FromBytes(data)
        };
    }

    public SXTask CreateTask(List<Byte> data, SXType type)
    {
        return type switch
        {
            SXType.File => SXTask.FromByteList(data),
            _ => SXTask.FromByteList(data)
        };
    }

    public async Task<Object> SendTaskAsync(SXTask[] tasks)
    {
        if (tasks is null || tasks.Length == 0)
        {
            return ServiceResult.InvalidParameter;
        }

        if (_connection is null || !_connection.IsConnected)
        {
            return ServiceResult.ConnectionFailed;
        }

        SubmitTaskRequest request = new();

        foreach (SXTask task in tasks)
        {
            request.Tasks.Add(new TaskData
            {
                TaskId = task.TaskId.Id,
                Type = task.Type,
                FilePath = task.FilePath,
                RawData = task.RawData
            });

            lock (_taskLock)
            {
                _tasks[task.TaskId] = task;
            }
        }

        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return ServiceResult.ConnectionFailed;
        }

        SubmitTaskResponse? submitResponse = BaseMessage.Deserialize<SubmitTaskResponse>(response);

        if (submitResponse is null)
        {
            return ServiceResult.InternalError;
        }

        if (submitResponse.IsQuickMode && submitResponse.QuickReport is not null)
        {
            ReportData reportData = submitResponse.QuickReport;
            Report report = ConvertToReport(reportData);

            lock (_taskLock)
            {
                TaskID taskId = new(reportData.TaskId);
                if (_tasks.TryGetValue(taskId, out SXTask? task))
                {
                    task.State = SXTaskState.Completed;
                    _reports[taskId] = report;
                }
            }

            return report;
        }

        TaskID[] taskIds = submitResponse.TaskIds.Select(id => new TaskID(id)).ToArray();

        lock (_taskLock)
        {
            foreach (Guid id in submitResponse.TaskIds)
            {
                TaskID taskId = new(id);
                if (_tasks.TryGetValue(taskId, out SXTask? task))
                {
                    task.State = SXTaskState.Running;
                }
            }
        }

        return taskIds;
    }

    public Object SendTask(SXTask[] tasks)
    {
        return SendTaskAsync(tasks).GetAwaiter().GetResult();
    }

    public async Task<SXTaskState> GetTaskStateAsync(TaskID taskId)
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return SXTaskState.Failed;
        }

        GetTaskStateRequest request = new() { TaskId = taskId.Id };
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return SXTaskState.Failed;
        }

        GetTaskStateResponse? stateResponse = BaseMessage.Deserialize<GetTaskStateResponse>(response);
        return stateResponse?.State ?? SXTaskState.Failed;
    }

    public SXTaskState GetTaskState(TaskID taskId)
    {
        return GetTaskStateAsync(taskId).GetAwaiter().GetResult();
    }

    public async Task<Double> GetProcessRateAsync(TaskID taskId)
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return 0.0;
        }

        GetProgressRequest request = new() { TaskId = taskId.Id };
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return 0.0;
        }

        GetProgressResponse? progressResponse = BaseMessage.Deserialize<GetProgressResponse>(response);
        return progressResponse?.Progress ?? 0.0;
    }

    public Double GetProcessRate(TaskID taskId)
    {
        return GetProcessRateAsync(taskId).GetAwaiter().GetResult();
    }

    public async Task<Report?> GetReportAsync(TaskID taskId)
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return null;
        }

        GetReportRequest request = new() { TaskId = taskId.Id };
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return null;
        }

        GetReportResponse? reportResponse = BaseMessage.Deserialize<GetReportResponse>(response);

        if (reportResponse is null || !reportResponse.Found || reportResponse.Report is null)
        {
            return null;
        }

        return ConvertToReport(reportResponse.Report);
    }

    public Report? GetReport(TaskID taskId)
    {
        return GetReportAsync(taskId).GetAwaiter().GetResult();
    }

    public async Task<Report> WaitTaskAsync(TaskID taskId)
    {
        while (true)
        {
            SXTaskState state = await GetTaskStateAsync(taskId);

            if (state == SXTaskState.Completed || state == SXTaskState.Failed || state == SXTaskState.Cancelled)
            {
                break;
            }

            await Task.Delay(100);
        }

        Report? report = await GetReportAsync(taskId);
        return report ?? Report.CreateEmpty();
    }

    public async Task<Report> WaitTask(TaskID taskId)
    {
        return await WaitTaskAsync(taskId);
    }

    public async Task<ServiceResult> StopAllAsync()
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return ServiceResult.ConnectionFailed;
        }

        StopAllRequest request = new();
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return ServiceResult.ConnectionFailed;
        }

        StopAllResponse? stopResponse = BaseMessage.Deserialize<StopAllResponse>(response);
        return stopResponse?.Success == true ? ServiceResult.Success : ServiceResult.Failed;
    }

    public ServiceResult StopAll()
    {
        return StopAllAsync().GetAwaiter().GetResult();
    }

    public async Task<ServiceResult> StopTaskAsync(TaskID taskId)
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return ServiceResult.ConnectionFailed;
        }

        StopTaskRequest request = new() { TaskId = taskId.Id };
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return ServiceResult.ConnectionFailed;
        }

        StopTaskResponse? stopResponse = BaseMessage.Deserialize<StopTaskResponse>(response);
        return stopResponse?.Success == true ? ServiceResult.Success : ServiceResult.TaskNotFound;
    }

    public ServiceResult StopTask(TaskID taskId)
    {
        return StopTaskAsync(taskId).GetAwaiter().GetResult();
    }

    public async Task<ServiceResult> RestartAllAsync()
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return ServiceResult.ConnectionFailed;
        }

        RestartAllRequest request = new();
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return ServiceResult.ConnectionFailed;
        }

        RestartAllResponse? restartResponse = BaseMessage.Deserialize<RestartAllResponse>(response);
        return restartResponse?.Success == true ? ServiceResult.Success : ServiceResult.Failed;
    }

    public ServiceResult RestartAll()
    {
        return RestartAllAsync().GetAwaiter().GetResult();
    }

    public async Task<ServiceResult> RestartTaskAsync(TaskID taskId)
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return ServiceResult.ConnectionFailed;
        }

        RestartTaskRequest request = new() { TaskId = taskId.Id };
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return ServiceResult.ConnectionFailed;
        }

        RestartTaskResponse? restartResponse = BaseMessage.Deserialize<RestartTaskResponse>(response);
        return restartResponse?.Success == true ? ServiceResult.Success : ServiceResult.TaskNotFound;
    }

    public ServiceResult RestartTask(TaskID taskId)
    {
        return RestartTaskAsync(taskId).GetAwaiter().GetResult();
    }

    public async Task<ServiceResult> ReleaseAllAsync()
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return ServiceResult.ConnectionFailed;
        }

        ReleaseAllRequest request = new();
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return ServiceResult.ConnectionFailed;
        }

        ReleaseAllResponse? releaseResponse = BaseMessage.Deserialize<ReleaseAllResponse>(response);

        if (releaseResponse?.Success == true)
        {
            lock (_taskLock)
            {
                _tasks.Clear();
                _reports.Clear();
            }
            return ServiceResult.Success;
        }

        return ServiceResult.Failed;
    }

    public ServiceResult ReleaseAll()
    {
        return ReleaseAllAsync().GetAwaiter().GetResult();
    }

    public async Task<ServiceResult> ReleaseTaskAsync(TaskID taskId)
    {
        if (_connection is null || !_connection.IsConnected)
        {
            return ServiceResult.ConnectionFailed;
        }

        ReleaseTaskRequest request = new() { TaskId = taskId.Id };
        Byte[]? response = await _connection.SendAndReceiveAsync(request.Serialize());

        if (response is null)
        {
            return ServiceResult.ConnectionFailed;
        }

        ReleaseTaskResponse? releaseResponse = BaseMessage.Deserialize<ReleaseTaskResponse>(response);

        if (releaseResponse?.Success == true)
        {
            lock (_taskLock)
            {
                _tasks.Remove(taskId);
                _reports.Remove(taskId);
            }
            return ServiceResult.Success;
        }

        return ServiceResult.TaskNotFound;
    }

    public ServiceResult ReleaseTask(TaskID taskId)
    {
        return ReleaseTaskAsync(taskId).GetAwaiter().GetResult();
    }

    private static Report ConvertToReport(ReportData data)
    {
        return data.IsMalicious
            ? Report.CreateMalicious(
                new TaskID(data.TaskId),
                data.FilePath,
                data.Type,
                data.ThreatName ?? "Unknown",
                data.ThreatType,
                data.Confidence,
                data.FileSize,
                data.FileHash)
            : Report.CreateClean(
                new TaskID(data.TaskId),
                data.FilePath,
                data.Type,
                data.FileSize,
                data.FileHash);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _connection?.Dispose();

        lock (_taskLock)
        {
            _tasks.Clear();
            _reports.Clear();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
