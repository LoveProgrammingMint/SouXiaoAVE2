# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
from typing import Tuple

from Model.public.mamba3 import Mamba3, RMSNorm


class ExpertC(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 256,
        output_dim: int = 256,
        num_layers: int = 2,
        mamba_d_state: int = 32,
        mamba_expand: int = 2,
        mamba_headdim: int = 32,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.mamba_layers = nn.ModuleList()
        self.norms = nn.ModuleList()

        for _ in range(num_layers):
            self.mamba_layers.append(
                Mamba3(
                    d_model=hidden_dim,
                    d_state=mamba_d_state,
                    expand=mamba_expand,
                    headdim=mamba_headdim,
                    ngroups=1,
                    is_mimo=False,
                )
            )
            self.norms.append(RMSNorm(hidden_dim))

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

        for mamba, norm in zip(self.mamba_layers, self.norms):
            residual = x
            x = mamba(x)
            x = norm(x + residual)

        x = self.mlp(x)

        x = self.output_norm(x)

        return x
