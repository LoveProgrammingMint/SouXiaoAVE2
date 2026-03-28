# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import numpy as np
from typing import Optional, Union, List
import pickle
import os


class SXLightGBM(nn.Module):
    def __init__(
        self,
        input_dim: int = 128,
        num_trees: int = 512,
        num_leaves: int = 31,
        output_dim: int = 512,
        model_path: Optional[str] = None,
    ) -> None:
        super().__init__()
        self.input_dim = input_dim
        self.num_trees = num_trees
        self.num_leaves = num_leaves
        self.output_dim = output_dim
        self.model_path = model_path

        self.lgb_model = None
        self._is_fitted = False

        self.leaf_embedding = nn.Embedding(
            num_embeddings=num_trees * num_leaves,
            embedding_dim=1,
        )
        nn.init.normal_(self.leaf_embedding.weight, mean=0.0, std=0.02)

        self.proj = nn.Linear(num_trees, output_dim)

    def fit(
        self,
        X: np.ndarray,
        y: np.ndarray,
        params: Optional[dict] = None,
    ) -> "SXLightGBM":
        try:
            import lightgbm as lgb
        except ImportError:
            raise ImportError("LightGBM is required. Install with: pip install lightgbm")

        if params is None:
            params = {
                "objective": "binary",
                "metric": "binary_logloss",
                "boosting_type": "gbdt",
                "num_leaves": self.num_leaves,
                "num_trees": self.num_trees,
                "learning_rate": 0.05,
                "feature_fraction": 0.9,
                "bagging_fraction": 0.8,
                "bagging_freq": 5,
                "verbose": -1,
                "seed": 42,
            }

        train_data = lgb.Dataset(X, label=y)
        self.lgb_model = lgb.train(params, train_data, num_boost_round=self.num_trees)
        self._is_fitted = True

        return self

    def predict_leaf(self, X: Union[np.ndarray, torch.Tensor]) -> np.ndarray:
        if not self._is_fitted:
            raise RuntimeError("Model is not fitted. Call fit() first.")

        if isinstance(X, torch.Tensor):
            X = X.detach().cpu().numpy()

        leaf_indices = self.lgb_model.predict(X, pred_leaf=True)
        return leaf_indices

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if not self._is_fitted:
            leaf_output = torch.zeros(x.shape[0], self.num_trees, device=x.device)
            return self.proj(leaf_output)

        leaf_indices = self.predict_leaf(x)

        batch_size = leaf_indices.shape[0]
        leaf_indices_tensor = torch.from_numpy(leaf_indices).long().to(x.device)

        tree_offsets = torch.arange(self.num_trees, device=x.device) * self.num_leaves
        leaf_indices_flat = leaf_indices_tensor + tree_offsets.unsqueeze(0)

        leaf_values = self.leaf_embedding(leaf_indices_flat).squeeze(-1)

        output = self.proj(leaf_values)

        return output

    def save_model(self, path: str) -> None:
        if self.lgb_model is not None:
            self.lgb_model.save_model(path + ".lgb")

        state = {
            "input_dim": self.input_dim,
            "num_trees": self.num_trees,
            "num_leaves": self.num_leaves,
            "output_dim": self.output_dim,
            "leaf_embedding": self.leaf_embedding.state_dict(),
            "proj": self.proj.state_dict(),
        }
        torch.save(state, path + ".pt")

    def load_model(self, path: str) -> "SXLightGBM":
        try:
            import lightgbm as lgb
        except ImportError:
            raise ImportError("LightGBM is required. Install with: pip install lightgbm")

        lgb_path = path + ".lgb"
        if os.path.exists(lgb_path):
            self.lgb_model = lgb.Booster(model_file=lgb_path)
            self._is_fitted = True

        pt_path = path + ".pt"
        if os.path.exists(pt_path):
            state = torch.load(pt_path, map_location="cpu")
            self.leaf_embedding.load_state_dict(state["leaf_embedding"])
            self.proj.load_state_dict(state["proj"])

        return self

    def freeze_lgb(self) -> None:
        self.leaf_embedding.weight.requires_grad = False
        self.proj.weight.requires_grad = False
        self.proj.bias.requires_grad = False

    def unfreeze_lgb(self) -> None:
        self.leaf_embedding.weight.requires_grad = True
        self.proj.weight.requires_grad = True
        self.proj.bias.requires_grad = True
