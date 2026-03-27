// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using SouXiaoAVE.Linker.Enums;

namespace SouXiaoAVE.Service.Network.Protocol.Messages;

public sealed class ConnectRequest : BaseMessage
{
    [JsonPropertyName("clientId")]
    public Guid ClientId { get; set; }

    [JsonPropertyName("version")]
    public String Version { get; set; }

    public ConnectRequest() : base(MessageType.ConnectRequest)
    {
        ClientId = Guid.NewGuid();
        Version = "1.0.0";
    }
}

public sealed class ConnectResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    [JsonPropertyName("serverVersion")]
    public String ServerVersion { get; set; }

    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    public ConnectResponse() : base(MessageType.ConnectResponse)
    {
        ServerVersion = "1.0.0";
        SessionId = Guid.NewGuid();
    }
}

public sealed class DisconnectRequest : BaseMessage
{
    public DisconnectRequest() : base(MessageType.DisconnectRequest) { }
}

public sealed class DisconnectResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    public DisconnectResponse() : base(MessageType.DisconnectResponse) { }
}

public sealed class SubmitTaskRequest : BaseMessage
{
    [JsonPropertyName("tasks")]
    public List<TaskData> Tasks { get; set; }

    public SubmitTaskRequest() : base(MessageType.SubmitTaskRequest)
    {
        Tasks = [];
    }
}

public sealed class TaskData
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    [JsonPropertyName("type")]
    public SXType Type { get; set; }

    [JsonPropertyName("filePath")]
    public String? FilePath { get; set; }

    [JsonPropertyName("rawData")]
    public Byte[]? RawData { get; set; }
}

public sealed class SubmitTaskResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    [JsonPropertyName("taskIds")]
    public List<Guid> TaskIds { get; set; }

    [JsonPropertyName("quickReport")]
    public ReportData? QuickReport { get; set; }

    [JsonPropertyName("isQuickMode")]
    public Boolean IsQuickMode { get; set; }

    public SubmitTaskResponse() : base(MessageType.SubmitTaskResponse)
    {
        TaskIds = [];
    }
}

public sealed class ReportData
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    [JsonPropertyName("filePath")]
    public String? FilePath { get; set; }

    [JsonPropertyName("type")]
    public SXType Type { get; set; }

    [JsonPropertyName("isMalicious")]
    public Boolean IsMalicious { get; set; }

    [JsonPropertyName("confidence")]
    public Double Confidence { get; set; }

    [JsonPropertyName("threatName")]
    public String? ThreatName { get; set; }

    [JsonPropertyName("threatType")]
    public String? ThreatType { get; set; }

    [JsonPropertyName("fileSize")]
    public Int64 FileSize { get; set; }

    [JsonPropertyName("fileHash")]
    public String? FileHash { get; set; }
}

public sealed class GetTaskStateRequest : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public GetTaskStateRequest() : base(MessageType.GetTaskStateRequest) { }
}

public sealed class GetTaskStateResponse : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    [JsonPropertyName("state")]
    public SXTaskState State { get; set; }

    public GetTaskStateResponse() : base(MessageType.GetTaskStateResponse) { }
}

public sealed class GetProgressRequest : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public GetProgressRequest() : base(MessageType.GetProgressRequest) { }
}

public sealed class GetProgressResponse : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    [JsonPropertyName("progress")]
    public Double Progress { get; set; }

    public GetProgressResponse() : base(MessageType.GetProgressResponse) { }
}

public sealed class GetReportRequest : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public GetReportRequest() : base(MessageType.GetReportRequest) { }
}

public sealed class GetReportResponse : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    [JsonPropertyName("report")]
    public ReportData? Report { get; set; }

    [JsonPropertyName("found")]
    public Boolean Found { get; set; }

    public GetReportResponse() : base(MessageType.GetReportResponse) { }
}

public sealed class StopTaskRequest : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public StopTaskRequest() : base(MessageType.StopTaskRequest) { }
}

public sealed class StopTaskResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public StopTaskResponse() : base(MessageType.StopTaskResponse) { }
}

public sealed class RestartTaskRequest : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public RestartTaskRequest() : base(MessageType.RestartTaskRequest) { }
}

public sealed class RestartTaskResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public RestartTaskResponse() : base(MessageType.RestartTaskResponse) { }
}

public sealed class ReleaseTaskRequest : BaseMessage
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public ReleaseTaskRequest() : base(MessageType.ReleaseTaskRequest) { }
}

public sealed class ReleaseTaskResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    [JsonPropertyName("taskId")]
    public Guid TaskId { get; set; }

    public ReleaseTaskResponse() : base(MessageType.ReleaseTaskResponse) { }
}

public sealed class StopAllRequest : BaseMessage
{
    public StopAllRequest() : base(MessageType.StopAllRequest) { }
}

public sealed class StopAllResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    public StopAllResponse() : base(MessageType.StopAllResponse) { }
}

public sealed class RestartAllRequest : BaseMessage
{
    public RestartAllRequest() : base(MessageType.RestartAllRequest) { }
}

public sealed class RestartAllResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    public RestartAllResponse() : base(MessageType.RestartAllResponse) { }
}

public sealed class ReleaseAllRequest : BaseMessage
{
    public ReleaseAllRequest() : base(MessageType.ReleaseAllRequest) { }
}

public sealed class ReleaseAllResponse : BaseMessage
{
    [JsonPropertyName("success")]
    public Boolean Success { get; set; }

    public ReleaseAllResponse() : base(MessageType.ReleaseAllResponse) { }
}

public sealed class HeartbeatRequest : BaseMessage
{
    public HeartbeatRequest() : base(MessageType.HeartbeatRequest) { }
}

public sealed class HeartbeatResponse : BaseMessage
{
    [JsonPropertyName("serverTime")]
    public DateTime ServerTime { get; set; }

    public HeartbeatResponse() : base(MessageType.HeartbeatResponse)
    {
        ServerTime = DateTime.UtcNow;
    }
}

public sealed class ErrorResponse : BaseMessage
{
    [JsonPropertyName("errorCode")]
    public Int32 ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public String ErrorMessage { get; set; }

    [JsonPropertyName("originalMessageId")]
    public Guid OriginalMessageId { get; set; }

    public ErrorResponse() : base(MessageType.ErrorResponse)
    {
        ErrorMessage = String.Empty;
    }
}
