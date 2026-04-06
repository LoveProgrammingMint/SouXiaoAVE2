// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.Text.Json.Serialization;

namespace SouXiaoAVE.Services;

[JsonSerializable(typeof(Dictionary<String, Object>))]
[JsonSerializable(typeof(AnalysisReport))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public sealed partial class AnalysisReportContext : JsonSerializerContext
{
}

public sealed class AnalysisReport
{
    public String GeneratedAt { get; set; } = String.Empty;
    public String EngineVersion { get; set; } = String.Empty;
    public String? FileName { get; set; }
    public Int64 FileSize { get; set; }
    public Int32 FeatureCount { get; set; }
    public Double Sparsity { get; set; }
    public Double PredictionScore { get; set; }
    public Double PredictionProbability { get; set; }
    public String PredictionLabel { get; set; } = String.Empty;
    public Boolean IsMalicious { get; set; }
}
