# Embedded Integration — Takeoff & Estimator Services

A demonstration solution showing bidirectional integration between **Takeoff** (data authoring service) and **Estimator** (data consumer service). Both services communicate over HTTP using a well-defined integration API, with fire-and-forget callbacks for changes and periodic polling for sync.

## Overview

### Services

- **Takeoff** — Authoritative source for condition data. Provides creation, editing, and deletion of conditions with automatic summary computation. Sends callbacks to Estimator on every change.
- **Estimator** — Consumer service that receives data from Takeoff. Maintains a read-only copy, polls for updates, and never recomputes summaries (trusts Takeoff).

### Key Principles

1. Takeoff is the single source of truth for all condition data and computed summaries.
2. Estimator does not compute summaries — it receives and displays them exactly as provided.
3. Bidirectional communication — Takeoff pushes callbacks; Estimator pulls snapshots.
4. Automatic aggregation — Zone quantities roll up through pages, documents, to conditions.
5. Data consistency — Deletion callbacks trigger snapshot syncs to ensure consistency.

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

Both UIs will open in your browser. Navigate to them via the URLs printed in the terminal.

---

## Documentation

All specification documents are in the `docs/` folder:

- `docs/integration-api-specification.md` — Production API contracts
- `docs/demo-application-specification.md` — Demo environment and UI
- `docs/step-by-step-build-specification.md` — Rebuild instructions
- `docs/contract-diagrams.md` — Mermaid diagrams of contracts and flows

---

## Solution Structure

```
EmbeddedIntegration/
├── Contracts/
├── Takeoff.Api/
├── Estimator.Api/
├── docs/
└── EmbeddedIntegration.sln
```

Refer to the `docs/` folder for the full tree and file list.

---

## Integration API Endpoints

### Takeoff → Estimator (Callbacks)

- `POST /api/interactions/condition-changed` — Body: `List<Condition>` (full condition with computed summaries)
- `POST /api/interactions/condition-deleted` — Body: `{ projectId, conditionId }`
- `POST /api/interactions/document-deleted` — Body: `{ projectId, documentId }`
- `POST /api/interactions/page-deleted` — Body: `{ projectId, pageId }`
- `POST /api/interactions/takeoffzone-deleted` — Body: `{ projectId, zoneId }`

### Estimator → Takeoff (Pull)

- `GET /api/interactions/projects/{projectId}/conditions` — Returns `List<Condition>` for the project
- `GET /api/interactions/health` — Health check (`"ok"`)

---

## Data Model

Top-level `Condition` contains `ProjectSummary` and a list of `Document` items. Each `Document` has a `DocumentSummary` and pages; each `Page` has a `PageSummary` and `TakeoffZones`; each `TakeoffZone` contains `ZoneSummary` (raw quantities). Quantities are grouped by `(Name, Unit)` and summed during aggregation.

---

## UI Features

### Takeoff
- Editable zone quantities
- Add/remove documents, pages, zones
- Project selector and hierarchical tree
- Server computes all summaries on save and notifies Estimator

### Estimator
- Pull snapshot button
- Auto-polling (500ms) to detect changes
- Read-only tree and detail panel

---

## Testing Scenarios

1. Initial load: Takeoff UI shows sample conditions with summaries
2. Pull snapshot: Estimator pulls data and displays the same tree
3. Edit zone value: Save in Takeoff → summaries update → Estimator updates via callback + polling
4. Add/remove quantity rows: Save → summaries update
5. Delete zone/page/document/condition: Takeoff notifies Estimator → Estimator pulls snapshot to sync

---

## Configuration

See `appsettings.json` in each project for the peer base URL and HTTP timeout under `PeerServices`.

---

## Additional Resources

See the `docs/` folder for full specifications and diagrams.

---

## License

This is a demonstration solution. Modify and use as needed.

---