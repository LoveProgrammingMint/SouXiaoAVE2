# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Dict, Optional, Tuple

from Model.Component.EntropyComponent import EntropyComponent
from Model.Classifier.EntropyClassifier import EntropyClassifier


class EntropyMap(nn.Module):
    def __init__(
        self,
        input_dim: int = 1024,
        hidden_dim: int = 512,
        output_dim: int = 2,
        mamba_d_state: int = 64,
        mamba_expand: int = 2,
        mamba_headdim: int = 32,
        transformer_nhead: int = 8,
        transformer_dim_feedforward: int = 1024,
        classifier_hidden_dim: int = 256,
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
            transformer_nhead=transformer_nhead,
            transformer_dim_feedforward=transformer_dim_feedforward,
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
    ) -> torch.Tensor | Tuple[torch.Tensor, torch.Tensor]:
        features = self.component(x)

        logits = self.classifier(features)

        if return_features:
            return logits, features
        return logits

    def predict(self, x: torch.Tensor) -> torch.Tensor:
        with torch.no_grad():
            logits = self.forward(x)
            predictions = torch.argmax(logits, dim=-1)
        return predictions

    def predict_proba(self, x: torch.Tensor) -> torch.Tensor:
        with torch.no_grad():
            logits = self.forward(x)
            probabilities = F.softmax(logits, dim=-1)
        return probabilities

    def get_features(self, x: torch.Tensor) -> torch.Tensor:
        with torch.no_grad():
            features = self.component(x)
        return features
