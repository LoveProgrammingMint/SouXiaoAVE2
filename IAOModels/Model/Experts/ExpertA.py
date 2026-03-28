# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Tuple


class AsymmetricConv1DBlock(nn.Module):
    def __init__(
        self,
        hidden_dim: int,
        kernel_sizes: Tuple[int, int, int] = (9, 7, 5),
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.conv9 = nn.Conv1d(hidden_dim, hidden_dim, kernel_size=kernel_sizes[0], padding=kernel_sizes[0]//2, bias=False)
        self.conv7 = nn.Conv1d(hidden_dim, hidden_dim, kernel_size=kernel_sizes[1], padding=kernel_sizes[1]//2, bias=False)
        self.conv5 = nn.Conv1d(hidden_dim, hidden_dim, kernel_size=kernel_sizes[2], padding=kernel_sizes[2]//2, bias=False)

        self.norm = nn.LayerNorm(hidden_dim)
        self.dropout = nn.Dropout(dropout)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        residual = x

        x = x.transpose(1, 2)
        out9 = self.conv9(x)
        out7 = self.conv7(x)
        out5 = self.conv5(x)
        x = out9 + out7 + out5
        x = x.transpose(1, 2)

        x = self.norm(x)
        x = self.dropout(x)

        return x + residual


class ExpertA(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 256,
        output_dim: int = 256,
        num_blocks: int = 2,
        kernel_sizes: Tuple[int, int, int] = (9, 7, 5),
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.conv_blocks = nn.ModuleList([
            AsymmetricConv1DBlock(hidden_dim, kernel_sizes, dropout)
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

        for block in self.conv_blocks:
            x = block(x)

        x = self.mlp(x)

        x = self.output_norm(x)

        return x
