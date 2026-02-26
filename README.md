# Embedded Integration — Takeoff & Estimator Services

A demonstration solution showing bidirectional integration between **Takeoff** (data authoring service) and **Estimator** (data consumer service). Both services communicate over HTTP using a well-defined integration API with fire-and-forget callbacks for changes and automatic snapshot pulls after deletions.

## Overview

### Services

- **Takeoff** — Authoritative source for condition data and computed summaries. Provides REST API for creating, editing, and deleting conditions. Sends fire-and-forget callbacks to Estimator on every change.
- **Estimator** — Consumer service that receives data from Takeoff. Maintains a local copy and polls for updates. Never computes summaries (trusts Takeoff).

### Core Architecture

1. **Takeoff is the single source of truth** — All condition data and quantity summaries are computed and stored in Takeoff.
2. **Estimator trusts Takeoff** — Estimator never recomputes summaries; it displays data exactly as provided.
3. **Bidirectional communication** — Takeoff pushes change/deletion callbacks; Estimator pulls snapshots after deletions.
4. **Automatic summary aggregation** — Takeoff computes zone quantities → page summaries → document summaries → project summaries (bottom-up).
5. **Consistency via snapshot pulls** — After any deletion, Estimator pulls a fresh snapshot to reconcile recalculated summaries.

---

## Getting Started

### Prerequisites

- .NET 9 SDK
- Visual Studio 2022 or VS Code
- Optional: Git for version control

### Clone & Build

```bash
git clone https://github.com/pkarabut-bw/embedded-integration.git
cd EmbeddedIntegration
dotnet build
```

### Run Both Services

In separate terminals:

```bash
# Terminal 1 — Takeoff (https://localhost:5001 | http://localhost:5000)
cd Takeoff.Api
dotnet run

# Terminal 2 — Estimator (https://localhost:5002 | http://localhost:5003)
cd Estimator.Api
dotnet run
```

Both UIs will open in your browser.

---

## Documentation

Specification documents are organized in the `docs/` folder:

### Integration Contracts
- **`docs/integration-api-specification.md`** — Production REST API contracts, endpoints, payloads, and error handling
- **`docs/contract-diagrams.md`** — Message flow diagrams and integration patterns (Mermaid)

### Demo & Implementation
- **`docs/demo/`** — Demo application and implementation guides
  - `demo-application-specification.md` — Demo app features, UI, data flow
  - `step-by-step-build-specification.md` — Step-by-step implementation guide

---

## Solution Structure

```
EmbeddedIntegration/
├── Contracts/                  # Shared data contracts
├── Takeoff.Api/               # Takeoff service (authoritative source)
├── Estimator.Api/             # Estimator service (consumer)
├── docs/                       # Documentation
│   ├── integration-api-specification.md
│   ├── contract-diagrams.md
│   └── demo/                   # Demo implementation guides
├── EmbeddedIntegration.sln
└── README.md
```

---

## Integration API Summary

### Callbacks (Takeoff → Estimator)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/interactions/conditions-changed` | POST | Notify of condition changes (create/update) |
| `/api/interactions/conditions-deleted` | POST | Notify of condition deletions |
| `/api/interactions/documents-deleted` | POST | Notify of document deletions |
| `/api/interactions/pages-deleted` | POST | Notify of page deletions |
| `/api/interactions/takeoffzones-deleted` | POST | Notify of zone deletions |
| `/api/interactions/project-deleted` | POST | Notify of project deletion |

### Pull (Estimator → Takeoff)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/interactions/projects/{projectId}/conditions-all` | GET | Get all conditions for a project |
| `/api/interactions/health` | GET | Health check |

**Post-Deletion Flow**: After any deletion callback (except project deletion), Estimator automatically pulls a fresh project snapshot to reconcile recalculated summaries.

---

## Data Model

The data model is a hierarchy of contracts with computed quantity summaries at each level:

```
ProjectConditionQuantities
├── Quantities (computed by Takeoff)
└── DocumentConditionQuantities[]
    ├── Quantities (computed by Takeoff)
    └── PageConditionQuantities[]
        ├── Quantities (computed by Takeoff)
        └── TakeoffZoneConditionQuantities[]
            └── Quantities (raw data from zones)
```

**Summary computation** (Takeoff only):
- **Page summaries** = aggregated zone quantities
- **Document summaries** = aggregated page summaries
- **Project summaries** = aggregated document summaries

All summaries are computed by Takeoff and **never** recomputed by Estimator. Estimator trusts summaries exactly as provided by Takeoff.

---

## Technology Stack

- **.NET 9** — Latest LTS framework
- **ASP.NET Core** — REST APIs
- **In-Memory Storage** — `Dictionary`-based data stores (no database)
- **HTTP/JSON** — Standard JSON serialization with camelCase naming
- **jQuery + jsTree** — Frontend UI (demo only)

---

## Development

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run Individual Services
```bash
dotnet run --project Takeoff.Api
dotnet run --project Estimator.Api
```

---

## References

For detailed specifications, implementation guides, and diagrams, see the `docs/` folder.