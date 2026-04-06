// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
namespace SXAVELinker;

public enum SXType : Byte
{
    Unknown = 0,
    PEFile = 1,
    ByteArray = 2,
    FilePath = 3,
    Directory = 4,
    Url = 5,
    FeatureVector = 6,
    Report = 7,
    Task = 8,
    TaskID = 9,
    Object = 10
}
