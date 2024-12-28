import os
import subprocess

# Пути к конфигурационному файлу и вашему Unity-проекту
CONFIG_FILE = "car_config.yaml"  # Путь к YAML-файлу
ENV_PATH = None  # Укажите путь к сборке Unity (или None для работы в редакторе)

# Уникальный ID для запуска обучения
RUN_ID = "CarAgentRun1"


def train_agent():
    """Функция для запуска обучения ML-Agents."""
    # Команда для запуска ML-Agents
    command = [
        "mlagents-learn",
        CONFIG_FILE,
        "--run-id", RUN_ID,
    ]

    # Если есть сборка Unity, добавляем параметр `--env`
    if ENV_PATH:
        command.extend(["--env", ENV_PATH])

    # Добавляем флаг тренировки
    command.append("--train")

    # Запускаем процесс обучения
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8")
    try:
        for line in process.stdout:
            print(line.strip())
    except Exception as e:
        print(f"Ошибка во время обучения: {e}")
    finally:
        process.wait()


if __name__ == "__main__":
    # Запускаем обучение
    train_agent()
