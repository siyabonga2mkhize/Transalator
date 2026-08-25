# isiZulu Energy Intelligence — KZN Computer Labs

This project has evolved from a basic translator into the prototype for the **isiZulu Energy Intelligence** EPIP project.

## What the prototype demonstrates

1. **Computer-level telemetry** — a lightweight Python agent can report CPU activity, machine ID, lab room and an estimated energy signal.
2. **Waste detection** — the API identifies idle and overnight-running machines and produces a specific alert such as `Computer 14 is idle and wasting electricity`.
3. **isiZulu translation** — the server sends the technical alert to Vulavula without exposing the API token to the browser.
4. **Operator dashboard** — the UI shows the lab status, the exact computer responsible, the English diagnostic and the isiZulu operator message.
5. **Demo mode** — the `Run lab scan` button generates a realistic 20-computer lab snapshot so the complete workflow can be demonstrated without 20 physical PCs.

## Architecture

```text
Lab PC(s)
   │
   │ Python telemetry agent / heartbeat
   ▼
ASP.NET Core API
   ├── /api/energy/dashboard
   ├── /api/energy/scan
   ├── /api/energy/heartbeat
   │
   └── /api/translate ─────► Vulavula / Lelapa AI
                              │
                              ▼
                       isiZulu operator alert

Browser dashboard ◄──────── ASP.NET Core API
```

## Run locally

```powershell
dotnet restore
dotnet run
```

Open the URL printed by ASP.NET Core and click **Run lab scan**.

### Live Vulavula translation

The API credential must be supplied through an environment variable rather than committed to GitHub:

```powershell
$env:Vulavula__ApiKey = "YOUR_VULAVULA_TOKEN"
dotnet run
```

The default endpoint is `https://vulavula-services.lelapa.ai/api/v1/translate` and the project uses `eng_Latn` → `zul_Latn` for the EPIP demo.

The published Vulavula examples use `X-CLIENT-TOKEN` authentication and language codes such as `eng_Latn` and `zul_Latn`.

## Computer telemetry agent

See [`agent/README.md`](agent/README.md). The agent uses `psutil` to collect real CPU utilisation and sends a heartbeat to the API. The prototype's wattage is explicitly an estimate; production deployment should replace it with validated energy telemetry.

## Security

Never commit API keys to source control. If a key has previously been committed, rotate/revoke it with the provider even after removing it from the current file, because Git history can retain the old value.

## Project story

The intended user is a KZN computer-lab manager who should not have to interpret English-only telemetry or electrical terminology. The system turns a low-level event into an actionable local-language message, for example:

> Computer 14B is idle and wasting electricity. Switch it off now.

→ isiZulu operator message via the language service.
