// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SouXiaoAVE.Utils;

public static class EntropyCalculator
{
    public static Double Calculate(Byte[] data)
    {
        if (data is null || data.Length == 0)
            return 0.0;

        Int32[] frequency = new Int32[256];
        foreach (Byte b in data)
        {
            frequency[b]++;
        }

        Double entropy = 0.0;
        Double len = (Double)data.Length;

        for (Int32 i = 0; i < 256; i++)
        {
            if (frequency[i] > 0)
            {
                Double p = frequency[i] / len;
                entropy -= p * Math.Log2(p);
            }
        }

        return entropy;
    }

    public static Double Calculate(ReadOnlySpan<Byte> data)
    {
        if (data.IsEmpty)
            return 0.0;

        Int32[] frequency = new Int32[256];
        foreach (Byte b in data)
        {
            frequency[b]++;
        }

        Double entropy = 0.0;
        Double len = (Double)data.Length;

        for (Int32 i = 0; i < 256; i++)
        {
            if (frequency[i] > 0)
            {
                Double p = frequency[i] / len;
                entropy -= p * Math.Log2(p);
            }
        }

        return entropy;
    }

    public static Double CalculateFromString(String data)
    {
        if (String.IsNullOrEmpty(data))
            return 0.0;

        Dictionary<Char, Int32> frequency = [];
        foreach (Char c in data)
        {
            if (frequency.TryGetValue(c, out Int32 value))
                frequency[c] = ++value;
            else
                frequency[c] = 1;
        }

        Double entropy = 0.0;
        Double len = (Double)data.Length;

        foreach (KeyValuePair<Char, Int32> kvp in frequency)
        {
            Double p = kvp.Value / len;
            entropy -= p * Math.Log2(p);
        }

        return entropy;
    }
}
