# Lab Energy Agent

A lightweight prototype agent for each lab computer. It reads real CPU utilisation from the host and sends a heartbeat to the ASP.NET API.

## Important

The `watts` value is an estimate for the prototype. Software running on a normal PC cannot claim to measure mains electricity accurately without validated power telemetry or a smart meter. For production, replace `estimate_watts()` with measured device-level telemetry.

## Run

```powershell
cd agent
python -m pip install -r requirements.txt
$env:ENERGY_API_URL="http://localhost:5000/api/energy/heartbeat"
$env:ENERGY_COMPUTER_ID="Computer-14B"
$env:ENERGY_ROOM="Lab A"
python energy_agent.py
```

The dashboard can then display the machine's CPU activity and estimated energy signal through the same heartbeat API.
