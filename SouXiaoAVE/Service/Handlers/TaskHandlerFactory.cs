// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;

using SouXiaoAVE.Linker.Enums;
using SouXiaoAVE.Linker.Models;

namespace SouXiaoAVE.Service.Handlers;

public sealed class TaskHandlerFactory : IDisposable
{
    private readonly Dictionary<SXType, ITaskHandler> _handlers = [];
    private readonly Object _lock = new();
    private Boolean _disposed = false;

    public TaskHandlerFactory()
    {
        RegisterHandler(new FileTaskHandler());
        RegisterHandler(new FolderTaskHandler());
        RegisterHandler(new ZipTaskHandler());
    }

    public void RegisterHandler(ITaskHandler handler)
    {
        lock (_lock)
        {
            _handlers[handler.SupportedType] = handler;
        }
    }

    public ITaskHandler? GetHandler(SXType type)
    {
        lock (_lock)
        {
            return _handlers.TryGetValue(type, out ITaskHandler? handler) ? handler : null;
        }
    }

    public ITaskHandler? GetHandler(SXTask task)
    {
        lock (_lock)
        {
            foreach (KeyValuePair<SXType, ITaskHandler> kvp in _handlers)
            {
                if (kvp.Value.CanHandle(task))
                {
                    return kvp.Value;
                }
            }
            return null;
        }
    }

    public Boolean HasHandler(SXType type)
    {
        lock (_lock)
        {
            return _handlers.ContainsKey(type);
        }
    }

    public IEnumerable<SXType> GetSupportedTypes()
    {
        lock (_lock)
        {
            return new List<SXType>(_handlers.Keys);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            foreach (KeyValuePair<SXType, ITaskHandler> kvp in _handlers)
            {
                kvp.Value.Dispose();
            }
            _handlers.Clear();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
