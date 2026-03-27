// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SouXiaoAVE.Linker.Enums;
using SouXiaoAVE.Linker.Models;

namespace SouXiaoAVE.Service;

internal sealed class ScanService : IDisposable
{
    private static ScanService? _instance;
    private static readonly Object _instanceLock = new();

    private Boolean _isRunning = false;
    private Boolean _disposed = false;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly Dictionary<Guid, TaskItem> _taskQueue = [];
    private readonly Object _queueLock = new();

    public static ScanService Instance
    {
        get
        {
            lock (_instanceLock)
            {
                _instance ??= new ScanService();
                return _instance;
            }
        }
    }

    public Boolean IsRunning => _isRunning;

    public Int32 ActiveTaskCount
    {
        get
        {
            lock (_queueLock)
            {
                return _taskQueue.Count;
            }
        }
    }

    private ScanService() { }

    public Boolean Start()
    {
        if (_isRunning)
        {
            return false;
        }
        _isRunning = true;
        return true;
    }

    public Boolean Stop()
    {
        if (!_isRunning)
        {
            return false;
        }
        _isRunning = false;
        _cancellationTokenSource.Cancel();
        return true;
    }

    public TaskID SubmitTask(SXTask task)
    {
        TaskID taskId = task.TaskId;

        lock (_queueLock)
        {
            _taskQueue[taskId.Id] = new TaskItem
            {
                Task = task,
                Status = SXTaskState.Pending,
                Progress = 0.0,
                CreatedTime = DateTime.UtcNow
            };
        }

        return taskId;
    }

    public TaskID[] SubmitTasks(SXTask[] tasks)
    {
        TaskID[] taskIds = new TaskID[tasks.Length];

        for (Int32 i = 0; i < tasks.Length; i++)
        {
            taskIds[i] = SubmitTask(tasks[i]);
        }

        return taskIds;
    }

    public SXTaskState GetTaskState(TaskID taskId)
    {
        lock (_queueLock)
        {
            if (_taskQueue.TryGetValue(taskId.Id, out TaskItem? item))
            {
                return item.Status;
            }
            return SXTaskState.Failed;
        }
    }

    public Double GetTaskProgress(TaskID taskId)
    {
        lock (_queueLock)
        {
            if (_taskQueue.TryGetValue(taskId.Id, out TaskItem? item))
            {
                return item.Progress;
            }
            return 0.0;
        }
    }

    public Report? GetTaskReport(TaskID taskId)
    {
        lock (_queueLock)
        {
            if (_taskQueue.TryGetValue(taskId.Id, out TaskItem? item))
            {
                return item.Report;
            }
            return null;
        }
    }

    public Boolean PauseTask(TaskID taskId)
    {
        lock (_queueLock)
        {
            if (_taskQueue.TryGetValue(taskId.Id, out TaskItem? item))
            {
                if (item.Status == SXTaskState.Running)
                {
                    item.Status = SXTaskState.Paused;
                    return true;
                }
            }
            return false;
        }
    }

    public Boolean ResumeTask(TaskID taskId)
    {
        lock (_queueLock)
        {
            if (_taskQueue.TryGetValue(taskId.Id, out TaskItem? item))
            {
                if (item.Status == SXTaskState.Paused)
                {
                    item.Status = SXTaskState.Running;
                    return true;
                }
            }
            return false;
        }
    }

    public Boolean CancelTask(TaskID taskId)
    {
        lock (_queueLock)
        {
            if (_taskQueue.TryGetValue(taskId.Id, out TaskItem? item))
            {
                item.Status = SXTaskState.Cancelled;
                _taskQueue.Remove(taskId.Id);
                return true;
            }
            return false;
        }
    }

    public void PauseAll()
    {
        lock (_queueLock)
        {
            foreach (KeyValuePair<Guid, TaskItem> kvp in _taskQueue)
            {
                if (kvp.Value.Status == SXTaskState.Running)
                {
                    kvp.Value.Status = SXTaskState.Paused;
                }
            }
        }
    }

    public void ResumeAll()
    {
        lock (_queueLock)
        {
            foreach (KeyValuePair<Guid, TaskItem> kvp in _taskQueue)
            {
                if (kvp.Value.Status == SXTaskState.Paused)
                {
                    kvp.Value.Status = SXTaskState.Running;
                }
            }
        }
    }

    public void CancelAll()
    {
        lock (_queueLock)
        {
            foreach (KeyValuePair<Guid, TaskItem> kvp in _taskQueue)
            {
                kvp.Value.Status = SXTaskState.Cancelled;
            }
            _taskQueue.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        lock (_queueLock)
        {
            _taskQueue.Clear();
        }

        _isRunning = false;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private sealed class TaskItem
    {
        public required SXTask Task { get; init; }
        public SXTaskState Status { get; set; }
        public Double Progress { get; set; }
        public DateTime CreatedTime { get; init; }
        public Report? Report { get; set; }
    }
}
