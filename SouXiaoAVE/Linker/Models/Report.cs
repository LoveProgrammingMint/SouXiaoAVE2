// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;

using SouXiaoAVE.Linker.Enums;

namespace SouXiaoAVE.Linker.Models;

public sealed class Report
{
    public TaskID TaskId { get; init; }

    public String? FilePath { get; init; }

    public SXType Type { get; init; }

    public Boolean IsMalicious { get; init; }

    public Double Confidence { get; init; }

    public String? ThreatName { get; init; }

    public String? ThreatType { get; init; }

    public Dictionary<String, Object>? AdditionalInfo { get; init; }

    public DateTime ScanTime { get; init; }

    public Int64 FileSize { get; init; }

    public String? FileHash { get; init; }

    public List<Report>? SubReports { get; init; }

    public Boolean IsValid { get; init; }

    private Report()
    {
        TaskId = TaskID.NewTaskID();
        ScanTime = DateTime.UtcNow;
        IsValid = false;
    }

    public static Report CreateClean(TaskID taskId, String? filePath, SXType type, Int64 fileSize = 0, String? fileHash = null)
    {
        return new Report
        {
            TaskId = taskId,
            FilePath = filePath,
            Type = type,
            IsMalicious = false,
            Confidence = 1.0,
            FileSize = fileSize,
            FileHash = fileHash,
            IsValid = true
        };
    }

    public static Report CreateMalicious(
        TaskID taskId,
        String? filePath,
        SXType type,
        String threatName,
        String? threatType = null,
        Double confidence = 1.0,
        Int64 fileSize = 0,
        String? fileHash = null,
        Dictionary<String, Object>? additionalInfo = null)
    {
        return new Report
        {
            TaskId = taskId,
            FilePath = filePath,
            Type = type,
            IsMalicious = true,
            Confidence = confidence,
            ThreatName = threatName,
            ThreatType = threatType,
            FileSize = fileSize,
            FileHash = fileHash,
            AdditionalInfo = additionalInfo,
            IsValid = true
        };
    }

    public static Report CreateEmpty() => new();
}
