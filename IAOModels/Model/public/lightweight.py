# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F


class LightConvNeXtBlock(nn.Module):
    def __init__(
        self,
        dim: int,
        expansion: int = 2,
        dropout: float = 0.0,
    ) -> None:
        super().__init__()
        self.dwconv = nn.Conv1d(dim, dim, kernel_size=7, padding=3, groups=dim, bias=False)
        self.norm = nn.LayerNorm(dim)
        self.pwconv1 = nn.Linear(dim, dim * expansion)
        self.act = nn.GELU()
        self.pwconv2 = nn.Linear(dim * expansion, dim)
        self.dropout = nn.Dropout(dropout) if dropout > 0 else nn.Identity()

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        residual = x
        x = x.transpose(1, 2)
        x = self.dwconv(x)
        x = x.transpose(1, 2)
        x = self.norm(x)
        x = self.pwconv1(x)
        x = self.act(x)
        x = self.pwconv2(x)
        x = self.dropout(x)
        return x + residual


class ConvNeXtDownsampler(nn.Module):
    def __init__(
        self,
        input_dim: int,
        hidden_dim: int,
        num_blocks: int = 2,
        downsample_factors: list = None,
    ) -> None:
        super().__init__()
        if downsample_factors is None:
            downsample_factors = [4, 4]

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.blocks = nn.ModuleList()
        self.downsamplers = nn.ModuleList()

        current_dim = hidden_dim
        for i, factor in enumerate(downsample_factors):
            for _ in range(num_blocks):
                self.blocks.append(LightConvNeXtBlock(current_dim))
            self.downsamplers.append(
                nn.Conv1d(current_dim, current_dim, kernel_size=factor, stride=factor, bias=False)
            )

        self.output_dim = current_dim

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if x.dim() == 2:
            x = x.unsqueeze(1)

        x = self.input_proj(x)

        block_idx = 0
        for i, downsampler in enumerate(self.downsamplers):
            for _ in range(len(self.blocks) // len(self.downsamplers)):
                x = self.blocks[block_idx](x)
                block_idx += 1
            x = x.transpose(1, 2)
            x = downsampler(x)
            x = x.transpose(1, 2)

        return x


def count_parameters(model: nn.Module) -> float:
    return sum(p.numel() for p in model.parameters()) / 1e6


def count_flops(model: nn.Module, input_shape: tuple) -> tuple:
    try:
        from thop import profile, clever_format
        dummy_input = torch.randn(*input_shape)
        flops, params = profile(model, inputs=(dummy_input,), verbose=False)
        flops_f, params_f = clever_format([flops, params], "%.2f")
        return flops_f, params_f, flops / 1e9, params / 1e6
    except ImportError:
        return "N/A", "N/A", 0, 0
