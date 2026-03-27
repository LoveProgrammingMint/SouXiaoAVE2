// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;

using SouXiaoAVE.Linker.Enums;

namespace SouXiaoAVE.Linker.Models;

public sealed class SXTask
{
    public TaskID TaskId { get; init; }

    public SXType Type { get; init; }

    public String? FilePath { get; init; }

    public Byte[]? RawData { get; init; }

    public List<Byte>? ByteList { get; init; }

    public DateTime CreatedTime { get; init; }

    public SXTaskState State { get; set; }

    public Double Progress { get; set; }

    public Boolean IsValid { get; init; }

    private SXTask()
    {
        TaskId = TaskID.NewTaskID();
        CreatedTime = DateTime.UtcNow;
        State = SXTaskState.Pending;
        Progress = 0.0;
        IsValid = false;
    }

    public static SXTask FromFile(String filePath)
    {
        return new SXTask
        {
            Type = SXType.File,
            FilePath = filePath,
            IsValid = !String.IsNullOrEmpty(filePath)
        };
    }

    public static SXTask FromBytes(Byte[] data)
    {
        return new SXTask
        {
            Type = SXType.File,
            RawData = data,
            IsValid = data is not null && data.Length > 0
        };
    }

    public static SXTask FromByteList(List<Byte> data)
    {
        return new SXTask
        {
            Type = SXType.File,
            ByteList = data,
            IsValid = data is not null && data.Count > 0
        };
    }

    public static SXTask FromFolder(String folderPath)
    {
        return new SXTask
        {
            Type = SXType.Folder,
            FilePath = folderPath,
            IsValid = !String.IsNullOrEmpty(folderPath)
        };
    }

    public static SXTask FromZip(String zipPath)
    {
        return new SXTask
        {
            Type = SXType.Zip,
            FilePath = zipPath,
            IsValid = !String.IsNullOrEmpty(zipPath)
        };
    }
}
