# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import math
import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Optional


class LinearAttention(nn.Module):
    def __init__(
        self,
        dim: int,
        num_heads: int = 8,
        dim_head: int = 64,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.dim = dim
        self.num_heads = num_heads
        self.dim_head = dim_head
        self.inner_dim = num_heads * dim_head

        self.to_qkv = nn.Linear(dim, self.inner_dim * 3, bias=False)
        self.to_out = nn.Linear(self.inner_dim, dim)
        self.dropout = nn.Dropout(dropout)

    def forward(
        self,
        x: torch.Tensor,
        attn_mask: Optional[torch.Tensor] = None,
    ) -> torch.Tensor:
        batch_size, seq_len, _ = x.shape

        qkv = self.to_qkv(x).chunk(3, dim=-1)
        q, k, v = map(
            lambda t: t.view(batch_size, seq_len, self.num_heads, self.dim_head).transpose(1, 2),
            qkv,
        )

        q = F.elu(q) + 1
        k = F.elu(k) + 1

        kv = torch.einsum("bhnd,bhne->bhde", k, v)

        k_cumsum = k.cumsum(dim=-2)
        z = k_cumsum[:, :, -1, :].unsqueeze(-2)

        numerator = torch.einsum("bhnd,bhde->bhne", q, kv)
        denominator = (q * z).sum(dim=-1, keepdim=True) + 1e-6

        out = numerator / denominator
        out = out.transpose(1, 2).contiguous().view(batch_size, seq_len, self.inner_dim)

        out = self.to_out(out)
        out = self.dropout(out)

        return out


class FastLinearAttention(nn.Module):
    def __init__(
        self,
        dim: int,
        num_heads: int = 8,
        dim_head: int = 64,
        dropout: float = 0.1,
        feature_dim: int = 64,
    ) -> None:
        super().__init__()
        self.dim = dim
        self.num_heads = num_heads
        self.dim_head = dim_head
        self.inner_dim = num_heads * dim_head
        self.feature_dim = feature_dim

        self.to_q = nn.Linear(dim, self.inner_dim, bias=False)
        self.to_k = nn.Linear(dim, self.inner_dim, bias=False)
        self.to_v = nn.Linear(dim, self.inner_dim, bias=False)

        self.proj_matrix = nn.Parameter(
            torch.randn(self.dim_head, feature_dim) / math.sqrt(feature_dim)
        )

        self.to_out = nn.Linear(self.inner_dim, dim)
        self.dropout = nn.Dropout(dropout)

    def _feature_map(self, x: torch.Tensor) -> torch.Tensor:
        x_proj = torch.einsum("bhnd,df->bhnf", x, self.proj_matrix)
        return F.relu(x_proj) + 1e-6

    def forward(
        self,
        x: torch.Tensor,
        attn_mask: Optional[torch.Tensor] = None,
    ) -> torch.Tensor:
        batch_size, seq_len, _ = x.shape

        q = self.to_q(x).view(batch_size, seq_len, self.num_heads, self.dim_head).transpose(1, 2)
        k = self.to_k(x).view(batch_size, seq_len, self.num_heads, self.dim_head).transpose(1, 2)
        v = self.to_v(x).view(batch_size, seq_len, self.num_heads, self.dim_head).transpose(1, 2)

        q = self._feature_map(q)
        k = self._feature_map(k)

        kv = torch.einsum("bhnd,bhne->bhde", k, v)

        k_sum = k.sum(dim=-2)

        numerator = torch.einsum("bhnd,bhde->bhne", q, kv)
        denominator = torch.einsum("bhnd,bhd->bhn", q, k_sum) + 1e-6

        out = numerator / denominator.unsqueeze(-1)

        out = out.transpose(1, 2).contiguous().view(batch_size, seq_len, self.inner_dim)
        out = self.to_out(out)
        out = self.dropout(out)

        return out


class LinearAttentionLayer(nn.Module):
    def __init__(
        self,
        dim: int,
        num_heads: int = 8,
        dim_head: int = 64,
        dim_feedforward: int = 256,
        dropout: float = 0.1,
        use_fast: bool = True,
        feature_dim: int = 64,
    ) -> None:
        super().__init__()
        self.norm1 = nn.LayerNorm(dim)

        if use_fast:
            self.attn = FastLinearAttention(
                dim=dim,
                num_heads=num_heads,
                dim_head=dim_head,
                dropout=dropout,
                feature_dim=feature_dim,
            )
        else:
            self.attn = LinearAttention(
                dim=dim,
                num_heads=num_heads,
                dim_head=dim_head,
                dropout=dropout,
            )

        self.norm2 = nn.LayerNorm(dim)
        self.ffn = nn.Sequential(
            nn.Linear(dim, dim_feedforward),
            nn.GELU(),
            nn.Dropout(dropout),
            nn.Linear(dim_feedforward, dim),
            nn.Dropout(dropout),
        )

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = x + self.attn(self.norm1(x))
        x = x + self.ffn(self.norm2(x))
        return x
