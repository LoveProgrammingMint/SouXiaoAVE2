// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SXAVELinker;

public sealed class SXLinker : IAsyncDisposable
{
    private readonly Dictionary<Guid, SXTask> _tasks;
    private readonly Dictionary<String, Func<SXTask, CancellationToken, Task<SXReport>>> _registeredFunctions;
    private readonly Dictionary<String, List<SXTask>> _workflows;
    private readonly Lock _lock;
    private readonly CancellationTokenSource _cts;
    private Boolean _disposed;

    public event EventHandler<SXTask>? TaskAdded;
    public event EventHandler<SXTask>? TaskStarted;
    public event EventHandler<(SXTask Task, SXReport Report)>? TaskCompleted;
    public event EventHandler<(SXTask Task, Exception Exception)>? TaskFailed;
    public event EventHandler<String>? WorkflowStarted;
    public event EventHandler<String>? WorkflowCompleted;

    public Int32 TaskCount => _tasks.Count;
    public Int32 WorkflowCount => _workflows.Count;
    public Int32 RegisteredFunctionCount => _registeredFunctions.Count;

    public SXLinker()
    {
        _tasks = new Dictionary<Guid, SXTask>();
        _registeredFunctions = new Dictionary<String, Func<SXTask, CancellationToken, Task<SXReport>>>();
        _workflows = new Dictionary<String, List<SXTask>>();
        _lock = new Lock();
        _cts = new CancellationTokenSource();
    }

    public void RegisterFunction(String name, Func<SXTask, CancellationToken, Task<SXReport>> func)
    {
        ThrowIfDisposed();
        _registeredFunctions[name] = func;
    }

    public void RegisterFunction(String name, Action<SXTask> action)
    {
        ThrowIfDisposed();
        _registeredFunctions[name] = (task, ct) =>
        {
            action(task);
            SXReport report = new(task.ID, task.Name);
            report.SetSuccess();
            return Task.FromResult(report);
        };
    }

    public void RegisterFunction<TInput, TOutput>(String name, Func<TInput, CancellationToken, Task<TOutput>> func)
    {
        ThrowIfDisposed();
        _registeredFunctions[name] = async (task, ct) =>
        {
            SXReport report = new(task.ID, task.Name);
            try
            {
                TInput? input = task.GetParameter<TInput>("input");
                if (input is null)
                {
                    report.SetFailure("Input parameter is null");
                    return report;
                }

                TOutput output = await func(input, ct);
                report.AddResult("output", output!);
                report.SetSuccess();
            }
            catch (Exception ex)
            {
                report.SetFailure(ex.Message);
            }
            return report;
        };
    }

    public Boolean UnregisterFunction(String name)
    {
        return _registeredFunctions.Remove(name);
    }

    public SXTask CreateTask(String name, SXType inputType, SXType outputType)
    {
        ThrowIfDisposed();
        SXTask task = new(name, inputType, outputType);
        task.OnStart += t => TaskStarted?.Invoke(this, t);
        task.OnComplete += (t, r) =>
        {
            if (r.IsSuccess)
                TaskCompleted?.Invoke(this, (t, r));
            else
                TaskFailed?.Invoke(this, (t, new Exception(r.ErrorMessage ?? "Unknown error")));
        };

        lock (_lock)
        {
            _tasks[task.ID.ID] = task;
        }
        TaskAdded?.Invoke(this, task);
        return task;
    }

    public SXTask CreateTask(String name, String functionName)
    {
        ThrowIfDisposed();
        if (!_registeredFunctions.TryGetValue(functionName, out Func<SXTask, CancellationToken, Task<SXReport>>? func))
        {
            throw new KeyNotFoundException($"Function '{functionName}' not registered");
        }

        SXTask task = CreateTask(name, SXType.Object, SXType.Report);
        task.ExecuteFunc = func;
        return task;
    }

    public SXTask? GetTask(Guid id)
    {
        lock (_lock)
        {
            return _tasks.TryGetValue(id, out SXTask? task) ? task : null;
        }
    }

    public SXTask? GetTask(String name)
    {
        lock (_lock)
        {
            return _tasks.Values.FirstOrDefault(t => t.Name == name);
        }
    }

