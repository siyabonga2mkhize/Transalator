# isiZulu Energy Intelligence — KZN Computer Labs

This branch evolves the original translator into the prototype for the **isiZulu Energy Intelligence** EPIP project on the **Languages Track**.

## What the prototype demonstrates

1. **Computer-level telemetry** — a lightweight Python agent can report CPU activity, machine ID, lab room and an estimated energy signal.
2. **Waste detection** — the API identifies idle and overnight-running machines and produces a specific alert such as `Computer 14 is idle and wasting electricity`.
3. **Historical analysis** — `/api/energy/history` provides a seven-day prototype series used to visualize estimated kWh and waste-alert trends.
4. **Multilingual dashboard** — the whole interface can be switched from a single dropdown across the 11 official South African languages. Four UI languages have built-in demo labels; the translation pipeline can request live translations for the selected language where the language service supports it.
5. **Operator translation** — the selected technical alert is translated into the dashboard language through the server-side language layer without exposing the API token to the browser.
6. **Light / dark mode** — the presentation demo includes a theme toggle and remembers the user's preference.
7. **Demo mode** — the `Run lab scan` button generates a realistic 20-computer lab snapshot so the complete workflow can be demonstrated without 20 physical PCs.

## Architecture

```text
Lab PC(s)
   │
   │ Python telemetry agent / heartbeat
   ▼
ASP.NET Core API
   ├── /api/energy/dashboard
   ├── /api/energy/history
   ├── /api/energy/scan
   ├── /api/energy/heartbeat
   │
   └── /api/translate ─────► Vulavula / Lelapa AI
                              │
                              ▼
                       local-language alert

Browser dashboard ◄──────── ASP.NET Core API
      │
      ├── 7-day energy chart
      ├── waste-alert trend chart
      ├── computer-level table
      ├── language selector
      └── light/dark theme
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

The default endpoint is `https://vulavula-services.lelapa.ai/api/v1/translate`. Published Vulavula examples use `X-CLIENT-TOKEN` authentication.

Live language coverage depends on the active Vulavula service/account. The UI still provides the full South African language selector so the research concept is visible without claiming unsupported live translations.

## Computer telemetry agent

See [`agent/README.md`](agent/README.md). The agent uses `psutil` to collect real CPU utilisation and sends a heartbeat to the API. The prototype's wattage is explicitly an estimate; production deployment should replace it with validated energy telemetry or power-meter data.

## Historical data note

The seven-day chart is intentionally labelled **prototype telemetry**. It demonstrates the analytical workflow requested by the judges; it is not presented as a measured field study. In production, `/api/energy/history` should be backed by stored telemetry from real lab machines/meters.

## Security

Never commit API keys to source control. If a key has previously been committed, rotate/revoke it with the provider even after removing it from the current file, because Git history can retain the old value.

## Project story

The intended user is a KZN computer-lab manager who should not have to interpret English-only telemetry or electrical terminology. The system turns a low-level event into an actionable local-language message, for example:

> Computer 14B is idle and wasting electricity. Switch it off now.

→ local-language operator message via the language service.
