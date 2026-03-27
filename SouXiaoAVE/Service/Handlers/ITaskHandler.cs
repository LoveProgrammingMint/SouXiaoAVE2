// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Threading.Tasks;

using SouXiaoAVE.Linker.Enums;
using SouXiaoAVE.Linker.Models;

namespace SouXiaoAVE.Service.Handlers;

public interface ITaskHandler : IDisposable
{
    SXType SupportedType { get; }

    String HandlerName { get; }

    Task<Report> ProcessAsync(SXTask task, IProgress<Double>? progress = null);

    Boolean CanHandle(SXTask task);

    void Cancel();
}
