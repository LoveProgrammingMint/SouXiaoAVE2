# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Optional

from Model.public.mamba3 import Mamba3, RMSNorm


class Transformer1DLayer(nn.Module):
    def __init__(
        self,
        d_model: int,
        nhead: int = 8,
        dim_feedforward: int = 2048,
        dropout: float = 0.1,
        activation: str = "gelu",
        batch_first: bool = True,
    ) -> None:
        super().__init__()
        self.self_attn = nn.MultiheadAttention(
            embed_dim=d_model,
            num_heads=nhead,
            dropout=dropout,
            batch_first=batch_first,
        )
        self.linear1 = nn.Linear(d_model, dim_feedforward)
        self.dropout = nn.Dropout(dropout)
        self.linear2 = nn.Linear(dim_feedforward, d_model)

        self.norm1 = nn.LayerNorm(d_model)
        self.norm2 = nn.LayerNorm(d_model)
        self.dropout1 = nn.Dropout(dropout)
        self.dropout2 = nn.Dropout(dropout)

        if activation == "gelu":
            self.activation = F.gelu
        elif activation == "relu":
            self.activation = F.relu
        else:
            self.activation = F.gelu

    def forward(
        self,
        src: torch.Tensor,
        src_mask: Optional[torch.Tensor] = None,
        src_key_padding_mask: Optional[torch.Tensor] = None,
    ) -> torch.Tensor:
        src2 = self.self_attn(
            src, src, src, attn_mask=src_mask, key_padding_mask=src_key_padding_mask
        )[0]
        src = src + self.dropout1(src2)
        src = self.norm1(src)
        src2 = self.linear2(self.dropout(self.activation(self.linear1(src))))
        src = src + self.dropout2(src2)
        src = self.norm2(src)
        return src


class EntropyComponent(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 512,
        mamba_d_state: int = 64,
        mamba_expand: int = 2,
        mamba_headdim: int = 32,
        transformer_nhead: int = 8,
        transformer_dim_feedforward: int = 1024,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.mamba_layer1 = Mamba3(
            d_model=hidden_dim,
            d_state=mamba_d_state,
            expand=mamba_expand,
            headdim=mamba_headdim,
            ngroups=1,
            is_mimo=False,
        )
        self.norm1 = RMSNorm(hidden_dim)

        self.mamba_layer2 = Mamba3(
            d_model=hidden_dim,
            d_state=mamba_d_state,
            expand=mamba_expand,
            headdim=mamba_headdim,
            ngroups=1,
            is_mimo=False,
        )
        self.norm2 = RMSNorm(hidden_dim)

        self.downsample = nn.Conv1d(
            in_channels=hidden_dim,
            out_channels=hidden_dim,
            kernel_size=3,
            stride=2,
            padding=1,
        )

        self.transformer = Transformer1DLayer(
            d_model=hidden_dim,
            nhead=transformer_nhead,
            dim_feedforward=transformer_dim_feedforward,
            dropout=dropout,
            activation="gelu",
            batch_first=True,
        )

        self.output_norm = nn.LayerNorm(hidden_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if x.dim() == 2:
            x = x.unsqueeze(1)

        x = self.input_proj(x)

        residual = x
        x = self.mamba_layer1(x)
        x = self.norm1(x + residual)

        residual = x
        x = self.mamba_layer2(x)
        x = self.norm2(x + residual)

        x = x.transpose(1, 2)
        x = self.downsample(x)
        x = x.transpose(1, 2)

        x = self.transformer(x)

        x = self.output_norm(x)

        return x
