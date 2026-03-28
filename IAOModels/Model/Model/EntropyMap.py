# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Tuple

from Model.public.lightweight import LightConvNeXtBlock
from Model.public.mamba3 import Mamba3, RMSNorm
from Model.public.transformer import Transformer1DLayer


class EntropyComponent(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 64,
        num_conv_blocks: int = 2,
        mamba_d_state: int = 16,
        mamba_expand: int = 2,
        mamba_headdim: int = 16,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim

        self.input_proj = nn.Linear(input_dim, hidden_dim)

        self.conv_blocks = nn.ModuleList([
            LightConvNeXtBlock(hidden_dim) for _ in range(num_conv_blocks)
        ])

        self.downsample = nn.Conv1d(hidden_dim, hidden_dim, kernel_size=4, stride=4, bias=False)

        self.mamba = Mamba3(
            d_model=hidden_dim,
            d_state=mamba_d_state,
            expand=mamba_expand,
            headdim=mamba_headdim,
            ngroups=1,
            is_mimo=False,
        )
        self.norm = RMSNorm(hidden_dim)

        self.transformer = Transformer1DLayer(
            d_model=hidden_dim,
            nhead=2,
            dim_feedforward=hidden_dim * 2,
            dropout=dropout,
            activation="gelu",
            batch_first=True,
        )

        self.output_norm = nn.LayerNorm(hidden_dim)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if x.dim() == 2:
            x = x.unsqueeze(1)

        x = self.input_proj(x)

        for block in self.conv_blocks:
            x = block(x)

        x = x.transpose(1, 2)
        x = self.downsample(x)
        x = x.transpose(1, 2)

        residual = x
        x = self.mamba(x)
        x = self.norm(x + residual)

        x = self.transformer(x)

        x = self.output_norm(x)

        return x


class EntropyClassifier(nn.Module):
    def __init__(
        self,
        input_dim: int = 64,
        hidden_dim: int = 128,
        output_dim: int = 2,
        dropout: float = 0.2,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.fc1 = nn.Linear(input_dim, hidden_dim)
        self.bn1 = nn.BatchNorm1d(hidden_dim)
        self.dropout1 = nn.Dropout(dropout)

        self.fc2 = nn.Linear(hidden_dim, 256)
        self.bn2 = nn.BatchNorm1d(256)
        self.dropout2 = nn.Dropout(dropout)

        self.fc3 = nn.Linear(256, output_dim)

    def forward(self, x: torch.Tensor) -> Tuple[torch.Tensor, torch.Tensor]:
        if x.dim() == 3:
            x = x.mean(dim=1)

        x = self.fc1(x)
        x = self.bn1(x)
        x = F.gelu(x)
        x = self.dropout1(x)

        x = self.fc2(x)
        x = self.bn2(x)
        x = F.gelu(x)
        x = self.dropout2(x)

        features = x
        logits = self.fc3(features)

        return logits, features


class EntropyMap(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 64,
        output_dim: int = 2,
        mamba_d_state: int = 16,
        mamba_expand: int = 2,
        mamba_headdim: int = 16,
        classifier_hidden_dim: int = 128,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.component = EntropyComponent(
            input_dim=input_dim,
            hidden_dim=hidden_dim,
            mamba_d_state=mamba_d_state,
            mamba_expand=mamba_expand,
            mamba_headdim=mamba_headdim,
            dropout=dropout,
        )

        self.classifier = EntropyClassifier(
            input_dim=hidden_dim,
            hidden_dim=classifier_hidden_dim,
            output_dim=output_dim,
            dropout=dropout * 2,
        )

    def forward(
        self,
        x: torch.Tensor,
        return_features: bool = False,
    ) -> torch.Tensor | Tuple[torch.Tensor, torch.Tensor, torch.Tensor]:
        features = self.component(x)

        logits, fc_features = self.classifier(features)

        if return_features:
            return logits, fc_features, features
        return logits, fc_features

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

    def get_features(self, x: torch.Tensor) -> torch.Tensor:
        with torch.no_grad():
            features = self.component(x)
        return features
