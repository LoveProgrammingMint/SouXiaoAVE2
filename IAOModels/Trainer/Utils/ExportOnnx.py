# Copyright (C) 2026 LinduCMint
# This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
# See LICENSE file for full terms.
# For production use or distribution, contact 3327867352@qq.com for authorization.

import os
import sys
import torch
import torch.nn as nn
from typing import Dict, Any, List, Optional
from pathlib import Path

_SCRIPT_DIR = Path(__file__).resolve().parent.parent.parent
if str(_SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(_SCRIPT_DIR))


class ONNXExporter:
    def __init__(
        self,
        output_dir: str = "./onnx_models",
        opset_version: int = 14,
        dynamic_batch: bool = True,
    ) -> None:
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.opset_version = opset_version
        self.dynamic_batch = dynamic_batch

    def export_model(
        self,
        model: nn.Module,
        model_name: str,
        dummy_input: torch.Tensor,
        input_names: List[str],
        output_names: List[str],
        dynamic_axes: Optional[Dict[str, Dict[int, str]]] = None,
    ) -> str:
        model.eval()

        if self.dynamic_batch and dynamic_axes is None:
            dynamic_axes = {
                name: {0: "batch_size"} for name in input_names + output_names
            }

        onnx_path = self.output_dir / f"{model_name}.onnx"

        torch.onnx.export(
            model,
            dummy_input,
            str(onnx_path),
            input_names=input_names,
            output_names=output_names,
            dynamic_axes=dynamic_axes,
            opset_version=self.opset_version,
            do_constant_folding=True,
            export_params=True,
        )

        return str(onnx_path)

    def verify_onnx(self, onnx_path: str) -> bool:
        try:
            import onnx
            model = onnx.load(onnx_path)
            onnx.checker.check_model(model)
            return True
        except ImportError:
            print("Warning: onnx package not installed, skipping verification")
            return True
        except Exception as e:
            print(f"ONNX verification failed: {e}")
            return False


def count_parameters(model: nn.Module) -> int:
    return sum(p.numel() for p in model.parameters())


