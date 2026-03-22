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

internal class ReadRawBytes : IDataExtraction
{
    public String ExtractorName { get; set; } = "Raw Bytes";
    public IDataPreprocessor? Preprocessor { get; set; }

    public List<Single> Extract(String filePath)
    {
        const Int32 byteCount = 12288;
        List<Single> result = [];

        using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
        Byte[] buffer = new Byte[byteCount];
        Int32 bytesRead = fs.Read(buffer, 0, byteCount);

        for (Int32 i = 0; i < bytesRead; i++)
        {
            result.Add((Single)buffer[i]);
        }

        return Preprocessor?.Preprocess(result) ?? result;
    }

    public async Task<List<Single>> ExtractAsync(String source) => await Task.Run(() => Extract(source));

    public void Dispose() => GC.SuppressFinalize(this);
}
