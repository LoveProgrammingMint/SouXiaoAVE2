// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;

using SouXiaoAVE.Linker.Enums;

namespace SouXiaoAVE.Service;

internal sealed class ServiceConfig
{
    public Int32 MaxConcurrentTasks { get; set; } = 4;

    public Int32 TaskTimeoutMs { get; set; } = 300000;

    public Boolean QuickScanMode { get; set; } = true;

    public Boolean EnableHeuristics { get; set; } = true;

    public Boolean EnableDeepScan { get; set; } = false;

    public Int32 MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    public String[]? ExcludeExtensions { get; set; }

    public String[]? IncludeExtensions { get; set; }

    public ServiceResult Validate()
    {
        if (MaxConcurrentTasks < 1 || MaxConcurrentTasks > 64)
        {
            return ServiceResult.InvalidParameter;
        }

        if (TaskTimeoutMs < 1000)
        {
            return ServiceResult.InvalidParameter;
        }

        if (MaxFileSizeBytes < 1024)
        {
            return ServiceResult.InvalidParameter;
        }

        return ServiceResult.Success;
    }

    public static ServiceConfig Default => new();
}
