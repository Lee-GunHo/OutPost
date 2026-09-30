"""Unity 배포용 작은 MLP(PyTorch). 학습 시엔 logits를 내고(CrossEntropyLoss와 궁합),
ONNX로 내보낼 때만 Softmax를 붙여서 Unity에서 바로 확률을 받을 수 있게 함."""

import logging

import numpy as np
import torch
from torch import nn
from torch.utils.data import DataLoader, TensorDataset

logger = logging.getLogger(__name__)


class DifficultyMLP(nn.Module):
    def __init__(self, input_dim: int, num_classes: int, hidden_dims=(32, 16)):
        super().__init__()

        layers = []
        prev_dim = input_dim

        for hidden_dim in hidden_dims:
            layers.append(nn.Linear(prev_dim, hidden_dim))
            layers.append(nn.ReLU())
            prev_dim = hidden_dim

        layers.append(nn.Linear(prev_dim, num_classes))

        self.network = nn.Sequential(*layers)

    def forward(self, x):
        return self.network(x)


class DifficultyMLPWithSoftmax(nn.Module):
    """ONNX export 전용 래퍼. 학습된 DifficultyMLP + Softmax."""

    def __init__(self, base_model: DifficultyMLP):
        super().__init__()
        self.base_model = base_model
        self.softmax = nn.Softmax(dim=1)

    def forward(self, x):
        return self.softmax(self.base_model(x))


def train_mlp(
    X_train: np.ndarray,
    y_train_idx: np.ndarray,
    X_test: np.ndarray,
    y_test_idx: np.ndarray,
    num_classes: int,
    hidden_dims=(32, 16),
    epochs: int = 200,
    batch_size: int = 32,
    lr: float = 1e-3,
    seed: int = 42,
):
    torch.manual_seed(seed)

    input_dim = X_train.shape[1]
    model = DifficultyMLP(input_dim, num_classes, hidden_dims)

    train_dataset = TensorDataset(
        torch.tensor(X_train, dtype=torch.float32),
        torch.tensor(y_train_idx, dtype=torch.long),
    )
    train_loader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True)

    X_test_t = torch.tensor(X_test, dtype=torch.float32)
    y_test_t = torch.tensor(y_test_idx, dtype=torch.long)

    optimizer = torch.optim.Adam(model.parameters(), lr=lr)
    criterion = nn.CrossEntropyLoss()

    history = []

    for epoch in range(epochs):
        model.train()
        epoch_loss = 0.0

        for batch_x, batch_y in train_loader:
            optimizer.zero_grad()
            logits = model(batch_x)
            loss = criterion(logits, batch_y)
            loss.backward()
            optimizer.step()
            epoch_loss += loss.item() * batch_x.size(0)

        epoch_loss /= len(train_dataset)

        model.eval()
        with torch.no_grad():
            test_logits = model(X_test_t)
            test_loss = criterion(test_logits, y_test_t).item()
            test_acc = (test_logits.argmax(dim=1) == y_test_t).float().mean().item()

        history.append({"epoch": epoch, "train_loss": epoch_loss,
                         "test_loss": test_loss, "test_acc": test_acc})

        if (epoch + 1) % max(1, epochs // 10) == 0 or epoch == epochs - 1:
            logger.info(
                "[MLP] epoch %d/%d train_loss=%.4f test_loss=%.4f test_acc=%.4f",
                epoch + 1, epochs, epoch_loss, test_loss, test_acc)

    model.eval()
    return model, history
