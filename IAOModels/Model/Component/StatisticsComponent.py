# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F

from Model.public.transformer import Transformer1DLayer


class StatisticsComponent(nn.Module):
    def __init__(
        self,
        embed_dim: int = 64,
        hidden_dim: int = 128,
        num_layers: int = 2,
        transformer_nhead: int = 4,
        transformer_dim_feedforward: int = 256,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.embed_dim = embed_dim
        self.hidden_dim = hidden_dim
        self.num_layers = num_layers

        self.input_proj = nn.Linear(embed_dim, hidden_dim)

        self.transformer_layers = nn.ModuleList([
            Transformer1DLayer(
                d_model=hidden_dim,
                nhead=transformer_nhead,
                dim_feedforward=transformer_dim_feedforward,
                dropout=dropout,
                activation="gelu",
                batch_first=True,
            )
            for _ in range(num_layers)
        ])

        self.output_norm = nn.LayerNorm(hidden_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = self.input_proj(x)

        for layer in self.transformer_layers:
            x = layer(x)

        x = self.output_norm(x)

        return x
