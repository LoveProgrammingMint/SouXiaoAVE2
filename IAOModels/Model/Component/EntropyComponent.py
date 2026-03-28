# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F

from Model.public.transformer import Transformer1DLayer


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


class EntropyComponent(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 128,
        num_conv_blocks: int = 4,
        num_transformer_layers: int = 2,
        transformer_nhead: int = 4,
        transformer_dim_feedforward: int = 256,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.conv_blocks = nn.ModuleList([
            ConvBlock1D(hidden_dim, expansion=4) for _ in range(num_conv_blocks)
        ])

        self.transformer_layers = nn.ModuleList([
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

        self.output_norm = nn.LayerNorm(hidden_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if x.dim() == 2:
            x = x.unsqueeze(1)

        x = self.input_proj(x)

        x = x.transpose(1, 2)
        for block in self.conv_blocks:
            x = block(x)
        x = x.transpose(1, 2)

        for layer in self.transformer_layers:
            x = layer(x)

        x = self.output_norm(x)

        return x
