# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn


class CharWolfEmbedding(nn.Module):
    def __init__(
        self,
        input_size: int = 1024,
        embed_dim: int = 16,
        max_value: int = 255,
    ) -> None:
        super().__init__()
        self.input_size = input_size
        self.embed_dim = embed_dim
        self.max_value = max_value

        self.embedding = nn.Embedding(
            num_embeddings=max_value + 1,
            embedding_dim=embed_dim,
        )

        self._init_weights()

    def _init_weights(self) -> None:
        nn.init.normal_(self.embedding.weight, mean=0.0, std=0.02)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if x.dim() == 1:
            x = x.unsqueeze(0)

        x = x.long()
        x = x.clamp(0, self.max_value)

        x = self.embedding(x)

        return x

    def from_bytes(self, byte_data: bytes, target_size: int = 1024) -> torch.Tensor:
        import numpy as np

        data = np.frombuffer(byte_data, dtype=np.uint8)

        if len(data) < target_size:
            padded = np.zeros(target_size, dtype=np.uint8)
            padded[:len(data)] = data
            data = padded
        else:
            data = data[:target_size]

        tensor = torch.from_numpy(data.astype(np.int64)).unsqueeze(0)
        return self.forward(tensor)
