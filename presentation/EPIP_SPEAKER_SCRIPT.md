# EPIP 2026 — Speaker Script

Target: approximately 15 minutes presentation + 5 minutes Q&A.

## Slide 1 — Title | 30 seconds
Good morning. My project is called **isiZulu Energy Intelligence for KZN Computer Labs**. The idea is simple: a computer lab should not only tell us that energy is being wasted; it should tell the lab manager exactly where the waste is happening and explain what to do in a language they understand.

## Slide 2 — Introduction | 1 minute
Computer labs produce a lot of technical information. CPU activity, system status, energy estimates and other telemetry are normally presented in English. A lab manager can be technically capable and still struggle when a message combines unfamiliar electrical terminology with English technical language. My project focuses on removing that barrier by connecting computer-level telemetry with an isiZulu language layer.

## Slide 3 — Problem Statement | 1 minute
The problem I am solving is not simply translation. The real problem is the distance between raw telemetry and an action. If a system knows that Computer 14 has been idle for hours, that information is useful only if the manager can identify Computer 14, understand why it matters, and know what action to take. My research question is therefore how to provide computer-level energy-waste detection together with actionable isiZulu guidance.

## Slide 4 — Aim & Objectives | 45 seconds
The aim is to design and prototype an isiZulu energy-intelligence system for computer labs. The first objective is to design the telemetry and detection workflow. The second is to implement the API and dashboard that turns a detected event into an isiZulu message. The third is to evaluate the prototype using simulated lab telemetry and real heartbeats from the lightweight agent.

## Slide 5 — Related Work | 1 minute
There are three important areas of related work. Energy dashboards provide visibility, but often at building level. Translation systems provide language access, but translation alone does not know which computer is wasting energy or what the operator should do. African language AI gives us an important language foundation. My contribution is to combine these pieces into one domain-specific workflow for computer labs.

## Slide 6 — Gap | 45 seconds
This is the gap I identified. We need a complete chain: telemetry, detection, computer-level attribution, domain-specific explanation and local-language action. If any one of these is missing, the operator still has to perform the translation or investigation themselves. This project puts those stages together.

## Slide 7 — Architecture | 1 minute
The architecture starts with a lab computer. A lightweight agent collects telemetry and sends a heartbeat to an ASP.NET Core API. The API detects waste patterns and exposes the dashboard. When an alert needs to be communicated, the server sends the technical message to the language service and returns the isiZulu result to the operator interface. The important security point is that the browser never needs the language-service credential.

## Slide 8 — Detection | 1 minute
For the prototype, the system demonstrates idle and overnight patterns. Each event is associated with a specific computer and lab. The prototype calculates an estimated energy and carbon signal so that the dashboard can demonstrate the complete workflow. I want to be very clear about the limitation: software alone cannot accurately claim to measure mains electricity on an ordinary computer. Production deployment must use validated power telemetry or meters.

## Slide 9 — Language Layer | 45 seconds
The language layer is where the project becomes useful to the intended operator. The English diagnostic is sent through the server-side translation proxy. The prototype uses Vulavula language codes for English and isiZulu. The browser never receives the API token. If the language service is not configured, the prototype has a small demo fallback so that the user interface can still be demonstrated.

## Slide 10 — Demonstration: Detect | 1 minute
Now I would demonstrate the actual system. I click **Run lab scan**. The dashboard shows 20 computers. Some are normal and some are flagged. I select a flagged computer and point out that this is different from a building-level alert: I can identify the exact machine and the condition that triggered the alert.

## Slide 11 — Demonstration: Translate | 1 minute
Next I click **Translate selected alert**. The English message describes the detected problem. The language service converts that into isiZulu. This is the key moment of the demonstration: the system has moved from telemetry, to a technical diagnosis, to a local-language instruction. The manager does not need to manually translate the alert before deciding what to do.

## Slide 12 — Demonstration: Agent | 1 minute
The second part is the agent. The Python agent uses `psutil` to read actual CPU utilisation from the computer it runs on. It sends a heartbeat containing the machine identity, lab room and activity signal. For the prototype, wattage is estimated. The next research step is to replace that estimate with validated power measurements so the system can make defensible energy claims.

## Slide 13 — Tools | 45 seconds
The implementation uses ASP.NET Core 8 and C# for the API, a lightweight HTML and JavaScript dashboard, Python and psutil for the computer agent, and Vulavula for the African-language translation layer. GitHub is used for version control. The architecture is intentionally lightweight so that the project can grow from a demonstration to a real lab deployment.

## Slide 14 — Contribution | 1 minute
The main contribution is not simply an isiZulu translator. It is the combination of computer-level attribution, energy-waste detection, domain-specific messages and isiZulu operator communication. The system is designed around the user's actual workflow: identify the machine, understand the problem and act. This is what makes the language technology useful rather than just decorative.

## Slide 15 — Conclusion | 1 minute
In conclusion, the prototype demonstrates the complete journey from a computer event to an actionable isiZulu message. The next stage is validation. I need to compare the estimated telemetry with real power measurements, collect natural KZN-specific technical terminology, test the system with lab managers, measure comprehension and response time, and then integrate WhatsApp for actual alert delivery.

## Slide 16 — References | 30 seconds
These are the main sources and technical references used to develop the project. The final version of the presentation will contain the full academic references required by EPIP.

## Closing | 20 seconds
The message I want the judges to remember is: **Your language. Your energy. Your data.** The goal is not to make the operator understand the machine's language. The goal is to make the machine explain itself in the operator's language.
