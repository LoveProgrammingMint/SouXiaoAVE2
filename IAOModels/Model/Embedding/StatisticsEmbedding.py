# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F


class StatisticsEmbedding(nn.Module):
    def __init__(
        self,
        input_dim: int = 512,
        embed_dim: int = 64,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.embed_dim = embed_dim

        self.proj = nn.Sequential(
            nn.Linear(input_dim, embed_dim * 2),
            nn.LayerNorm(embed_dim * 2),
            nn.GELU(),
            nn.Dropout(dropout),
            nn.Linear(embed_dim * 2, embed_dim),
            nn.LayerNorm(embed_dim),
        )

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if x.dim() == 3:
            batch_size, seq_len, _ = x.shape
            x = x.view(batch_size * seq_len, -1)
            x = self.proj(x)
            x = x.view(batch_size, seq_len, self.embed_dim)
        else:
            x = self.proj(x)
            x = x.unsqueeze(1)

        return x
