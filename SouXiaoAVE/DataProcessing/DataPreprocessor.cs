// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Linq;

using SouXiaoAVE.Interfaces;

namespace SouXiaoAVE.DataProcessing;

internal sealed class DataPreprocessor : IDataPreprocessor
{
    public String PreprocessorName { get; set; } = "Default Preprocessor";

    public List<Single> Preprocess(List<Single> rawData)
    {
        if (rawData == null || rawData.Count == 0)
        {
            return [];
        }

        List<Single> processed = [.. rawData];
        return processed;
    }

    public List<Single> Normalize(List<Single> data)
    {
        if (data == null || data.Count == 0)
        {
            return [];
        }

        Single min = data.Min();
        Single max = data.Max();
        Single range = max - min;

        if (range == 0f)
        {
            return [.. data.Select(_ => 0f)];
        }

        return [.. data.Select(x => (x - min) / range)];
    }

    public List<Single> PadOrTruncate(List<Single> data, Int32 targetLength, Single padValue = 0f)
    {
        if (data == null)
        {
            return PadOrTruncate([], targetLength, padValue);
        }

        if (data.Count == targetLength)
        {
            return [.. data];
        }

        if (data.Count > targetLength)
        {
            return [.. data.Take(targetLength)];
        }

        List<Single> result = [.. data];
        Int32 padCount = targetLength - data.Count;
        for (Int32 i = 0; i < padCount; i++)
        {
            result.Add(padValue);
        }

        return result;
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
