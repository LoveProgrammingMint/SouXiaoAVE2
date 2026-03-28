# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Optional, Tuple, List

from Model.public.convnext import ConvNeXtBlock, LayerNorm2d


class RawBytesComponent(nn.Module):
    def __init__(
        self,
        in_channels: int = 16,
        hidden_dim: int = 64,
        num_layers: int = 5,
        kernel_sizes: Tuple[int, int, int] = (9, 7, 5),
        mlp_ratio: float = 4.0,
        drop_path_rate: float = 0.1,
        layer_scale_init_value: float = 1e-6,
    ) -> None:
        super().__init__()
        self.in_channels = in_channels
        self.hidden_dim = hidden_dim
        self.num_layers = num_layers

        self.stem = nn.Sequential(
            nn.Conv2d(in_channels, hidden_dim, kernel_size=4, stride=4, bias=False),
            LayerNorm2d(hidden_dim, eps=1e-6),
        )

        total_depth = num_layers
        dpr = [x.item() for x in torch.linspace(0, drop_path_rate, total_depth)]

        self.blocks = nn.ModuleList([
            ConvNeXtBlock(
                dim=hidden_dim,
                kernel_sizes=kernel_sizes,
                mlp_ratio=mlp_ratio,
                drop_path=dpr[i],
                layer_scale_init_value=layer_scale_init_value,
            )
            for i in range(num_layers)
        ])

        self.final_norm = LayerNorm2d(hidden_dim, eps=1e-6)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = self.stem(x)

        for block in self.blocks:
            x = block(x)

        x = self.final_norm(x)

        return x

    def merge_branches(self, weights: Optional[Tuple[float, float, float]] = None) -> None:
        for block in self.blocks:
            block.merge_branches(weights)

    def get_feature_dim(self) -> int:
        return self.hidden_dim
