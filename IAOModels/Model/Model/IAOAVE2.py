# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Tuple

from Model.public.mamba3 import Mamba3, RMSNorm
from Model.public.transformer import Transformer1DLayer
from Model.Component.MoERouter import MoERouter
from Model.Experts.ExpertA import ExpertA
from Model.Experts.ExpertB import ExpertB
from Model.Experts.ExpertC import ExpertC
from Model.Experts.ExpertD import ExpertD


class IAOAVE2(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 256,
        output_dim: int = 2,
        mamba_d_state: int = 32,
        mamba_expand: int = 2,
        mamba_headdim: int = 32,
        transformer_nhead: int = 4,
        transformer_dim_feedforward: int = 512,
        num_experts: int = 4,
        top_k: int = 2,
        expert_hidden_dim: int = 256,
        expert_output_dim: int = 256,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim
        self.num_experts = num_experts
        self.top_k = top_k

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.mamba = Mamba3(
            d_model=hidden_dim,
            d_state=mamba_d_state,
            expand=mamba_expand,
            headdim=mamba_headdim,
            ngroups=1,
            is_mimo=False,
        )
        self.mamba_norm = RMSNorm(hidden_dim)

        self.transformer = Transformer1DLayer(
            d_model=hidden_dim,
            nhead=transformer_nhead,
            dim_feedforward=transformer_dim_feedforward,
            dropout=dropout,
            activation="gelu",
            batch_first=True,
        )

        self.router = MoERouter(
            input_dim=hidden_dim,
            num_experts=num_experts,
            top_k=top_k,
        )

        self.experts = nn.ModuleList([
            ExpertA(input_dim=hidden_dim, hidden_dim=expert_hidden_dim, output_dim=expert_output_dim),
            ExpertB(input_dim=hidden_dim, hidden_dim=expert_hidden_dim, output_dim=expert_output_dim),
            ExpertC(input_dim=hidden_dim, hidden_dim=expert_hidden_dim, output_dim=expert_output_dim),
            ExpertD(input_dim=hidden_dim, hidden_dim=expert_hidden_dim, output_dim=expert_output_dim),
        ])

        self.fc1 = nn.Linear(expert_output_dim, 256)
        self.bn1 = nn.BatchNorm1d(256)
        self.dropout1 = nn.Dropout(dropout * 2)

        self.fc2 = nn.Linear(256, output_dim)

        self.output_norm = nn.LayerNorm(output_dim)

    def forward(
        self,
        x: torch.Tensor,
        return_features: bool = False,
    ) -> torch.Tensor | Tuple[torch.Tensor, torch.Tensor]:
        if x.dim() == 2:
            x = x.unsqueeze(1)

        x = self.input_proj(x)

        residual = x
        x = self.mamba(x)
        x = self.mamba_norm(x + residual)

        x = self.transformer(x)

        if x.dim() == 3:
            x_pooled = x.mean(dim=1)
        else:
            x_pooled = x

        top_k_indices, top_k_weights, _ = self.router(x_pooled)

        batch_size = x_pooled.shape[0]
        expert_outputs = torch.zeros(
            batch_size, self.experts[0].output_dim,
            device=x.device, dtype=x.dtype
        )

        for i in range(self.top_k):
            expert_idx_batch = top_k_indices[:, i]
            weights_batch = top_k_weights[:, i]

            for expert_idx in range(self.num_experts):
                mask = expert_idx_batch == expert_idx
                if mask.sum() > 0:
                    expert_input = x[mask]
                    expert_out = self.experts[expert_idx](expert_input)
                    if expert_out.dim() == 3:
                        expert_out = expert_out.mean(dim=1)
                    weighted_out = expert_out * weights_batch[mask].unsqueeze(-1)
                    expert_outputs[mask] += weighted_out

        x = self.fc1(expert_outputs)
        x = self.bn1(x)
        x = F.gelu(x)
        x = self.dropout1(x)

        features = x
        logits = self.fc2(features)

        logits = self.output_norm(logits)

        if return_features:
            return logits, features
        return logits, features

    def predict(self, x: torch.Tensor) -> torch.Tensor:
        with torch.no_grad():
            logits, _ = self.forward(x)
            predictions = torch.argmax(logits, dim=-1)
        return predictions

    def predict_proba(self, x: torch.Tensor) -> torch.Tensor:
        with torch.no_grad():
            logits, _ = self.forward(x)
            probabilities = F.softmax(logits, dim=-1)
        return probabilities

    def get_expert_weights(self, x: torch.Tensor) -> torch.Tensor:
        with torch.no_grad():
            if x.dim() == 2:
                x = x.unsqueeze(1)

            x = self.input_proj(x)
            residual = x
            x = self.mamba(x)
            x = self.mamba_norm(x + residual)
            x = self.transformer(x)

            if x.dim() == 3:
                x = x.mean(dim=1)

            _, _, full_weights = self.router(x)
        return full_weights
