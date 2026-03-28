# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn

from Model.public.mamba3 import Mamba3, RMSNorm
from Model.public.linear_attention import LinearAttentionLayer


class CharWolfComponent(nn.Module):
    def __init__(
        self,
        embed_dim: int = 16,
        hidden_dim: int = 64,
        mamba_d_state: int = 32,
        mamba_expand: int = 2,
        mamba_headdim: int = 32,
        attn_num_heads: int = 4,
        attn_dim_head: int = 32,
        attn_dim_feedforward: int = 128,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.embed_dim = embed_dim
        self.hidden_dim = hidden_dim

        self.input_proj = nn.Linear(embed_dim, hidden_dim)

        self.mamba1 = Mamba3(
            d_model=hidden_dim,
            d_state=mamba_d_state,
            expand=mamba_expand,
            headdim=mamba_headdim,
            ngroups=1,
            is_mimo=False,
        )
        self.norm1 = RMSNorm(hidden_dim)

        self.mamba2 = Mamba3(
            d_model=hidden_dim,
            d_state=mamba_d_state,
            expand=mamba_expand,
            headdim=mamba_headdim,
            ngroups=1,
            is_mimo=False,
        )
        self.norm2 = RMSNorm(hidden_dim)

        self.downsample1 = nn.Conv1d(
            in_channels=hidden_dim,
            out_channels=hidden_dim,
            kernel_size=2,
            stride=2,
            padding=0,
            bias=False,
        )

        self.mamba3 = Mamba3(
            d_model=hidden_dim,
            d_state=mamba_d_state,
            expand=mamba_expand,
            headdim=mamba_headdim,
            ngroups=1,
            is_mimo=False,
        )
        self.norm3 = RMSNorm(hidden_dim)

        self.downsample2 = nn.Conv1d(
            in_channels=hidden_dim,
            out_channels=hidden_dim,
            kernel_size=2,
            stride=2,
            padding=0,
            bias=False,
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

        self.output_norm = nn.LayerNorm(hidden_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = self.input_proj(x)

        residual = x
        x = self.mamba1(x)
        x = self.norm1(x + residual)

        residual = x
        x = self.mamba2(x)
        x = self.norm2(x + residual)

        x = x.transpose(1, 2)
        x = self.downsample1(x)
        x = x.transpose(1, 2)

        residual = x
        x = self.mamba3(x)
        x = self.norm3(x + residual)

        x = x.transpose(1, 2)
        x = self.downsample2(x)
        x = x.transpose(1, 2)

        x = self.linear_attn(x)

        x = self.output_norm(x)

        return x
