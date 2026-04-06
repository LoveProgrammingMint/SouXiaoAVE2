// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SouXiaoAVE.Utils;

public static class MurmurHash3
{
    public static UInt32 Hash32(Byte[] data, UInt32 seed = 0)
    {
        const UInt32 c1 = 0xcc9e2d51;
        const UInt32 c2 = 0x1b873593;

        UInt32 h1 = seed;
        Int32 length = data.Length;
        Int32 blockCount = length / 4;

        for (Int32 i = 0; i < blockCount; i++)
        {
            UInt32 k1 = BitConverter.ToUInt32(data, i * 4);

            k1 *= c1;
            k1 = RotateLeft(k1, 15);
            k1 *= c2;

            h1 ^= k1;
            h1 = RotateLeft(h1, 13);
            h1 = h1 * 5 + 0xe6546b64;
        }

        ReadOnlySpan<Byte> tail = data.AsSpan(blockCount * 4);
        UInt32 k2 = 0;

        switch (tail.Length)
        {
            case 3:
                k2 ^= (UInt32)tail[2] << 16;
                goto case 2;
            case 2:
                k2 ^= (UInt32)tail[1] << 8;
                goto case 1;
            case 1:
                k2 ^= tail[0];
                k2 *= c1;
                k2 = RotateLeft(k2, 15);
                k2 *= c2;
                h1 ^= k2;
                break;
        }

        h1 ^= (UInt32)length;

        h1 = FMix(h1);

        return h1;
    }

    public static UInt32 HashString(String str, UInt32 seed = 0)
    {
        return Hash32(System.Text.Encoding.UTF8.GetBytes(str), seed);
    }

    private static UInt32 RotateLeft(UInt32 x, Byte r)
    {
        return (x << r) | (x >> (32 - r));
    }

    private static UInt32 FMix(UInt32 h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    }
}
