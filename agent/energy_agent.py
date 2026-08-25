import os
import time
import socket
import psutil
import requests

API_URL = os.getenv("ENERGY_API_URL", "http://localhost:5000/api/energy/heartbeat")
COMPUTER_ID = os.getenv("ENERGY_COMPUTER_ID", socket.gethostname())
ROOM = os.getenv("ENERGY_ROOM", "Demo Lab")
INTERVAL = int(os.getenv("ENERGY_INTERVAL_SECONDS", "30"))


def estimate_watts(cpu_percent: float) -> float:
    """Prototype estimate only; replace with validated meter/telemetry in production."""
    return round(25 + (cpu_percent / 100.0) * 90, 1)


def heartbeat():
    cpu = psutil.cpu_percent(interval=1)
    watts = estimate_watts(cpu)
    payload = {
        "computerId": COMPUTER_ID,
        "room": ROOM,
        "cpuPercent": cpu,
        "watts": watts,
        "hoursObserved": 0.0,
        "idle": cpu < 10,
    }
    response = requests.post(API_URL, json=payload, timeout=10)
    response.raise_for_status()
    print(response.json())


if __name__ == "__main__":
    print(f"Energy agent: {COMPUTER_ID} | {ROOM} | {API_URL}")
    while True:
        try:
            heartbeat()
        except Exception as exc:
            print(f"heartbeat failed: {exc}")
        time.sleep(INTERVAL)
