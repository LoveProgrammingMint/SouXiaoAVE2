# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Tuple

from Model.public.transformer import Transformer1DLayer
from Model.Component.MoERouter import MoERouter
from Model.Experts.ExpertA import ExpertA
from Model.Experts.ExpertB import ExpertB
from Model.Experts.ExpertC import ExpertC
from Model.Experts.ExpertD import ExpertD


class ConvBlock1D(nn.Module):
    def __init__(self, hidden_dim: int, expansion: int = 4):
        super().__init__()
        self.dwconv = nn.Conv1d(hidden_dim, hidden_dim, kernel_size=7, padding=3, groups=hidden_dim, bias=False)
        self.norm = nn.LayerNorm(hidden_dim)
        self.pwconv1 = nn.Linear(hidden_dim, hidden_dim * expansion)
        self.act = nn.GELU()
        self.pwconv2 = nn.Linear(hidden_dim * expansion, hidden_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        residual = x
        x = self.dwconv(x)
        x = x.transpose(1, 2)
        x = self.norm(x)
        x = self.pwconv1(x)
        x = self.act(x)
        x = self.pwconv2(x)
        x = x.transpose(1, 2)
        return x + residual


class IAOAVE2(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 128,
        output_dim: int = 2,
        num_conv_blocks: int = 3,
        num_transformer_layers: int = 2,
        transformer_nhead: int = 4,
        transformer_dim_feedforward: int = 256,
        num_experts: int = 4,
        top_k: int = 2,
        expert_hidden_dim: int = 128,
        expert_output_dim: int = 128,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim
        self.num_experts = num_experts
        self.top_k = top_k

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.conv_blocks = nn.ModuleList([
            ConvBlock1D(hidden_dim, expansion=4) for _ in range(num_conv_blocks)
        ])

        self.transformer_layers = nn.ModuleList([
            Transformer1DLayer(
                d_model=hidden_dim,
                nhead=transformer_nhead,
                dim_feedforward=transformer_dim_feedforward,
                dropout=dropout,
                activation="gelu",
                batch_first=True,
            )
            for _ in range(num_transformer_layers)
        ])

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

        x = x.transpose(1, 2)
        for block in self.conv_blocks:
            x = block(x)
        x = x.transpose(1, 2)

        for layer in self.transformer_layers:
            x = layer(x)

        if x.dim() == 3:
            x_pooled = x.mean(dim=1)
        else:
            x_pooled = x

        _, _, full_weights = self.router(x_pooled)

        batch_size = x_pooled.shape[0]
        expert_outputs = torch.zeros(
            batch_size, self.experts[0].output_dim,
            device=x.device, dtype=x.dtype
        )

        for expert_idx in range(self.num_experts):
            expert_out = self.experts[expert_idx](x)
            if expert_out.dim() == 3:
                expert_out = expert_out.mean(dim=1)
            weight = full_weights[:, expert_idx:expert_idx+1]
            expert_outputs = expert_outputs + expert_out * weight

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

            x = x.transpose(1, 2)
            for block in self.conv_blocks:
                x = block(x)
            x = x.transpose(1, 2)

            for layer in self.transformer_layers:
                x = layer(x)

            if x.dim() == 3:
                x = x.mean(dim=1)

            _, _, full_weights = self.router(x)
        return full_weights
