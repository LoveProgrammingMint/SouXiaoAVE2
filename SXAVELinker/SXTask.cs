// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SXAVELinker;

public sealed class SXTask
{
    public SXTaskID ID { get; }
    public String Name { get; }
    public SXType InputType { get; }
    public SXType OutputType { get; }
    public SXData? Input { get; set; }
    public SXData? Output { get; set; }
    public Dictionary<String, Object> Parameters { get; }
    public List<SXTask> Dependencies { get; }
    public Func<SXTask, CancellationToken, Task<SXReport>>? ExecuteFunc { get; set; }
    public Action<SXTask>? OnStart { get; set; }
    public Action<SXTask, SXReport>? OnComplete { get; set; }
    public Boolean IsCompleted { get; private set; }
    public Boolean IsRunning { get; private set; }
    public SXReport? LastReport { get; private set; }

    public SXTask(String name, SXType inputType, SXType outputType)
    {
        ID = new SXTaskID(name);
        Name = name;
        InputType = inputType;
        OutputType = outputType;
        Parameters = new Dictionary<String, Object>();
        Dependencies = new List<SXTask>();
    }

    public void SetParameter(String key, Object value)
    {
        Parameters[key] = value;
    }

    public T? GetParameter<T>(String key)
    {
        if (Parameters.TryGetValue(key, out Object? value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public void AddDependency(SXTask task)
    {
        if (!Dependencies.Contains(task))
        {
            Dependencies.Add(task);
        }
    }

    public Boolean CanExecute()
    {
        return Dependencies.All(d => d.IsCompleted);
    }

    public async Task<SXReport> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (IsCompleted || IsRunning)
        {
            return LastReport ?? new SXReport(ID, Name);
        }

        if (!CanExecute())
        {
            SXReport blockedReport = new(ID, Name);
            blockedReport.SetFailure("Dependencies not completed");
            return blockedReport;
        }

        IsRunning = true;
        OnStart?.Invoke(this);

        SXReport report = new(ID, Name);

        try
        {
            if (ExecuteFunc is not null)
            {
                report = await ExecuteFunc(this, cancellationToken);
            }
            else
            {
                report.AddWarning("No execute function defined");
                report.SetSuccess();
            }

            IsCompleted = true;
        }
        catch (Exception ex)
        {
            report.SetFailure(ex.Message);
        }
        finally
        {
            IsRunning = false;
            LastReport = report;
            OnComplete?.Invoke(this, report);
        }

        return report;
    }

    public void Reset()
    {
        IsCompleted = false;
        IsRunning = false;
        LastReport = null;
    }

    public override String ToString()
    {
        return $"Task[{Name}] {ID.ID:N8}";
    }
}
