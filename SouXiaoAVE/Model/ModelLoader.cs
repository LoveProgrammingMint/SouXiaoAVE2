// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.ML.OnnxRuntime;

namespace SouXiaoAVE.Model;

public sealed class ModelLoader
{
    private static readonly Object _lock = new();
    private static readonly Dictionary<Int32, Dictionary<String, InferenceSession>> _modelCache = new();

    public static ModelLoader Instance { get; } = new ModelLoader();

    private ModelLoader() { }

    public static ModelLoader LoadModel(String path, Int32 id)
    {
        if (String.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        lock (_lock)
        {
        if (!_modelCache.ContainsKey(id))
        {
            _modelCache[id] = new Dictionary<String, InferenceSession>();
        }

        Dictionary<String, InferenceSession> modelDict = _modelCache[id];
        String[] onnxFiles = Directory.GetFiles(path, "*.onnx");

        foreach (String filePath in onnxFiles)
        {
            String modelName = Path.GetFileNameWithoutExtension(filePath);

            if (!modelDict.ContainsKey(modelName))
            {
                InferenceSession session = new InferenceSession(filePath);
                modelDict[modelName] = session;
            }
        }
        }

        return Instance;
    }

    public static InferenceSession? GetInferSession(String name, Int32 id)
    {
        if (String.IsNullOrEmpty(name))
        {
            return null;
        }

        lock (_lock)
        {
        if (_modelCache.TryGetValue(id, out Dictionary<String, InferenceSession>? modelDict))
        {
            if (modelDict.TryGetValue(name, out InferenceSession? session))
            {
                return session;
            }
        }
        }

        return null;
    }

    public static Boolean IsModelLoaded(Int32 id)
    {
        lock (_lock)
        {
            return _modelCache.ContainsKey(id);
        }
    }

    public static Boolean IsModelLoaded(String name, Int32 id)
    {
        lock (_lock)
        {
            if (_modelCache.TryGetValue(id, out Dictionary<String, InferenceSession>? modelDict))
            {
                return modelDict.ContainsKey(name);
            }
            return false;
        }
    }

    public static IReadOnlyCollection<String> GetLoadedModelNames(Int32 id)
    {
        lock (_lock)
        {
            if (_modelCache.TryGetValue(id, out Dictionary<String, InferenceSession>? modelDict))
            {
                return modelDict.Keys;
            }
            return Array.Empty<String>();
        }
    }

    public static IReadOnlyCollection<Int32> GetLoadedModelIds()
    {
        lock (_lock)
        {
            return _modelCache.Keys;
        }
    }

    public static Int32 GetModelCount(Int32 id)
    {
        lock (_lock)
        {
            if (_modelCache.TryGetValue(id, out Dictionary<String, InferenceSession>? modelDict))
            {
                return modelDict.Count;
            }
            return 0;
        }
    }

    public static Int32 GetTotalModelCount()
    {
        lock (_lock)
        {
            Int32 count = 0;
            foreach (KeyValuePair<Int32, Dictionary<String, InferenceSession>> kvp in _modelCache)
            {
                count += kvp.Value.Count;
            }
            return count;
        }
    }

    public static Boolean UnloadModel(String name, Int32 id)
    {
        lock (_lock)
        {
            if (_modelCache.TryGetValue(id, out Dictionary<String, InferenceSession>? modelDict))
            {
                if (modelDict.TryGetValue(name, out InferenceSession? session))
                {
                    session.Dispose();
                    modelDict.Remove(name);
                    return true;
                }
            }
            return false;
        }
    }

    public static void UnloadAllModels(Int32 id)
    {
        lock (_lock)
        {
            if (_modelCache.TryGetValue(id, out Dictionary<String, InferenceSession>? modelDict))
            {
                foreach (KeyValuePair<String, InferenceSession> kvp in modelDict)
                {
                    kvp.Value.Dispose();
                }
                modelDict.Clear();
                _modelCache.Remove(id);
            }
        }
    }

    public static void UnloadAll()
    {
        lock (_lock)
        {
            foreach (KeyValuePair<Int32, Dictionary<String, InferenceSession>> idKvp in _modelCache)
            {
                foreach (KeyValuePair<String, InferenceSession> modelKvp in idKvp.Value)
                {
                    modelKvp.Value.Dispose();
                }
                idKvp.Value.Clear();
            }
            _modelCache.Clear();
        }
    }

    public static void ReloadModel(String path, Int32 id)
    {
        UnloadAllModels(id);
        LoadModel(path, id);
    }

    public static Boolean ContainsModel(String name, Int32 id)
    {
        lock (_lock)
        {
            if (_modelCache.TryGetValue(id, out Dictionary<String, InferenceSession>? modelDict))
            {
                return modelDict.ContainsKey(name);
            }
            return false;
        }
    }
}
