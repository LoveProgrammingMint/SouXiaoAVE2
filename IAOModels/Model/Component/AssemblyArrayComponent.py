# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Optional

from Model.public.mamba3 import Mamba3, RMSNorm
from Model.public.linear_attention import LinearAttentionLayer


class AssemblyArrayComponent(nn.Module):
    def __init__(
        self,
        embed_dim: int = 16,
        hidden_dim: int = 128,
        mamba_d_state: int = 32,
        mamba_expand: int = 2,
        mamba_headdim: int = 32,
        attn_num_heads: int = 4,
        attn_dim_head: int = 32,
        attn_dim_feedforward: int = 256,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.embed_dim = embed_dim
        self.hidden_dim = hidden_dim

        self.input_proj = nn.Linear(embed_dim, hidden_dim)

        self.conv_downsample = nn.Sequential(
            nn.Conv1d(hidden_dim, hidden_dim, kernel_size=8, stride=8, padding=0, bias=False),
            nn.BatchNorm1d(hidden_dim),
            nn.GELU(),
            nn.Conv1d(hidden_dim, hidden_dim, kernel_size=4, stride=4, padding=0, bias=False),
            nn.BatchNorm1d(hidden_dim),
            nn.GELU(),
        )

        self.linear_attn = LinearAttentionLayer(
            dim=hidden_dim,
            num_heads=attn_num_heads,
            dim_head=attn_dim_head,
            dim_feedforward=attn_dim_feedforward,
            dropout=dropout,
            use_fast=True,
            feature_dim=32,
        )

        self.mamba = Mamba3(
            d_model=hidden_dim,
            d_state=mamba_d_state,
            expand=mamba_expand,
            headdim=mamba_headdim,
            ngroups=1,
            is_mimo=False,
        )
        self.norm = RMSNorm(hidden_dim)

        self.output_norm = nn.LayerNorm(hidden_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = self.input_proj(x)

        x = x.transpose(1, 2)
        x = self.conv_downsample(x)
        x = x.transpose(1, 2)

        x = self.linear_attn(x)

        residual = x
        x = self.mamba(x)
        x = self.norm(x + residual)

        x = self.output_norm(x)

        return x
