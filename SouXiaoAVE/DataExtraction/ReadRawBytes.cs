// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Text;

using SouXiaoAVE.Interfaces;

namespace SouXiaoAVE.DataExtraction;

internal class ReadRawBytes : IDataExtraction
{
    public String ExtractorName { get; set; } = "Raw Bytes";

    public List<Single> Extract(String filePath)
    {
        const int byteCount = 12288;
        var result = new List<float>();

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[byteCount];
            int bytesRead = fs.Read(buffer, 0, byteCount);

            for (int i = 0; i < bytesRead; i++)
            {
                result.Add((float)buffer[i]);
            }
        }

        return result;
    }

    public async Task<List<Single>> ExtractAsyns(String source) => await Task.Run(() => Extract(source));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

}
