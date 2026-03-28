# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F
from typing import Tuple, Optional
import numpy as np

from Model.public.lightgbm_wrapper import SXLightGBM
from Model.Embedding.StatisticsEmbedding import StatisticsEmbedding
from Model.Component.StatisticsComponent import StatisticsComponent
from Model.Classifier.StatisticsClassifier import StatisticsClassifier


class StatisticsMap(nn.Module):
    def __init__(
        self,
        lgb_input_dim: int = 128,
        lgb_num_trees: int = 512,
        lgb_num_leaves: int = 31,
        lgb_output_dim: int = 512,
        embed_dim: int = 64,
        hidden_dim: int = 128,
        output_dim: int = 2,
        num_transformer_layers: int = 2,
        transformer_nhead: int = 4,
        transformer_dim_feedforward: int = 256,
        classifier_hidden_dim: int = 128,
        dropout: float = 0.1,
        lgb_model_path: Optional[str] = None,
    ) -> None:
        super().__init__()
        self.lgb_input_dim = lgb_input_dim
        self.lgb_num_trees = lgb_num_trees
        self.lgb_num_leaves = lgb_num_leaves
        self.lgb_output_dim = lgb_output_dim
        self.embed_dim = embed_dim
        self.hidden_dim = hidden_dim
        self.output_dim = output_dim

        self.lgb = SXLightGBM(
            input_dim=lgb_input_dim,
            num_trees=lgb_num_trees,
            num_leaves=lgb_num_leaves,
            output_dim=lgb_output_dim,
        )

        if lgb_model_path is not None:
            self.lgb.load_model(lgb_model_path)

        self.embedding = StatisticsEmbedding(
            input_dim=lgb_output_dim,
            embed_dim=embed_dim,
            dropout=dropout,
        )

        self.component = StatisticsComponent(
            embed_dim=embed_dim,
            hidden_dim=hidden_dim,
            num_layers=num_transformer_layers,
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

    def fit_lgb(
        self,
        X: np.ndarray,
        y: np.ndarray,
        params: Optional[dict] = None,
    ) -> "StatisticsMap":
        self.lgb.fit(X, y, params)
        return self

    def forward(
        self,
        x: torch.Tensor,
        return_features: bool = False,
        use_lgb: bool = True,
    ) -> torch.Tensor | Tuple[torch.Tensor, torch.Tensor]:
        if use_lgb:
            x = self.lgb(x)

        x = self.embedding(x)

        features = self.component(x)

        logits = self.classifier(features)

        if return_features:
            return logits, features
        return logits

    def predict(self, x: torch.Tensor, use_lgb: bool = True) -> torch.Tensor:
        with torch.no_grad():
            logits = self.forward(x, use_lgb=use_lgb)
            predictions = torch.argmax(logits, dim=-1)
        return predictions

    def predict_proba(self, x: torch.Tensor, use_lgb: bool = True) -> torch.Tensor:
        with torch.no_grad():
            logits = self.forward(x, use_lgb=use_lgb)
            probabilities = F.softmax(logits, dim=-1)
        return probabilities

    def get_features(self, x: torch.Tensor, use_lgb: bool = True) -> torch.Tensor:
        with torch.no_grad():
            if use_lgb:
                x = self.lgb(x)
            x = self.embedding(x)
            features = self.component(x)
        return features

    def save_lgb(self, path: str) -> None:
        self.lgb.save_model(path)

    def load_lgb(self, path: str) -> "StatisticsMap":
        self.lgb.load_model(path)
        return self

    def freeze_lgb(self) -> None:
        self.lgb.freeze_lgb()

    def unfreeze_lgb(self) -> None:
        self.lgb.unfreeze_lgb()
