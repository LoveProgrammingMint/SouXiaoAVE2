// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.Text;
using System.Text.Json;

namespace SouXiaoAVE.Service.Network.Protocol;

public static class NetworkConstants
{
    public const Int32 HeaderSize = 4;

    public const Int32 DefaultPort = 9527;

    public const Int32 BufferSize = 65536;

    public const Int32 TimeoutMs = 30000;

    public static readonly Encoding Encoding = Encoding.UTF8;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };
}
