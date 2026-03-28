# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Tuple


class MoERouter(nn.Module):
    def __init__(
        self,
        input_dim: int,
        num_experts: int = 4,
        top_k: int = 2,
        noise_std: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.num_experts = num_experts
        self.top_k = top_k
        self.noise_std = noise_std

        self.gate = nn.Linear(input_dim, num_experts, bias=False)

    def forward(
        self,
        x: torch.Tensor,
    ) -> Tuple[torch.Tensor, torch.Tensor, torch.Tensor]:
        if x.dim() == 3:
            x = x.mean(dim=1)

        logits = self.gate(x)

        if self.training and self.noise_std > 0:
            noise = torch.randn_like(logits) * self.noise_std
            logits = logits + noise

        top_k_logits, top_k_indices = torch.topk(logits, self.top_k, dim=-1)

        top_k_weights = F.softmax(top_k_logits, dim=-1)

        full_weights = torch.zeros_like(logits)
        full_weights.scatter_(1, top_k_indices, top_k_weights)

        return top_k_indices, top_k_weights, full_weights


class MoELayer(nn.Module):
    def __init__(
        self,
        input_dim: int,
        hidden_dim: int,
        output_dim: int,
        num_experts: int = 4,
        top_k: int = 2,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim
        self.num_experts = num_experts
        self.top_k = top_k

        self.router = MoERouter(input_dim, num_experts, top_k)

        self.experts = nn.ModuleList([
            nn.Sequential(
                nn.Linear(input_dim, hidden_dim),
                nn.GELU(),
                nn.Dropout(dropout),
                nn.Linear(hidden_dim, output_dim),
            )
            for _ in range(num_experts)
        ])

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        original_shape = x.shape
        if x.dim() == 3:
            x = x.mean(dim=1)

        top_k_indices, top_k_weights, _ = self.router(x)

        batch_size = x.shape[0]
        output = torch.zeros(batch_size, self.output_dim, device=x.device, dtype=x.dtype)

        for i in range(self.top_k):
            expert_indices = top_k_indices[:, i]
            expert_weights = top_k_weights[:, i]

            for expert_idx in range(self.num_experts):
                mask = expert_indices == expert_idx
                if mask.sum() > 0:
                    expert_input = x[mask]
                    expert_output = self.experts[expert_idx](expert_input)
                    weighted_output = expert_output * expert_weights[mask].unsqueeze(-1)
                    output[mask] += weighted_output

        return output
