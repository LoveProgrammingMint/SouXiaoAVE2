# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Optional, Tuple


class LayerNorm2d(nn.Module):
    def __init__(self, num_channels: int, eps: float = 1e-6) -> None:
        super().__init__()
        self.weight = nn.Parameter(torch.ones(num_channels))
        self.bias = nn.Parameter(torch.zeros(num_channels))
        self.eps = eps

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        u = x.mean(1, keepdim=True)
        s = (x - u).pow(2).mean(1, keepdim=True)
        x = (x - u) / torch.sqrt(s + self.eps)
        x = self.weight[:, None, None] * x + self.bias[:, None, None]
        return x


class AsymmetricConvBlock(nn.Module):
    def __init__(
        self,
        in_channels: int,
        kernel_sizes: Tuple[int, int, int] = (9, 7, 5),
        bias: bool = False,
    ) -> None:
        super().__init__()
        self.kernel_sizes = kernel_sizes

        self.conv9 = nn.Conv2d(
            in_channels, in_channels, kernel_size=(kernel_sizes[0], 1),
            padding=(kernel_sizes[0] // 2, 0), bias=bias
        )
        self.conv7 = nn.Conv2d(
            in_channels, in_channels, kernel_size=(kernel_sizes[1], 1),
            padding=(kernel_sizes[1] // 2, 0), bias=bias
        )
        self.conv5 = nn.Conv2d(
            in_channels, in_channels, kernel_size=(kernel_sizes[2], 1),
            padding=(kernel_sizes[2] // 2, 0), bias=bias
        )

        self.merge_weights: Optional[nn.Parameter] = None
        self._merged = False

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if self._merged and self.merge_weights is not None:
            return self._forward_merged(x)

        out9 = self.conv9(x)
        out7 = self.conv7(x)
        out5 = self.conv5(x)

        return out9 + out7 + out5

    def _forward_merged(self, x: torch.Tensor) -> torch.Tensor:
        merged_conv = self._create_merged_conv()
        return merged_conv(x)

    def _create_merged_conv(self) -> nn.Conv2d:
        max_k = max(self.kernel_sizes)
        in_ch = self.conv9.in_channels

        merged_weight = torch.zeros(in_ch, in_ch, max_k, 1, device=self.conv9.weight.device)

        for i, (conv, k) in enumerate(zip([self.conv9, self.conv7, self.conv5], self.kernel_sizes)):
            start = (max_k - k) // 2
            if self.merge_weights is not None:
                weight = self.merge_weights[i]
            else:
                weight = 1.0 / 3.0
            merged_weight[:, :, start:start + k, :] += weight * conv.weight.data

        merged_conv = nn.Conv2d(
            in_ch, in_ch, kernel_size=(max_k, 1),
            padding=(max_k // 2, 0), bias=False
        )
        merged_conv.weight.data = merged_weight
        return merged_conv

    def merge_branches(self, weights: Optional[Tuple[float, float, float]] = None) -> None:
        if weights is None:
            weights = (1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0)
        self.merge_weights = nn.Parameter(torch.tensor(weights))
        self._merged = True


class ConvNeXtBlock(nn.Module):
    def __init__(
        self,
        dim: int,
        kernel_sizes: Tuple[int, int, int] = (9, 7, 5),
        mlp_ratio: float = 4.0,
        drop_path: float = 0.0,
        layer_scale_init_value: float = 1e-6,
    ) -> None:
        super().__init__()
        self.dwconv = AsymmetricConvBlock(dim, kernel_sizes, bias=False)
        self.norm = nn.LayerNorm(dim, eps=1e-6)
        self.pwconv1 = nn.Linear(dim, int(mlp_ratio * dim))
        self.act = nn.GELU()
        self.pwconv2 = nn.Linear(int(mlp_ratio * dim), dim)

        self.gamma = nn.Parameter(
            layer_scale_init_value * torch.ones(dim)
        ) if layer_scale_init_value > 0 else None

        self.drop_path = DropPath(drop_path) if drop_path > 0.0 else nn.Identity()

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        shortcut = x
        x = self.dwconv(x)
        x = x.permute(0, 2, 3, 1)
        x = self.norm(x)
        x = self.pwconv1(x)
        x = self.act(x)
        x = self.pwconv2(x)
        if self.gamma is not None:
            x = self.gamma * x
        x = x.permute(0, 3, 1, 2)

        x = shortcut + self.drop_path(x)
        return x

    def merge_branches(self, weights: Optional[Tuple[float, float, float]] = None) -> None:
        self.dwconv.merge_branches(weights)


class DropPath(nn.Module):
    def __init__(self, drop_prob: float = 0.0) -> None:
        super().__init__()
        self.drop_prob = drop_prob

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if self.drop_prob == 0.0 or not self.training:
            return x
        keep_prob = 1 - self.drop_prob
        shape = (x.shape[0],) + (1,) * (x.ndim - 1)
        random_tensor = keep_prob + torch.rand(shape, dtype=x.dtype, device=x.device)
        random_tensor.floor_()
        return x.div(keep_prob) * random_tensor


class ConvNeXtStage(nn.Module):
    def __init__(
        self,
        dim: int,
        depth: int,
        kernel_sizes: Tuple[int, int, int] = (9, 7, 5),
        mlp_ratio: float = 4.0,
        drop_path_rates: Optional[list] = None,
        layer_scale_init_value: float = 1e-6,
    ) -> None:
        super().__init__()
        if drop_path_rates is None:
            drop_path_rates = [0.0] * depth

        self.blocks = nn.ModuleList([
            ConvNeXtBlock(
                dim=dim,
                kernel_sizes=kernel_sizes,
                mlp_ratio=mlp_ratio,
                drop_path=drop_path_rates[i] if i < len(drop_path_rates) else 0.0,
                layer_scale_init_value=layer_scale_init_value,
            )
            for i in range(depth)
        ])

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        for block in self.blocks:
            x = block(x)
        return x

    def merge_branches(self, weights: Optional[Tuple[float, float, float]] = None) -> None:
        for block in self.blocks:
            block.merge_branches(weights)
