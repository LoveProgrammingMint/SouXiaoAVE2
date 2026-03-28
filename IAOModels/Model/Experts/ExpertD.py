# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Tuple


class GatedConvBlock(nn.Module):
    def __init__(
        self,
        hidden_dim: int,
        kernel_size: int = 3,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.conv = nn.Conv1d(
            hidden_dim, hidden_dim * 2, kernel_size,
            padding=kernel_size // 2, bias=False
        )
        self.dropout = nn.Dropout(dropout)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = x.transpose(1, 2)
        x = self.conv(x)
        x = x.transpose(1, 2)

        gate, value = x.chunk(2, dim=-1)
        gate = torch.sigmoid(gate)
        x = gate * value

        x = self.dropout(x)
        return x


class ExpertD(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 256,
        output_dim: int = 256,
        num_blocks: int = 2,
        kernel_size: int = 3,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.gated_conv_blocks = nn.ModuleList([
            GatedConvBlock(hidden_dim, kernel_size, dropout)
            for _ in range(num_blocks)
        ])

        self.layer_norms = nn.ModuleList([
            nn.LayerNorm(hidden_dim)
            for _ in range(num_blocks)
        ])

        self.mlp = nn.Sequential(
            nn.LayerNorm(hidden_dim),
            nn.Linear(hidden_dim, hidden_dim * 2),
            nn.GELU(),
            nn.Dropout(dropout),
            nn.Linear(hidden_dim * 2, output_dim),
        )

        self.output_norm = nn.LayerNorm(output_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if x.dim() == 2:
            x = x.unsqueeze(1)

        x = self.input_proj(x)

        for gated_conv, norm in zip(self.gated_conv_blocks, self.layer_norms):
            residual = x
            x = gated_conv(x)
            x = norm(x + residual)

        x = self.mlp(x)

        x = self.output_norm(x)

        return x
