// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SouXiaoAVE.Service.Network.Protocol;

public enum MessageType : UInt16
{
    None = 0,
    ConnectRequest = 1,
    ConnectResponse = 2,
    DisconnectRequest = 3,
    DisconnectResponse = 4,
    SubmitTaskRequest = 5,
    SubmitTaskResponse = 6,
    GetTaskStateRequest = 7,
    GetTaskStateResponse = 8,
    GetProgressRequest = 9,
    GetProgressResponse = 10,
    GetReportRequest = 11,
    GetReportResponse = 12,
    StopTaskRequest = 13,
    StopTaskResponse = 14,
    RestartTaskRequest = 15,
    RestartTaskResponse = 16,
    ReleaseTaskRequest = 17,
    ReleaseTaskResponse = 18,
    StopAllRequest = 19,
    StopAllResponse = 20,
    RestartAllRequest = 21,
    RestartAllResponse = 22,
    ReleaseAllRequest = 23,
    ReleaseAllResponse = 24,
    HeartbeatRequest = 25,
    HeartbeatResponse = 26,
    ErrorResponse = 100
}
