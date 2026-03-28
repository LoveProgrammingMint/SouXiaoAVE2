// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;

using SouXiaoAVE.Model;

namespace SouXiaoAVE.Utils;

public sealed class ModelManager : IDisposable
{
    private static ModelManager? _instance;
    private static readonly Lock _lock = new();

    private Boolean _disposed = false;
    private readonly Dictionary<Int32, String> _modelPaths = [];

    public static ModelManager Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new ModelManager();
                return _instance;
            }
        }
    }

    private ModelManager() { }

    public Boolean RegisterModelPath(Int32 id, String path)
    {
        if (String.IsNullOrEmpty(path))
        {
            return false;
        }

        lock (_lock)
        {
            if (_modelPaths.ContainsKey(id))
            {
                return false;
            }
            _modelPaths[id] = path;
            return true;
        }
    }

    public Boolean UnregisterModelPath(Int32 id)
    {
        lock (_lock)
        {
            return _modelPaths.Remove(id);
        }
    }

    public String? GetModelPath(Int32 id)
    {
        lock (_lock)
        {
            if (_modelPaths.TryGetValue(id, out String? path))
            {
                return path;
            }
            return null;
        }
    }

    public Boolean LoadModel(Int32 id)
    {
        lock (_lock)
        {
            if (!_modelPaths.TryGetValue(id, out String? path))
            {
                return false;
            }

            try
            {
                ModelLoader.LoadModel(path, id);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public Boolean LoadAllModels()
    {
        Boolean allSuccess = true;

        lock (_lock)
        {
            foreach (KeyValuePair<Int32, String> kvp in _modelPaths)
            {
                try
                {
                    ModelLoader.LoadModel(kvp.Value, kvp.Key);
                }
                catch (Exception)
                {
                    allSuccess = false;
                }
            }
        }

        return allSuccess;
    }

    public InferenceSession? GetSession(String modelName, Int32 id)
    {
        return ModelLoader.GetInferSession(modelName, id);
    }

    public Boolean IsModelLoaded(Int32 id)
    {
        return ModelLoader.IsModelLoaded(id);
    }

    public Boolean IsModelLoaded(String modelName, Int32 id)
    {
        return ModelLoader.IsModelLoaded(modelName, id);
    }

    public IReadOnlyCollection<String> GetLoadedModelNames(Int32 id)
    {
        return ModelLoader.GetLoadedModelNames(id);
    }

    public IReadOnlyCollection<Int32> GetLoadedModelIds()
    {
        return ModelLoader.GetLoadedModelIds();
    }

    public Int32 GetModelCount(Int32 id)
    {
        return ModelLoader.GetModelCount(id);
    }

    public Int32 GetTotalModelCount()
    {
        return ModelLoader.GetTotalModelCount();
    }

    public Boolean UnloadModel(String modelName, Int32 id)
    {
        return ModelLoader.UnloadModel(modelName, id);
    }

    public void UnloadAllModels(Int32 id)
    {
        ModelLoader.UnloadAllModels(id);
    }

    public void UnloadAll()
    {
        ModelLoader.UnloadAll();
    }

    public Boolean ReloadModel(Int32 id)
    {
        lock (_lock)
        {
            if (!_modelPaths.TryGetValue(id, out String? path))
            {
                return false;
            }

            try
            {
                ModelLoader.ReloadModel(path, id);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public Boolean ContainsModel(String modelName, Int32 id)
    {
        return ModelLoader.ContainsModel(modelName, id);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnloadAll();

        lock (_lock)
        {
            _modelPaths.Clear();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
