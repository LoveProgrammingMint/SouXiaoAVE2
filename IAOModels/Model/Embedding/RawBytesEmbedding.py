# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import torch
import torch.nn as nn
import torch.nn.functional as F


class RawBytesEmbedding(nn.Module):
    def __init__(
        self,
        embed_dim: int = 16,
        height: int = 128,
        width: int = 128,
        max_byte_value: int = 255,
    ) -> None:
        super().__init__()
        self.embed_dim = embed_dim
        self.height = height
        self.width = width
        self.max_byte_value = max_byte_value

        self.byte_embedding = nn.Embedding(
            num_embeddings=max_byte_value + 1,
            embedding_dim=embed_dim,
        )

        self._init_weights()

    def _init_weights(self) -> None:
        nn.init.normal_(self.byte_embedding.weight, mean=0.0, std=0.02)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        if x.dim() == 3:
            x = x.unsqueeze(1)

        batch_size = x.shape[0]

        if x.dim() == 4 and x.shape[1] == 1:
            x = x.squeeze(1)

        x = x.long()

        x = x.clamp(0, self.max_byte_value)

        x = self.byte_embedding(x)

        x = x.permute(0, 3, 1, 2)

        return x

    def from_bytes(self, byte_data: bytes, height: int = 128, width: int = 128) -> torch.Tensor:
        import numpy as np

        data = np.frombuffer(byte_data, dtype=np.uint8)

        target_size = height * width
        if len(data) < target_size:
            padded = np.zeros(target_size, dtype=np.uint8)
            padded[:len(data)] = data
            data = padded
        else:
            data = data[:target_size]

        data = data.reshape(1, height, width)
        tensor = torch.from_numpy(data.astype(np.int64))

        return self.forward(tensor)
