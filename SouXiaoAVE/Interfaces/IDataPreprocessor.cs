// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;

namespace SouXiaoAVE.Interfaces;

internal interface IDataPreprocessor : IDisposable
{
    public String PreprocessorName { get; set; }

    public List<Single> Preprocess(List<Single> rawData);

    public List<Single> Normalize(List<Single> data);

    public List<Single> PadOrTruncate(List<Single> data, Int32 targetLength, Single padValue = 0f);
}
