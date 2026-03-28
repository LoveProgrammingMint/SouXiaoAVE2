# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Tuple

from Model.Embedding.StatisticsEmbedding import StatisticsEmbedding
from Model.Component.StatisticsComponent import StatisticsComponent
from Model.Classifier.StatisticsClassifier import StatisticsClassifier


class StatisticsMap(nn.Module):
    def __init__(
        self,
        input_dim: int = 512,
        embed_dim: int = 64,
        hidden_dim: int = 96,
        output_dim: int = 2,
        num_conv_blocks: int = 3,
        num_layers: int = 2,
        transformer_nhead: int = 4,
        transformer_dim_feedforward: int = 192,
        classifier_hidden_dim: int = 256,
        dropout: float = 0.1,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.embed_dim = embed_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.embedding = StatisticsEmbedding(
            input_dim=input_dim,
            embed_dim=embed_dim,
            dropout=dropout,
        )

        self.component = StatisticsComponent(
            embed_dim=embed_dim,
            hidden_dim=hidden_dim,
            num_conv_blocks=num_conv_blocks,
            num_layers=num_layers,
            transformer_nhead=transformer_nhead,
            transformer_dim_feedforward=transformer_dim_feedforward,
            dropout=dropout,
        )

        self.classifier = StatisticsClassifier(
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
        x = self.embedding(x)
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
            x = self.embedding(x)
            features = self.component(x)
        return features
