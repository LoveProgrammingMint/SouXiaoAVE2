// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SouXiaoAVE.Interfaces;

internal interface IDataExtraction : IDisposable
{
    public String ExtractorName { get; set; }

    public IDataPreprocessor? Preprocessor { get; set; }

    public List<Single> Extract(String source);

    public async Task<List<Single>> ExtractAsync(String source) => await Task.Run(() => Extract(source));
}
