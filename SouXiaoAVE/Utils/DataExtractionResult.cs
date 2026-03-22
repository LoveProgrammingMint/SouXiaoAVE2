// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;

namespace SouXiaoAVE.Utils;

internal sealed class DataExtractionResult
{
    public String ExtractorName { get; init; }
    public List<Single> Features { get; init; }
    public Int32 FeatureCount => Features.Count;
    public DateTime ExtractionTime { get; init; }
    public String? SourcePath { get; init; }
    public Boolean IsValid { get; init; }

    public DataExtractionResult()
    {
        ExtractorName = String.Empty;
        Features = [];
        ExtractionTime = DateTime.UtcNow;
        IsValid = false;
    }

    public DataExtractionResult(String extractorName, List<Single> features, String? sourcePath = null)
    {
        ExtractorName = extractorName;
        Features = features;
        ExtractionTime = DateTime.UtcNow;
        SourcePath = sourcePath;
        IsValid = features.Count > 0;
    }

    public Single[] ToArray() => [.. Features];
}
