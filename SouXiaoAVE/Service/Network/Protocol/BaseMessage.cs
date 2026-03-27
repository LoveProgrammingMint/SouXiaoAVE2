// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SouXiaoAVE.Service.Network.Protocol;

public abstract class BaseMessage
{
    [JsonPropertyName("type")]
    public MessageType Type { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("messageId")]
    public Guid MessageId { get; init; }

    protected BaseMessage(MessageType type)
    {
        Type = type;
        Timestamp = DateTime.UtcNow;
        MessageId = Guid.NewGuid();
    }

    public Byte[] Serialize()
    {
        String json = JsonSerializer.Serialize(this, GetType(), MessageJsonContext.Default.Options);
        Byte[] jsonBytes = NetworkConstants.Encoding.GetBytes(json);
        Byte[] lengthBytes = BitConverter.GetBytes((UInt32)jsonBytes.Length);
        Byte[] result = new Byte[NetworkConstants.HeaderSize + jsonBytes.Length];
        Buffer.BlockCopy(lengthBytes, 0, result, 0, NetworkConstants.HeaderSize);
        Buffer.BlockCopy(jsonBytes, 0, result, NetworkConstants.HeaderSize, jsonBytes.Length);
        return result;
    }

    public static T? Deserialize<T>(Byte[] data) where T : BaseMessage
    {
        if (data.Length < NetworkConstants.HeaderSize)
        {
            return null;
        }

        Int32 jsonLength = BitConverter.ToInt32(data, 0);
        if (data.Length < NetworkConstants.HeaderSize + jsonLength)
        {
            return null;
        }

        String json = NetworkConstants.Encoding.GetString(data, NetworkConstants.HeaderSize, jsonLength);
        return JsonSerializer.Deserialize<T>(json, MessageJsonContext.Default.Options);
    }

    public static BaseMessage? Deserialize(Byte[] data, Type targetType)
    {
        if (data.Length < NetworkConstants.HeaderSize)
        {
            return null;
        }

        Int32 jsonLength = BitConverter.ToInt32(data, 0);
        if (data.Length < NetworkConstants.HeaderSize + jsonLength)
        {
            return null;
        }

        String json = NetworkConstants.Encoding.GetString(data, NetworkConstants.HeaderSize, jsonLength);
        return JsonSerializer.Deserialize(json, targetType, MessageJsonContext.Default.Options) as BaseMessage;
    }
}