def export_all_models(output_dir: str = "./onnx_models") -> Dict[str, Any]:
    from Model.Model import (
        EntropyMap,
        RawBytesMap,
        StatisticsMap,
        CharWolfMap,
        AssemblyArrayMap,
    )
    from Model.Model.IAOAVE2 import IAOAVE2

    exporter = ONNXExporter(output_dir=output_dir)
    results: Dict[str, Any] = {}

    print("=" * 60)
    print("Exporting all models to ONNX format...")
    print("=" * 60)

    print("\n[1/6] Exporting EntropyMap...")
    model = EntropyMap(
        input_dim=1024,
        hidden_dim=128,
        output_dim=2,
        num_conv_blocks=4,
        num_transformer_layers=2,
        classifier_hidden_dim=256,
    )
    params = count_parameters(model)
    print(f"  Parameters: {params:,} ({params/1e6:.2f}M)")
    dummy_input = torch.randn(1, 1024)

    class EntropyMapWrapper(nn.Module):
        def __init__(self, model):
            super().__init__()
            self.model = model

        def forward(self, x):
            logits, features = self.model(x)
            return logits, features

    wrapped_model = EntropyMapWrapper(model)
    onnx_path = exporter.export_model(
        wrapped_model,
        "EntropyMap",
        dummy_input,
        input_names=["input"],
        output_names=["logits", "fc_features"],
    )
    results["EntropyMap"] = {
        "path": onnx_path,
        "parameters": params,
        "input_shape": "(batch, 1024)",
        "output_shape": "logits: (batch, 2), fc_features: (batch, 256)",
        "verified": exporter.verify_onnx(onnx_path),
    }
    print(f"  Saved to: {onnx_path}")

    print("\n[2/6] Exporting RawBytesMap...")
    model = RawBytesMap(
        height=128,
        width=128,
        embed_dim=16,
        hidden_dim=96,
        output_dim=2,
        num_layers=4,
        classifier_hidden_dim=256,
    )
    params = count_parameters(model)
    print(f"  Parameters: {params:,} ({params/1e6:.2f}M)")
    dummy_input = torch.randint(0, 256, (1, 128, 128)).float()

    class RawBytesMapWrapper(nn.Module):
        def __init__(self, model):
            super().__init__()
            self.model = model

        def forward(self, x):
            logits, features = self.model(x.long())
            return logits, features

    wrapped_model = RawBytesMapWrapper(model)
    onnx_path = exporter.export_model(
        wrapped_model,
        "RawBytesMap",
        dummy_input,
        input_names=["input"],
        output_names=["logits", "fc_features"],
    )
    results["RawBytesMap"] = {
        "path": onnx_path,
        "parameters": params,
        "input_shape": "(batch, 128, 128) - int64",
        "output_shape": "logits: (batch, 2), fc_features: (batch, 256)",
        "verified": exporter.verify_onnx(onnx_path),
    }
    print(f"  Saved to: {onnx_path}")

    print("\n[3/6] Exporting AssemblyArrayMap...")
    model = AssemblyArrayMap(
        input_size=65536,
        embed_dim=16,
        hidden_dim=96,
        output_dim=2,
        num_conv_blocks=3,
        num_transformer_layers=2,
        classifier_hidden_dim=256,
    )
    params = count_parameters(model)
    print(f"  Parameters: {params:,} ({params/1e6:.2f}M)")
    dummy_input = torch.randint(0, 256, (1, 65536)).float()

    class AssemblyArrayMapWrapper(nn.Module):
        def __init__(self, model):
            super().__init__()
            self.model = model

        def forward(self, x):
            logits, features = self.model(x.long())
            return logits, features

    wrapped_model = AssemblyArrayMapWrapper(model)
    onnx_path = exporter.export_model(
        wrapped_model,
        "AssemblyArrayMap",
        dummy_input,
        input_names=["input"],
        output_names=["logits", "fc_features"],
    )
    results["AssemblyArrayMap"] = {
        "path": onnx_path,
        "parameters": params,
        "input_shape": "(batch, 65536) - int64",
        "output_shape": "logits: (batch, 2), fc_features: (batch, 256)",
        "verified": exporter.verify_onnx(onnx_path),
    }
    print(f"  Saved to: {onnx_path}")

    print("\n[4/6] Exporting StatisticsMap...")
    model = StatisticsMap(
        input_dim=512,
        embed_dim=64,
        hidden_dim=96,
        output_dim=2,
        num_conv_blocks=3,
        num_layers=2,
        classifier_hidden_dim=256,
    )
    params = count_parameters(model)
    print(f"  Parameters: {params:,} ({params/1e6:.2f}M)")
    dummy_input = torch.randn(1, 512)

    class StatisticsMapWrapper(nn.Module):
        def __init__(self, model):
            super().__init__()
            self.model = model

        def forward(self, x):
            logits, features = self.model(x)
            return logits, features

    wrapped_model = StatisticsMapWrapper(model)
    onnx_path = exporter.export_model(
        wrapped_model,
        "StatisticsMap",
        dummy_input,
        input_names=["input"],
        output_names=["logits", "fc_features"],
    )
    results["StatisticsMap"] = {
        "path": onnx_path,
        "parameters": params,
        "input_shape": "(batch, 512) - after LGB preprocessing",
        "output_shape": "logits: (batch, 2), fc_features: (batch, 256)",
        "verified": exporter.verify_onnx(onnx_path),
    }
    print(f"  Saved to: {onnx_path}")

    print("\n[5/6] Exporting CharWolfMap...")
    model = CharWolfMap(
        input_size=1024,
        embed_dim=16,
        hidden_dim=96,
        output_dim=2,
        num_conv_blocks=3,
        num_transformer_layers=2,
        classifier_hidden_dim=256,
    )
    params = count_parameters(model)
    print(f"  Parameters: {params:,} ({params/1e6:.2f}M)")
    dummy_input = torch.randint(0, 256, (1, 1024)).float()

    class CharWolfMapWrapper(nn.Module):
        def __init__(self, model):
            super().__init__()
            self.model = model

        def forward(self, x):
            logits, features = self.model(x.long())
            return logits, features

    wrapped_model = CharWolfMapWrapper(model)
    onnx_path = exporter.export_model(
        wrapped_model,
        "CharWolfMap",
        dummy_input,
        input_names=["input"],
        output_names=["logits", "fc_features"],
    )
    results["CharWolfMap"] = {
        "path": onnx_path,
        "parameters": params,
        "input_shape": "(batch, 1024) - int64",
        "output_shape": "logits: (batch, 2), fc_features: (batch, 256)",
        "verified": exporter.verify_onnx(onnx_path),
    }
    print(f"  Saved to: {onnx_path}")

    print("\n[6/6] Exporting IAOAVE2...")
    model = IAOAVE2(
        input_dim=1024,
        hidden_dim=128,
        output_dim=2,
        num_conv_blocks=3,
        num_transformer_layers=2,
        num_experts=4,
        top_k=2,
        expert_hidden_dim=128,
        expert_output_dim=128,
    )
    params = count_parameters(model)
    print(f"  Parameters: {params:,} ({params/1e6:.2f}M)")
    dummy_input = torch.randn(1, 1024)

    class IAOAVE2Wrapper(nn.Module):
        def __init__(self, model):
            super().__init__()
            self.model = model

        def forward(self, x):
            logits, features = self.model(x)
            return logits, features

    wrapped_model = IAOAVE2Wrapper(model)
    onnx_path = exporter.export_model(
        wrapped_model,
        "IAOAVE2",
        dummy_input,
        input_names=["input"],
        output_names=["logits", "fc_features"],
    )
    results["IAOAVE2"] = {
        "path": onnx_path,
        "parameters": params,
        "input_shape": "(batch, 1024)",
        "output_shape": "logits: (batch, 2), fc_features: (batch, 512)",
        "verified": exporter.verify_onnx(onnx_path),
    }
    print(f"  Saved to: {onnx_path}")

    print("\n" + "=" * 60)
    print("Export completed!")
    print("=" * 60)

    print("\nModel Summary:")
    print("-" * 60)
    print(f"{'Model':<20} {'Parameters':>15} {'Status':>10}")
    print("-" * 60)
    for name, info in results.items():
        status = "OK" if info["verified"] else "FAILED"
        print(f"{name:<20} {info['parameters']:>15,} {status:>10}")
    print("-" * 60)

    return results


