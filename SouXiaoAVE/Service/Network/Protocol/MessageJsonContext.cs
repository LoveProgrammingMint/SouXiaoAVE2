// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.Text.Json.Serialization;

using SouXiaoAVE.Service.Network.Protocol;
using SouXiaoAVE.Service.Network.Protocol.Messages;

namespace SouXiaoAVE.Service.Network.Protocol;

[JsonSerializable(typeof(ConnectRequest))]
[JsonSerializable(typeof(ConnectResponse))]
[JsonSerializable(typeof(DisconnectRequest))]
[JsonSerializable(typeof(DisconnectResponse))]
[JsonSerializable(typeof(SubmitTaskRequest))]
[JsonSerializable(typeof(TaskData))]
[JsonSerializable(typeof(SubmitTaskResponse))]
[JsonSerializable(typeof(ReportData))]
[JsonSerializable(typeof(GetTaskStateRequest))]
[JsonSerializable(typeof(GetTaskStateResponse))]
[JsonSerializable(typeof(GetProgressRequest))]
[JsonSerializable(typeof(GetProgressResponse))]
[JsonSerializable(typeof(GetReportRequest))]
[JsonSerializable(typeof(GetReportResponse))]
[JsonSerializable(typeof(StopTaskRequest))]
[JsonSerializable(typeof(StopTaskResponse))]
[JsonSerializable(typeof(RestartTaskRequest))]
[JsonSerializable(typeof(RestartTaskResponse))]
[JsonSerializable(typeof(ReleaseTaskRequest))]
[JsonSerializable(typeof(ReleaseTaskResponse))]
[JsonSerializable(typeof(StopAllRequest))]
[JsonSerializable(typeof(StopAllResponse))]
[JsonSerializable(typeof(RestartAllRequest))]
[JsonSerializable(typeof(RestartAllResponse))]
[JsonSerializable(typeof(ReleaseAllRequest))]
[JsonSerializable(typeof(ReleaseAllResponse))]
[JsonSerializable(typeof(HeartbeatRequest))]
[JsonSerializable(typeof(HeartbeatResponse))]
[JsonSerializable(typeof(ErrorResponse))]
internal partial class MessageJsonContext : JsonSerializerContext
{
}
