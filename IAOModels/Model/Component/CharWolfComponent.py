# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn

from Model.public.transformer import Transformer1DLayer
from Model.public.linear_attention import LinearAttentionLayer


class ConvBlock1D(nn.Module):
    def __init__(self, hidden_dim: int, expansion: int = 4):
        super().__init__()
        self.dwconv = nn.Conv1d(hidden_dim, hidden_dim, kernel_size=7, padding=3, groups=hidden_dim, bias=False)
        self.norm = nn.LayerNorm(hidden_dim)
        self.pwconv1 = nn.Linear(hidden_dim, hidden_dim * expansion)
        self.act = nn.GELU()
        self.pwconv2 = nn.Linear(hidden_dim * expansion, hidden_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        residual = x
        x = self.dwconv(x)
        x = x.transpose(1, 2)
        x = self.norm(x)
        x = self.pwconv1(x)
        x = self.act(x)
        x = self.pwconv2(x)
        x = x.transpose(1, 2)
        return x + residual


class CharWolfComponent(nn.Module):
    def __init__(
        self,
        embed_dim: int = 16,
        hidden_dim: int = 96,
        num_conv_blocks: int = 3,
        num_transformer_layers: int = 2,
        transformer_nhead: int = 4,
        transformer_dim_feedforward: int = 192,
        attn_num_heads: int = 4,
        attn_dim_head: int = 24,
        attn_dim_feedforward: int = 192,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.embed_dim = embed_dim
        self.hidden_dim = hidden_dim

        self.input_proj = nn.Linear(embed_dim, hidden_dim)

        self.conv_blocks = nn.ModuleList([
            ConvBlock1D(hidden_dim, expansion=4) for _ in range(num_conv_blocks)
        ])

        self.downsample1 = nn.Conv1d(
            in_channels=hidden_dim,
            out_channels=hidden_dim,
            kernel_size=2,
            stride=2,
            padding=0,
            bias=False,
        )

        self.transformer_layers1 = nn.ModuleList([
            Transformer1DLayer(
                d_model=hidden_dim,
                nhead=transformer_nhead,
                dim_feedforward=transformer_dim_feedforward,
                dropout=dropout,
                activation="gelu",
                batch_first=True,
            )
            for _ in range(num_transformer_layers)
        ])

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

        x = x.transpose(1, 2)
        for block in self.conv_blocks:
            x = block(x)
        x = x.transpose(1, 2)

        for layer in self.transformer_layers1:
            x = layer(x)

        x = x.transpose(1, 2)
        x = self.downsample1(x)
        x = x.transpose(1, 2)

        x = x.transpose(1, 2)
        x = self.downsample2(x)
        x = x.transpose(1, 2)

        x = self.linear_attn(x)

        x = self.output_norm(x)

        return x
