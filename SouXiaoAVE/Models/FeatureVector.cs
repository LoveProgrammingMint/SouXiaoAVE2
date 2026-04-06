// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SouXiaoAVE.Models;

public sealed class FeatureVector
{
    public const Int32 TotalDimensions = 512;

    public Double[] Features { get; }

    public FeatureVector()
    {
        Features = new Double[TotalDimensions];
    }

    public FeatureVector(Double[] features)
    {
        if (features.Length != TotalDimensions)
            throw new ArgumentException($"Feature vector must have exactly {TotalDimensions} dimensions, but got {features.Length}");
        Features = features;
    }

    public Double this[Int32 index]
    {
        get => Features[index];
        set => Features[index] = value;
    }

    public static implicit operator Double[](FeatureVector fv) => fv.Features;
}
