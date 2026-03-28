# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
from typing import Tuple

from Model.public.transformer import Transformer1DLayer


class ExpertB(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 256,
        output_dim: int = 256,
        num_layers: int = 2,
        num_heads: int = 4,
        dim_feedforward: int = 512,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.transformer_layers = nn.ModuleList([
            Transformer1DLayer(
                d_model=hidden_dim,
                nhead=num_heads,
                dim_feedforward=dim_feedforward,
                dropout=dropout,
                activation="gelu",
                batch_first=True,
            )
            for _ in range(num_layers)
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

        for layer in self.transformer_layers:
            x = layer(x)

        x = self.mlp(x)

        x = self.output_norm(x)

        return x
