// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.Text;

namespace SXAVELinker;

public sealed class SXReport
{
    public SXTaskID TaskID { get; }
    public String TaskName { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; private set; }
    public TimeSpan Duration => EndTime - StartTime;
    public Boolean IsSuccess { get; private set; }
    public String? ErrorMessage { get; private set; }
    public Dictionary<String, Object> Results { get; }
    public List<String> Warnings { get; }
    public List<String> Info { get; }

    public SXReport(SXTaskID taskID, String taskName)
    {
        TaskID = taskID;
        TaskName = taskName;
        StartTime = DateTime.UtcNow;
        Results = new Dictionary<String, Object>();
        Warnings = new List<String>();
        Info = new List<String>();
    }

    public void SetSuccess()
    {
        IsSuccess = true;
        EndTime = DateTime.UtcNow;
    }

    public void SetFailure(String errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
        EndTime = DateTime.UtcNow;
    }

    public void AddResult(String key, Object value)
    {
        Results[key] = value;
    }

    public T? GetResult<T>(String key)
    {
        if (Results.TryGetValue(key, out Object? value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public void AddWarning(String warning)
    {
        Warnings.Add(warning);
    }

    public void AddInfo(String info)
    {
        Info.Add(info);
    }

    public override String ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine($"=== Task Report: {TaskName} ===");
        sb.AppendLine($"ID: {TaskID.ID:N}");
        sb.AppendLine($"Status: {(IsSuccess ? "Success" : "Failed")}");
        sb.AppendLine($"Duration: {Duration.TotalMilliseconds:F2}ms");

        if (!IsSuccess && ErrorMessage is not null)
        {
            sb.AppendLine($"Error: {ErrorMessage}");
        }

        if (Results.Count > 0)
        {
            sb.AppendLine("Results:");
            foreach (KeyValuePair<String, Object> kvp in Results)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        if (Warnings.Count > 0)
        {
            sb.AppendLine($"Warnings: {Warnings.Count}");
        }

        return sb.ToString();
    }
}
