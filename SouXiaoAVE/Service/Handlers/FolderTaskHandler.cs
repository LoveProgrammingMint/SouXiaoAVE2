// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Threading.Tasks;

using SouXiaoAVE.Linker.Enums;
using SouXiaoAVE.Linker.Models;

namespace SouXiaoAVE.Service.Handlers;

public sealed class FolderTaskHandler : ITaskHandler
{
    private Boolean _disposed = false;
    private Boolean _cancelled = false;

    public SXType SupportedType => SXType.Folder;

    public String HandlerName => "Folder Scan Handler";

    public Boolean CanHandle(SXTask task) => task.Type == SXType.Folder && task.IsValid;

    public async Task<Report> ProcessAsync(SXTask task, IProgress<Double>? progress = null)
    {
        return await Task.Run(() =>
        {
            _cancelled = false;

            if (String.IsNullOrEmpty(task.FilePath))
            {
                return Report.CreateEmpty();
            }

            progress?.Report(0.0);

            if (_cancelled)
            {
                return Report.CreateEmpty();
            }

            progress?.Report(0.25);

            if (_cancelled)
            {
                return Report.CreateEmpty();
            }

            progress?.Report(0.5);

            if (_cancelled)
            {
                return Report.CreateEmpty();
            }

            progress?.Report(0.75);

            if (_cancelled)
            {
                return Report.CreateEmpty();
            }

            progress?.Report(1.0);

            return Report.CreateClean(task.TaskId, task.FilePath, task.Type, 0, String.Empty);
        });
    }

    public void Cancel() => _cancelled = true;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