    public Boolean RemoveTask(Guid id)
    {
        lock (_lock)
        {
            return _tasks.Remove(id);
        }
    }

    public IEnumerable<SXTask> GetAllTasks()
    {
        lock (_lock)
        {
            return _tasks.Values.ToList();
        }
    }

    public IEnumerable<SXTask> GetPendingTasks()
    {
        lock (_lock)
        {
            return _tasks.Values.Where(t => !t.IsCompleted && !t.IsRunning).ToList();
        }
    }

    public String CreateWorkflow(String name, params SXTask[] tasks)
    {
        ThrowIfDisposed();
        String workflowId = Guid.NewGuid().ToString("N");
        List<SXTask> workflowTasks = [.. tasks];
        _workflows[workflowId] = workflowTasks;
        return workflowId;
    }

    public String CreateWorkflow(String name, params String[] functionNames)
    {
        ThrowIfDisposed();
        String workflowId = Guid.NewGuid().ToString("N");
        List<SXTask> workflowTasks = [];

        for (Int32 i = 0; i < functionNames.Length; i++)
        {
            String funcName = functionNames[i];
            if (_registeredFunctions.TryGetValue(funcName, out Func<SXTask, CancellationToken, Task<SXReport>>? func))
            {
                SXTask task = new($"{name}_{funcName}_{i}", SXType.Object, SXType.Report);
                task.ExecuteFunc = func;
                if (workflowTasks.Count > 0)
                {
                    task.AddDependency(workflowTasks[^1]);
                }
                workflowTasks.Add(task);
                _tasks[task.ID.ID] = task;
            }
        }

        _workflows[workflowId] = workflowTasks;
        return workflowId;
    }

    public async Task<List<SXReport>> ExecuteWorkflowAsync(String workflowId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!_workflows.TryGetValue(workflowId, out List<SXTask>? tasks))
        {
            throw new KeyNotFoundException($"Workflow '{workflowId}' not found");
        }

        WorkflowStarted?.Invoke(this, workflowId);
        List<SXReport> reports = [];

        for (Int32 i = 0; i < tasks.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            SXTask task = tasks[i];
            if (i > 0)
            {
                if (tasks[i - 1].Output is not null)
                {
                    task.Input = tasks[i - 1].Output;
                }
                foreach (KeyValuePair<String, Object> param in tasks[i - 1].Parameters)
                {
                    if (!task.Parameters.ContainsKey(param.Key))
                    {
                        task.SetParameter(param.Key, param.Value);
                    }
                }
            }

            SXReport report = await task.ExecuteAsync(cancellationToken);
            reports.Add(report);
        }

        WorkflowCompleted?.Invoke(this, workflowId);
        return reports;
    }

    public async Task<SXReport> ExecuteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        SXTask? task = GetTask(taskId);
        if (task is null)
        {
            throw new KeyNotFoundException($"Task '{taskId}' not found");
        }
        return await task.ExecuteAsync(cancellationToken);
    }

    public async Task<SXReport> ExecuteTaskAsync(String taskName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        SXTask? task = GetTask(taskName);
        if (task is null)
        {
            throw new KeyNotFoundException($"Task '{taskName}' not found");
        }
        return await task.ExecuteAsync(cancellationToken);
    }

    public async Task<List<SXReport>> ExecuteAllPendingAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        List<SXReport> reports = [];
        List<SXTask> pendingTasks;

        lock (_lock)
        {
            pendingTasks = _tasks.Values.Where(t => !t.IsCompleted && !t.IsRunning).ToList();
        }

        foreach (SXTask task in pendingTasks)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (task.CanExecute())
            {
                SXReport report = await task.ExecuteAsync(cancellationToken);
                reports.Add(report);
            }
        }

        return reports;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _tasks.Clear();
            _workflows.Clear();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SXLinker));
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            await Task.CompletedTask;
            _cts.Dispose();
            _tasks.Clear();
            _workflows.Clear();
            _registeredFunctions.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~SXLinker()
    {
        DisposeAsync().AsTask().Wait();
    }
}
