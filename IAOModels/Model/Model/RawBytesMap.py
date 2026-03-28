# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Optional, Tuple

from Model.Embedding.RawBytesEmbedding import RawBytesEmbedding
from Model.Component.RawBytesComponent import RawBytesComponent
from Model.Classifier.RawBytesClassifier import RawBytesClassifier


class RawBytesMap(nn.Module):
    def __init__(
        self,
        height: int = 128,
        width: int = 128,
        embed_dim: int = 16,
        hidden_dim: int = 64,
        output_dim: int = 2,
        num_layers: int = 5,
        kernel_sizes: Tuple[int, int, int] = (9, 7, 5),
        mlp_ratio: float = 4.0,
        drop_path_rate: float = 0.1,
        classifier_hidden_dim: int = 256,
        dropout: float = 0.3,
        layer_scale_init_value: float = 1e-6,
    ) -> None:
        super().__init__()
        self.height = height
        self.width = width
        self.embed_dim = embed_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.embedding = RawBytesEmbedding(
            embed_dim=embed_dim,
            height=height,
            width=width,
        )

        self.component = RawBytesComponent(
            in_channels=embed_dim,
            hidden_dim=hidden_dim,
            num_layers=num_layers,
            kernel_sizes=kernel_sizes,
            mlp_ratio=mlp_ratio,
            drop_path_rate=drop_path_rate,
            layer_scale_init_value=layer_scale_init_value,
        )

        self.classifier = RawBytesClassifier(
            input_dim=hidden_dim,
            hidden_dim=classifier_hidden_dim,
            output_dim=output_dim,
            dropout=dropout,
        )

    def forward(
        self,
        x: torch.Tensor,
        return_features: bool = False,
    ) -> torch.Tensor | Tuple[torch.Tensor, torch.Tensor]:
        x = self.embedding(x)

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
            x = self.embedding(x)
            features = self.component(x)
        return features

    def merge_branches(
        self,
        weights: Optional[Tuple[float, float, float]] = None,
    ) -> None:
        self.component.merge_branches(weights)

    def from_bytes(self, byte_data: bytes) -> torch.Tensor:
        x = self.embedding.from_bytes(byte_data, self.height, self.width)
        return self.forward(x)
