// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using SouXiaoAVE.Interfaces;

namespace SouXiaoAVE.DataExtraction;

internal class CalculateEntropy : IDataExtraction
{
    public String ExtractorName { get; set; } = "Entropy Map";

    public List<Single> Extract(String filePath)
    {
        const Int32 segmentSize = 16384;
        const Int32 segmentCount = 1024;
        const Single maxEntropy = 8.0f;

        List<Single> entropies = [];
        Byte[] buffer = new Byte[segmentSize];

        using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
        for (Int32 i = 0; i < segmentCount; i++)
        {
            Int32 bytesRead = fs.Read(buffer, 0, segmentSize);

            if (bytesRead == 0)
            {
                entropies.Add(0f);
                continue;
            }

            Int32[] freq = new Int32[256];
            for (Int32 j = 0; j < bytesRead; j++)
            {
                freq[buffer[j]]++;
            }

            Double entropy = 0.0;
            for (Int32 k = 0; k < 256; k++)
            {
                if (freq[k] > 0)
                {
                    Double p = (Double)freq[k] / bytesRead;
                    entropy -= p * Math.Log(p, 2);
                }
            }

            Single normalized = (Single)(entropy / maxEntropy * 10.0);
            if (normalized > 10f) normalized = 10f;
            entropies.Add(normalized);
        }

        return entropies;
    }

    public async Task<List<Single>> ExtractAsyns(String source) => await Task.Run(() => Extract(source));

    public void Dispose() => GC.SuppressFinalize(this);
}
