import torch
import sys
sys.path.insert(0, '.')

from Model.public.lightweight import count_parameters, count_flops

print("=" * 70)
print("SouXiao AntiVirus Engine 2.0 - Model Complexity Analysis")
print("Target: FLOPs < 1GFLOP (0.5GMACS), Params < 4M")
print("=" * 70)

results = {}

print("\n[1/6] EntropyMap...")
from Model.Model.EntropyMap import EntropyMap
model = EntropyMap(input_dim=1024, hidden_dim=64, output_dim=2)
flops_str, params_str, flops_g, params_m = count_flops(model, (1, 1024))
results["EntropyMap"] = {"flops": flops_str, "params": params_str, "flops_g": flops_g, "params_m": params_m}
print(f"  FLOPs: {flops_str}, Params: {params_str}")

print("\n[2/6] RawBytesMap...")
from Model.Model.RawBytesMap import RawBytesMap
model = RawBytesMap(height=128, width=128, embed_dim=16, hidden_dim=64, output_dim=2)
flops_str, params_str, flops_g, params_m = count_flops(model, (1, 128, 128))
results["RawBytesMap"] = {"flops": flops_str, "params": params_str, "flops_g": flops_g, "params_m": params_m}
print(f"  FLOPs: {flops_str}, Params: {params_str}")

print("\n[3/6] AssemblyArrayMap...")
from Model.Model.AssemblyArrayMap import AssemblyArrayMap
model = AssemblyArrayMap(input_size=65536, embed_dim=16, hidden_dim=64, output_dim=2)
flops_str, params_str, flops_g, params_m = count_flops(model, (1, 65536))
results["AssemblyArrayMap"] = {"flops": flops_str, "params": params_str, "flops_g": flops_g, "params_m": params_m}
print(f"  FLOPs: {flops_str}, Params: {params_str}")

print("\n[4/6] StatisticsMap...")
from Model.Model.StatisticsMap import StatisticsMap
model = StatisticsMap(lgb_input_dim=128, lgb_output_dim=512, embed_dim=64, hidden_dim=64, output_dim=2)
flops_str, params_str, flops_g, params_m = count_flops(model, (1, 512))
results["StatisticsMap"] = {"flops": flops_str, "params": params_str, "flops_g": flops_g, "params_m": params_m}
print(f"  FLOPs: {flops_str}, Params: {params_str}")

print("\n[5/6] CharWolfMap...")
from Model.Model.CharWolfMap import CharWolfMap
model = CharWolfMap(input_size=1024, embed_dim=16, hidden_dim=64, output_dim=2)
flops_str, params_str, flops_g, params_m = count_flops(model, (1, 1024))
results["CharWolfMap"] = {"flops": flops_str, "params": params_str, "flops_g": flops_g, "params_m": params_m}
print(f"  FLOPs: {flops_str}, Params: {params_str}")

print("\n[6/6] IAOAVE2...")
from Model.Model.IAOAVE2 import IAOAVE2
model = IAOAVE2(input_dim=1024, hidden_dim=64, output_dim=2, num_experts=4, top_k=2)
flops_str, params_str, flops_g, params_m = count_flops(model, (1, 1024))
results["IAOAVE2"] = {"flops": flops_str, "params": params_str, "flops_g": flops_g, "params_m": params_m}
print(f"  FLOPs: {flops_str}, Params: {params_str}")

print("\n" + "=" * 70)
print("Summary:")
print("=" * 70)
print(f"{'Model':<20} {'FLOPs':<15} {'Params':<15} {'Status'}")
print("-" * 70)

all_pass = True
for name, info in results.items():
    flops_ok = info["flops_g"] < 1.0
    params_ok = info["params_m"] < 4.0
    status = "✓ PASS" if (flops_ok and params_ok) else "✗ FAIL"
    if not (flops_ok and params_ok):
        all_pass = False
    print(f"{name:<20} {info['flops']:<15} {info['params']:<15} {status}")

print("=" * 70)
if all_pass:
    print("All models meet the requirements!")
else:
    print("Some models need optimization!")
