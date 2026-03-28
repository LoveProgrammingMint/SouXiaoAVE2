# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F

from Model.public.mamba3 import Mamba3, RMSNorm
from Model.public.transformer import Transformer1DLayer


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
