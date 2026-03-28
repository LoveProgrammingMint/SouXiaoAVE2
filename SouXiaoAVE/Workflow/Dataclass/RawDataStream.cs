// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Text;

namespace SouXiaoAVE.Workflow.Dataclass
{
    internal record class RawDataStream
    {

        public List<Single> Entropys = [];

        public List<Single> RawBytes = [];

        public List<Single> StatisticalInformations = [];

        public List<Single> AssemblyArray = [];

    }
}
