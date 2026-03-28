// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using SouXiaoAVE.Interfaces;

namespace SouXiaoAVE.DataExtraction;

internal sealed class YaraScanExtractor : IDataExtraction
{
    public String ExtractorName { get; set; } = "YARA Scan Features";
    public IDataPreprocessor? Preprocessor { get; set; }

    private readonly Yara.YaraScanner _scanner;
    private Boolean _disposed = false;

    public YaraScanExtractor()
    {
        _scanner = new Yara.YaraScanner();
    }

    public void Initialize()
    {
        _scanner.Initialize();
    }

    public void AddRulesFromFile(String filePath)
    {
        _scanner.AddRulesFromFile(filePath);
    }

    public void AddRulesFromString(String rules)
    {
        _scanner.AddRulesFromString(rules);
    }

    public void CompileRules()
    {
        _scanner.CompileRules();
    }

    public void LoadCompiledRules(String filePath)
    {
        _scanner.LoadCompiledRules(filePath);
    }

    public void SaveCompiledRules(String filePath)
    {
        _scanner.SaveCompiledRules(filePath);
    }

    public List<Single> Extract(String source)
    {
        if (!File.Exists(source))
        {
            return [];
        }

        List<Single> features = [];

        try
        {
            List<Yara.YaraScanner.YaraMatch> matches = _scanner.ScanFile(source);

            features.Add(matches.Count > 0 ? 1.0f : 0.0f);
            features.Add(Convert.ToSingle(matches.Count));

            foreach (Yara.YaraScanner.YaraMatch match in matches)
            {
                features.Add(Convert.ToSingle(match.RuleName.Length));
                features.Add(Convert.ToSingle(match.Tags.Count));
                features.Add(Convert.ToSingle(match.Metadata.Count));
            }

            const Int32 maxFeatures = 256;
            while (features.Count < maxFeatures)
            {
                features.Add(0.0f);
            }

            if (features.Count > maxFeatures)
            {
                features = features.GetRange(0, maxFeatures);
            }
        }
        catch (Exception)
        {
            return [];
        }

        return Preprocessor?.Preprocess(features) ?? features;
    }

    public List<Single> ExtractFromMemory(Byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            return [];
        }

        List<Single> features = [];

        try
        {
            List<Yara.YaraScanner.YaraMatch> matches = _scanner.ScanMemory(data);

            features.Add(matches.Count > 0 ? 1.0f : 0.0f);
            features.Add(Convert.ToSingle(matches.Count));

            foreach (Yara.YaraScanner.YaraMatch match in matches)
            {
                features.Add(Convert.ToSingle(match.RuleName.Length));
                features.Add(Convert.ToSingle(match.Tags.Count));
                features.Add(Convert.ToSingle(match.Metadata.Count));
            }

            const Int32 maxFeatures = 256;
            while (features.Count < maxFeatures)
            {
                features.Add(0.0f);
            }

            if (features.Count > maxFeatures)
            {
                features = features.GetRange(0, maxFeatures);
            }
        }
        catch (Exception)
        {
            return [];
        }

        return Preprocessor?.Preprocess(features) ?? features;
    }

    public async Task<List<Single>> ExtractAsync(String source) => await Task.Run(() => Extract(source));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _scanner.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
