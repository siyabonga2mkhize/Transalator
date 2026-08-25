# EPIP 2026 — isiZulu Energy Intelligence

## Slide 1 — Title
**isiZulu Energy Intelligence for KZN Computer Labs**
- Ubuntu Builders
- Public Services & Inclusion
- Data Scientia Sub-Challenge
- Student: Siyabonga Mkhize

## Slide 2 — Introduction
- Computer labs generate technical telemetry in English.
- Lab managers may understand computers but not electrical engineering terminology.
- Language and technical complexity can delay action on energy waste.
- The project combines energy telemetry with isiZulu operator guidance.

## Slide 3 — Problem Statement
**How can a KZN computer-lab manager identify energy waste at individual-computer level and understand the required action in isiZulu?**
- Building-level dashboards do not identify the exact computer.
- English-only technical messages create a language/technical gap.
- Idle and overnight machines can remain unnoticed.

## Slide 4 — Aim & Objectives
**Aim:** Design and prototype an isiZulu energy-intelligence system for computer labs.
1. Design a computer-level telemetry and waste-detection workflow.
2. Implement an API and dashboard that converts detected waste into actionable isiZulu alerts.
3. Evaluate the prototype through simulated lab telemetry and live agent heartbeats.

## Slide 5 — Related Work
| Approach | Strength | Limitation addressed by this project |
|---|---|---|
| Building energy dashboards | Energy visibility | Limited computer-level attribution |
| Generic translation | Language access | Does not understand lab-energy context by itself |
| African language AI | Local-language capability | Needs domain-specific telemetry and action workflow |
| This prototype | Telemetry + detection + isiZulu | Targets the KZN computer-lab operator |

## Slide 6 — Gap
**Existing tools can show data, and language tools can translate, but the operator still needs a domain workflow.**

Gap: computer-level energy signal → waste detection → specific machine → understandable isiZulu action.

## Slide 7 — Methodology: Architecture
```text
Lab PC
  ↓
Telemetry Agent
  ↓
ASP.NET Core API
  ├── Waste Detection
  ├── Energy Dashboard
  └── Vulavula Translation
          ↓
     isiZulu Alert
          ↓
     Lab Manager
```

## Slide 8 — Methodology: Detection
- CPU activity and agent heartbeat are collected.
- Prototype identifies idle and overnight patterns.
- Each alert is linked to a computer ID and lab room.
- Estimated kWh and carbon are calculated for the prototype.
- Production deployment should use validated energy telemetry/meters.

## Slide 9 — Methodology: Language Layer
- Browser never receives the language-service token.
- ASP.NET Core acts as the secure translation proxy.
- English diagnostic → Vulavula → isiZulu operator message.
- Language codes: `eng_Latn` and `zul_Latn`.

## Slide 10 — Demonstration: Detect
**Run lab scan**
- Show 20 computers.
- Identify machines marked **Waste detected**.
- Select one machine.
- Explain the exact reason for the alert.

## Slide 11 — Demonstration: Translate
Example:

**English:**
`Computer 14 is idle and wasting electricity. Estimated waste: 0.32 kWh today.`

**isiZulu:**
Translation returned by the configured language service.

Then explain that the same API can receive a heartbeat from a real lab PC.

## Slide 12 — Demonstration: Agent
- Run `agent/energy_agent.py` on a computer.
- Agent reads real CPU utilisation.
- Agent sends a heartbeat to `/api/energy/heartbeat`.
- Dashboard can consume the signal.
- Wattage is currently an estimate and must be replaced by validated metering for production.

## Slide 13 — Tools & Resources
- ASP.NET Core 8 / C#
- HTML + JavaScript + Tailwind prototype UI
- Python + `psutil` telemetry agent
- Vulavula / Lelapa AI language service
- GitHub
- Future: validated power meters, WhatsApp Business integration, South African hosting

## Slide 14 — Contribution
**The innovation is the workflow, not translation alone.**
- Computer-level attribution
- Domain-specific energy alerts
- isiZulu-first operator communication
- Lightweight agent architecture
- Secure server-side language API integration
- Designed for African language and data-sovereignty requirements

## Slide 15 — Conclusion & Next Steps
- Prototype demonstrates the complete detection → translation → action journey.
- Next: validate telemetry against real power meters.
- Collect KZN-specific isiZulu energy terminology.
- Test with lab managers and measure response time/comprehension.
- Add WhatsApp alert delivery.
- Evaluate cost, accuracy and sustainability impact.

## Slide 16 — References
- EPIP Challenge 2026 Phase 2 presentation guide.
- Sovereign AI Blueprint: isiZulu Energy Intelligence for KZN Computer Labs.
- Lelapa AI / Vulavula documentation and examples.
- Relevant African-language NLP and energy-intelligence literature used in the final written report.