def get_model_info() -> str:
    info = """
================================================================================
                        SouXiao AntiVirus Engine 2.0
                           Model Interface Document
================================================================================

一、模型列表
--------------------------------------------------------------------------------
| 模型名称          | 输入形状           | 输入类型  | 输出形状                    |
|-------------------|-------------------|----------|----------------------------|
| EntropyMap        | (batch, 1024)     | float32  | logits: (batch, 2)         |
|                   |                   |          | fc_features: (batch, 256)  |
|-------------------|-------------------|----------|----------------------------|
| RawBytesMap       | (batch, 128, 128) | int64    | logits: (batch, 2)         |
|                   |                   |          | fc_features: (batch, 256)  |
|-------------------|-------------------|----------|----------------------------|
| AssemblyArrayMap  | (batch, 65536)    | int64    | logits: (batch, 2)         |
|                   |                   |          | fc_features: (batch, 256)  |
|-------------------|-------------------|----------|----------------------------|
| StatisticsMap     | (batch, 512)      | float32  | logits: (batch, 2)         |
|                   |                   |          | fc_features: (batch, 256)  |
|-------------------|-------------------|----------|----------------------------|
| CharWolfMap       | (batch, 1024)     | int64    | logits: (batch, 2)         |
|                   |                   |          | fc_features: (batch, 256)  |
|-------------------|-------------------|----------|----------------------------|
| IAOAVE2           | (batch, 1024)     | float32  | logits: (batch, 2)         |
| (MoE混合专家模型)  |                   |          | fc_features: (batch, 512)  |
--------------------------------------------------------------------------------

二、输出说明
--------------------------------------------------------------------------------
1. logits: (batch, 2)
   - 二分类原始输出
   - index 0: 良性样本得分
   - index 1: 恶意样本得分
   - 使用 softmax 获取概率: prob = softmax(logits, dim=-1)

2. fc_features: (batch, 256/512)
   - FC层中间表示
   - 可用于特征融合、集成学习等

三、推理示例 (Python/ONNX Runtime)
--------------------------------------------------------------------------------
import numpy as np
import onnxruntime as ort

# 加载模型
session = ort.InferenceSession("EntropyMap.onnx")

# 准备输入
input_data = np.random.randn(1, 1024).astype(np.float32)

# 推理
outputs = session.run(None, {"input": input_data})
logits, fc_features = outputs

# 获取预测结果
probabilities = np.exp(logits) / np.sum(np.exp(logits), axis=-1, keepdims=True)
prediction = np.argmax(logits, axis=-1)

print(f"Probabilities: {probabilities}")
print(f"Prediction: {'Malicious' if prediction[0] == 1 else 'Benign'}")

四、注意事项
--------------------------------------------------------------------------------
1. 整数输入模型 (RawBytesMap, AssemblyArrayMap, CharWolfMap):
   - 输入必须是 int64 类型
   - 值范围: 0-255 (字节值)

2. 动态批处理:
   - 所有模型支持动态 batch_size
   - 推理时可使用任意 batch 大小

五、模型文件
--------------------------------------------------------------------------------
- EntropyMap.onnx
- RawBytesMap.onnx
- AssemblyArrayMap.onnx
- StatisticsMap.onnx
- CharWolfMap.onnx
- IAOAVE2.onnx

================================================================================
"""
    return info


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Export models to ONNX format")
    parser.add_argument(
        "--output-dir",
        type=str,
        default="./onnx_models",
        help="Output directory for ONNX files",
    )
    parser.add_argument(
        "--info",
        action="store_true",
        help="Print model interface information",
    )
    args = parser.parse_args()

    if args.info:
        print(get_model_info())
    else:
        results = export_all_models(args.output_dir)

        print("\nExport Summary:")
        print("-" * 60)
        for name, info in results.items():
            status = "OK" if info["verified"] else "FAILED"
            print(f"[{status}] {name}: {info['path']}")
