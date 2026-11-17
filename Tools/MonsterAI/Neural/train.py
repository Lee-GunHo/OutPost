"""Offline policy distillation. No Unity gameplay, player saves, or research logs are modified."""
import argparse
import csv
import hashlib
import json
import platform
from pathlib import Path

import numpy as np
import torch
from torch import nn
import onnx
import onnxruntime as ort

ROOT = Path(__file__).resolve().parents[3]
REPORT = ROOT / "Reports/MonsterAI/neural-policy"
ASSET = ROOT / "Assets/Resources/MonsterCombat/monster_attack_mlp.onnx"


class AttackMLP(nn.Module):
    def __init__(self):
        super().__init__()
        self.layers = nn.Sequential(nn.Linear(4, 32), nn.ReLU(), nn.Linear(32, 32), nn.ReLU(), nn.Linear(32, 4))

    def forward(self, x):
        return torch.exp(self.layers(x))  # Positive preferences, NOT softmax or a chosen attack.


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def compare(target, prediction, raw):
    base = np.array([3, 2, 2, 2], dtype=np.float32)
    expected = target * base
    actual = prediction * base
    expected /= expected.sum(axis=1, keepdims=True)
    actual /= actual.sum(axis=1, keepdims=True)
    delta = np.abs(actual - expected)
    distance, melee = raw[:, 0], raw[:, 1]
    boundary = np.minimum.reduce([np.abs(distance - 2), np.abs(distance - 2.3), np.abs(distance - melee - .5)]) < .05
    # Enumerate every non-empty legal-action mask and the production squad multipliers.
    worst_masked_mae = 0.0
    for role in ([1, 1, 1, 1], [1, 0, 1, 6], [.15, 12, .15, 0], [1, 1, 1, 1.5]):
        for mask in range(1, 16):
            factors = base * role * np.array([(mask >> i) & 1 for i in range(4)])
            if not factors.any():
                continue
            a, b = target * factors, prediction * factors
            a /= a.sum(axis=1, keepdims=True)
            b /= b.sum(axis=1, keepdims=True)
            worst_masked_mae = max(worst_masked_mae, float(np.abs(a - b).mean()))
    return {
        "samples": len(target), "preference_mae": float(np.abs(prediction - target).mean()),
        "probability_mae_percentage_points": float(delta.mean() * 100),
        "probability_max_error_percentage_points": float(delta.max() * 100),
        "away_from_boundaries_mae_pp": float(delta[~boundary].mean() * 100),
        "near_boundaries_mae_pp": float(delta[boundary].mean() * 100),
        "near_boundary_samples": int(boundary.sum()),
        "worst_role_mask_mean_error_pp": worst_masked_mae * 100,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--epochs", type=int, default=1200)
    args = parser.parse_args()
    torch.manual_seed(20261118)
    np.random.seed(20261118)
    torch.set_num_threads(2)
    torch.use_deterministic_algorithms(True)
    data_path = ROOT / "Temp/MonsterNeuralTrainingData/dataset.csv"
    with data_path.open(encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    datasets = {}
    for split in ("train", "validation", "test"):
        selected = [row for row in rows if row["split"] == split]
        x = np.array([[float(row[f"x{i}"]) for i in range(4)] for row in selected], np.float32)
        y = np.array([[float(row[k]) for k in ("melee", "projectile", "circle", "charge")] for row in selected], np.float32)
        raw = np.array([[float(row[k]) for k in ("distance", "meleeRange", "lowHealth", "recentlyHit")] for row in selected], np.float32)
        datasets[split] = (torch.from_numpy(x), torch.from_numpy(y), raw)
    # Assert split leakage is absent; compare feature tuples, including the two flags.
    keys = [set(map(tuple, datasets[s][0].numpy())) for s in ("train", "validation", "test")]
    assert not (keys[0] & keys[1] or keys[0] & keys[2] or keys[1] & keys[2]), "Duplicate inputs across splits"
    model = AttackMLP()
    optimizer = torch.optim.Adam(model.parameters(), lr=.003)
    scheduler = torch.optim.lr_scheduler.MultiStepLR(optimizer, milestones=[args.epochs // 2, args.epochs * 4 // 5], gamma=.3)
    train_x, train_y, _ = datasets["train"]
    val_x, val_y, _ = datasets["validation"]
    train_log = train_y.log()
    history, best_loss, best_epoch, best_state = [], float("inf"), 0, None
    for epoch in range(1, args.epochs + 1):
        model.train()
        order = torch.randperm(len(train_x))
        epoch_loss = 0.0
        for ids in order.split(512):
            optimizer.zero_grad()
            loss = nn.functional.mse_loss(model.layers(train_x[ids]), train_log[ids])
            loss.backward()
            optimizer.step()
            epoch_loss += loss.item() * len(ids)
        scheduler.step()
        model.eval()
        with torch.no_grad():
            val_loss = nn.functional.mse_loss(model.layers(val_x), val_y.log()).item()
        history.append([epoch, epoch_loss / len(train_x), val_loss])
        if val_loss < best_loss:
            best_loss, best_epoch = val_loss, epoch
            best_state = {key: value.detach().clone() for key, value in model.state_dict().items()}
        if epoch % 100 == 0:
            print(f"epoch={epoch} train={history[-1][1]:.6f} validation={val_loss:.6f}", flush=True)
    model.load_state_dict(best_state)
    model.eval()
    REPORT.mkdir(parents=True, exist_ok=True)
    with (REPORT / "training_history.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(["epoch", "train_log_preference_mse", "validation_log_preference_mse"])
        writer.writerows(history)
    # The test set is evaluated only after selecting the checkpoint on validation loss.
    test_x, test_y, raw = datasets["test"]
    with torch.no_grad():
        predictions = model(test_x).numpy()
    metrics = compare(test_y.numpy(), predictions, raw)
    assert np.isfinite(predictions).all() and (predictions > 0).all() and (predictions < 20).all()
    if metrics["probability_mae_percentage_points"] > 2.0 or metrics["worst_role_mask_mean_error_pp"] > 3.0:
        raise RuntimeError(f"Model failed fidelity gate: {metrics}")
    # Stage ONNX outside Assets until both the model checker and ONNX Runtime parity pass.
    staged = REPORT / "monster_attack_mlp.onnx"
    torch.onnx.export(model, torch.zeros(1, 4), str(staged), input_names=["observation"], output_names=["preferences"],
                      opset_version=15, dynamo=False)
    onnx.checker.check_model(onnx.load(str(staged)))
    session = ort.InferenceSession(str(staged), providers=["CPUExecutionProvider"])
    ort_values = np.concatenate([session.run(None, {"observation": row[None]})[0] for row in test_x.numpy()])
    metrics["pytorch_onnx_max_abs_difference"] = float(np.max(np.abs(predictions - ort_values)))
    assert np.allclose(predictions, ort_values, atol=2e-5, rtol=2e-5), "ONNX output mismatch"
    ASSET.parent.mkdir(parents=True, exist_ok=True)
    ASSET.write_bytes(staged.read_bytes())
    staged.unlink()
    torch.save({"state_dict": model.state_dict(), "architecture": [4, 32, 32, 4], "seed": 20261118}, REPORT / "checkpoint.pt")
    np.savez_compressed(REPORT / "dataset.npz", **{f"{s}_{name}": value.numpy() if isinstance(value, torch.Tensor) else value
                         for s, values in datasets.items() for name, value in zip(("x", "y", "raw"), values)})
    metadata = {"modelId": "attack-preference-mlp-v1", "method": "supervised policy distillation of original utility rules",
                "architecture": [4, 32, 32, 4], "hiddenActivation": "ReLU", "outputActivation": "Exp",
                "input": ["distance / 8", "(distance - meleeRange) / 8", "lowHealth", "recentlyHit"],
                "output": ["Melee", "Projectile", "Circle", "Charge"], "seed": 20261118,
                "bestEpoch": best_epoch, "epochs": args.epochs, "bestValidationLoss": best_loss,
                "splitSizes": {key: len(value[0]) for key, value in datasets.items()},
                "teacherSourceSha256": digest(ROOT / "Assets/Script/monster/MonsterNeuralPolicyModel.cs"),
                "datasetCsvSha256": digest(data_path), "onnxSha256": digest(ASSET),
                "python": platform.python_version(), "torch": torch.__version__, "onnx": onnx.__version__,
                "onnxruntime": ort.__version__, "testMetrics": metrics}
    (REPORT / "metrics.json").write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    with (REPORT / "test_predictions.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(["distance", "meleeRange", "lowHealth", "recentlyHit"] +
                        [f"teacher_{k}" for k in metadata["output"]] + [f"mlp_{k}" for k in metadata["output"]])
        writer.writerows(np.concatenate([raw, test_y.numpy(), predictions], axis=1).tolist())
    # Unity Editor checks its own inference backend and encoder against these held-out Python outputs.
    probes = {"cases": [{"distance": float(r[0]), "meleeRange": float(r[1]), "lowHealth": bool(r[2]),
                         "recentlyHit": bool(r[3]), "expected": p.tolist()} for r, p in zip(raw[:256], predictions[:256])]}
    (REPORT / "unity_probes.json").write_text(json.dumps(probes), encoding="utf-8")
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    h = np.array(history)
    fig, ax = plt.subplots(figsize=(8, 4.5))
    ax.plot(h[:, 0], h[:, 1], label="Training")
    ax.plot(h[:, 0], h[:, 2], label="Validation")
    ax.set(xlabel="Epoch", ylabel="MSE of log preferences", title="MLP policy distillation", yscale="log")
    ax.legend()
    fig.tight_layout()
    fig.savefig(REPORT / "training_curve.png", dpi=160)
    plt.close(fig)
    print(json.dumps(metadata, indent=2), flush=True)


if __name__ == "__main__":
    main()
