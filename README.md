# Embedded Integration — Takeoff & Estimator Services

A demonstration solution showing bidirectional integration between **Takeoff** (data authoring service) and **Estimator** (data consumer service). Both services communicate over HTTP using a well-defined integration API, with fire-and-forget callbacks for changes and periodic polling for sync.

## ?? Overview

### Services

- **Takeoff** — Authoritative source for condition data. Provides creation, editing, and deletion of conditions with automatic summary computation. Sends callbacks to Estimator on every change.
- **Estimator** — Consumer service that receives data from Takeoff. Maintains a read-only copy, polls for updates, and never recomputes summaries (trusts Takeoff).

### Key Principles

1. **Takeoff is the single source of truth** for all condition data and computed summaries.
2. **Estimator does not compute summaries** — it receives and displays them exactly as provided.
3. **Bidirectional communication** — Takeoff pushes callbacks; Estimator pulls snapshots.
4. **Automatic aggregation** — Zone quantities roll up through pages, documents, to conditions.
5. **Data consistency** — Deletion callbacks trigger snapshot syncs to ensure consistency.

---

## ?? Getting Started

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

## ?? Documentation

All specifications are in the `docs/` folder:

### 1. **Integration API Specification** (`docs/integration-api-specification.md`)
The production-ready API contracts between services:
- Shared data contracts (Condition, Document, Page, TakeoffZone, Quantity)
- Data hierarchy and summary aggregation rules
- Integration endpoints (callbacks and pull)
- Configuration and error handling

### 2. **Demo Application Specification** (`docs/demo-application-specification.md`)
The interactive testing environment:
- In-memory data stores and sample data
- REST endpoints for testing (not part of production API)
- Web UI features (tree view, forms, polling)
- Complete callback and polling flow

### 3. **Step-by-Step Build Specification** (`docs/step-by-step-build-specification.md`)
Instructions to recreate the entire solution from scratch:
- 17 ordered steps, each producing working code
- Covers projects, contracts, services, controllers, UI
- Verification checkpoints at each step

### 4. **Contract Data Model Diagrams** (`docs/contract-diagrams.md`)
Visual representations using Mermaid:
- Class diagram of all data types
- Data hierarchy tree
- Summary aggregation flow
- Quantity grouping logic
- Message sequence diagrams (create/update, delete, pull)
- Callback type summary

---

## ??? Solution Structure

```
EmbeddedIntegration/
??? Contracts/                          # Shared data contracts (.NET 9)
?   ??? Quantity.cs                     # Measured quantity (name, unit, value)
?   ??? TakeoffZone.cs                  # Zone with raw summaries
?   ??? Page.cs                         # Page with aggregated summaries
?   ??? Document.cs                     # Document with aggregated summaries
?   ??? Condition.cs                    # Condition (root) with project summary
?
??? Takeoff.Api/                        # Data authoring service (.NET 9 Web API)
?   ??? Controllers/
?   ?   ??? InteractionsController.cs   # Integration API endpoints
?   ?   ??? DemoController.cs           # Demo endpoints (testing UI)
?   ??? Services/
?   ?   ??? TakeoffDataStore.cs         # In-memory store + summary computation
?   ?   ??? EstimatorClient.cs          # HTTP client for callbacks
?   ??? Options/
?   ?   ??? PeerServicesOptions.cs      # Configuration model
?   ??? wwwroot/
?   ?   ??? index.html                  # Takeoff UI (editable forms, tree)
?   ??? Program.cs                      # Host configuration
?   ??? appsettings.json                # Configuration
?   ??? Properties/launchSettings.json  # Launch profile
?   ??? Takeoff.Api.csproj
?
??? Estimator.Api/                      # Data consumer service (.NET 9 Web API)
?   ??? Controllers/
?   ?   ??? InteractionsController.cs   # Integration API endpoints
?   ?   ??? DemoController.cs           # Demo endpoints (testing UI)
?   ??? Services/
?   ?   ??? EstimatorDataStore.cs       # In-memory store (no computation)
?   ?   ??? TakeoffClient.cs            # HTTP client for pulling data
?   ??? Options/
?   ?   ??? PeerServicesOptions.cs      # Configuration model
?   ??? wwwroot/
?   ?   ??? index.html                  # Estimator UI (read-only tree, polling)
?   ??? Program.cs                      # Host configuration
?   ??? appsettings.json                # Configuration
?   ??? Properties/launchSettings.json  # Launch profile
?   ??? Estimator.Api.csproj
?
??? docs/
?   ??? integration-api-specification.md         # Production API contracts
?   ??? demo-application-specification.md        # Demo environment
?   ??? step-by-step-build-specification.md      # Build instructions
?   ??? contract-diagrams.md                     # Mermaid diagrams
?
??? EmbeddedIntegration.sln
??? README.md                           # This file
```

---

## ?? Integration API Endpoints

### Takeoff ? Estimator (Callbacks, Fire-and-Forget)

| Endpoint | Method | Payload | Purpose |
|----------|--------|---------|---------|
| `/api/interactions/condition-changed` | POST | `List<Condition>` | Create/update with computed summaries |
| `/api/interactions/condition-deleted` | POST | `{ projectId, conditionId }` | Condition removed |
| `/api/interactions/document-deleted` | POST | `{ projectId, documentId }` | Document removed |
| `/api/interactions/page-deleted` | POST | `{ projectId, pageId }` | Page removed |
| `/api/interactions/takeoffzone-deleted` | POST | `{ projectId, zoneId }` | Zone removed |

### Estimator ? Takeoff (Pull)

| Endpoint | Method | Response | Purpose |
|----------|--------|----------|---------|
| `/api/interactions/projects/{projectId}/conditions` | GET | `List<Condition>` | Get conditions for a project |

### Health

| Service | Endpoint | Response |
|---------|----------|----------|
| Both | `/api/interactions/health` | `"ok"` |

---

## ?? Data Model

### Contracts

```
Condition
??? id: Guid
??? projectId: Guid
??? projectSummary: List<Quantity>  (aggregated)
??? documents: List<Document>
    ??? id: Guid
    ??? documentSummary: List<Quantity>  (aggregated)
    ??? pages: List<Page>
        ??? id: Guid
        ??? pageNumber: int
        ??? pageSummary: List<Quantity>  (aggregated)
        ??? takeoffZones: List<TakeoffZone>
            ??? id: Guid
            ??? zoneSummary: List<Quantity>  (raw data)

Quantity
??? name: string
??? unit: string
??? value: double
```

### Summary Aggregation

Quantities are aggregated **bottom-up** by Takeoff:

1. **PageSummary** = sum of all zone summaries on the page, grouped by `(Name, Unit)`
2. **DocumentSummary** = sum of all page summaries in the document, grouped by `(Name, Unit)`
3. **ProjectSummary** = sum of all document summaries in the condition, grouped by `(Name, Unit)`

Estimator **never** recomputes these — it trusts the values from Takeoff.

---

## ?? Takeoff UI Features

- **Project Selector** — Choose which project to work with; auto-selects first project
- **Tree View** — Hierarchical display of conditions, documents, pages, zones with emoji icons
- **Stable Numbering** — Documents, pages, and zones maintain consistent sequential numbers across refreshes
- **Forms Panel** — Edit zone quantities, add/remove documents, pages, zones
- **Read-Only Summaries** — Display aggregated summaries for each level (condition, document, page)
- **Save Zone** — Edit raw zone quantities; server recomputes all summaries and sends callback to Estimator

---

## ?? Estimator UI Features

- **Pull Snapshot** — Fetch all data from Takeoff on demand
- **Auto Polling** — Every 500ms, poll for changes and refresh tree
- **Read-Only Tree** — Display conditions grouped by project
- **Info Panel** — Click a node to see its read-only details (ID, summaries)
- **Live Sync** — Receive updates via callbacks and display them within 500ms
- **Stable Numbering** — Same client-side numbering as Takeoff UI

---

## ?? Integration Flows

### Create/Update Condition

```
Takeoff UI
  ?
PUT /api/demo/conditions/{id}
  ?
TakeoffDataStore.Update()
  ? ComputeSummaries()  (zone ? page ? document ? project)
  ?
POST /api/interactions/condition-changed (fire-and-forget)
  ?
Estimator.UpsertByCallback()  (merge by ID)
  ?
Estimator UI polls ? tree updates within 500ms
```

### Delete with Snapshot Sync

```
Takeoff UI
  ?
DELETE /api/demo/zones/{id}
  ?
TakeoffDataStore.DeleteZone()
  ? ComputeSummaries()  (recalculate affected summaries)
  ?
POST /api/interactions/takeoffzone-deleted (fire-and-forget)
  ?
Estimator.DeleteTakeoffZone()
  ? GET /api/interactions/projects/{pid}/conditions (pull fresh data)
  ? ReplaceAll() (replace local store)
  ?
Estimator UI polls ? tree updates within 500ms
```

### Pull Snapshot

```
Estimator UI
  ?
POST /api/demo/pull-snapshot
  ?
TakeoffClient.PullSnapshotAsync()
  ? GET /api/demo/projects  (get all project IDs)
  ? For each project: GET /api/demo/projects/{pid}/conditions
  ?
EstimatorDataStore.ReplaceAllProjects()
  ?
GET /api/demo/all-conditions ? render tree
```

---

## ?? Configuration

### Takeoff (appsettings.json)

```json
{
  "PeerServices": {
    "EstimatorBaseUrl": "https://localhost:5002/",
    "HttpTimeoutSeconds": 10
  }
}
```

### Estimator (appsettings.json)

```json
{
  "PeerServices": {
    "TakeoffBaseUrl": "https://localhost:5001/",
    "HttpTimeoutSeconds": 10
  }
}
```

---

## ?? Testing Scenarios

1. **Initial Load** — Takeoff UI shows 4 sample conditions with correct summaries
2. **Pull Snapshot** — Estimator pulls data; tree matches Takeoff exactly
3. **Edit Zone Value** — Change a quantity ? Save ? Summaries recalculate ? Estimator updates via callback + polling
4. **Add Quantity Row** — Add row to zone ? Save ? Summaries recalculate
5. **Remove Quantity Row** — Remove row ? Save ? Summaries recalculate
6. **Delete Zone** — Summaries recalculate ? Estimator receives callback ? Pulls snapshot ? Summaries match
7. **Delete Page** — Cascades to summaries ? Callback ? Snapshot pull
8. **Delete Document** — Cascades to summaries ? Callback ? Snapshot pull
9. **Delete Condition** — Estimator receives callback ? Pulls snapshot
10. **Add Condition/Document/Page/Zone** — Created in Takeoff ? Callback sent ? Polling reflects change within 500ms

---

## ?? Sample Data

On startup, Takeoff initializes **1 project** with **4 conditions**:

| Condition | Quantities | Base Multiplier |
|-----------|-----------|-----------------|
| 1 | Length (ft), Width (ft), Height (ft), Area (ft?) | 50 |
| 2 | Volume (ft?), SurfaceArea (ft?), Weight (lbs), Density (lbs/ft?) | 75 |
| 3 | Distance (ft), Time (hours), Rate (ft/hr), Cost ($) | 100 |
| 4 | Quantity (units), UnitCost ($), TotalCost ($), Markup (%) | 125 |

All conditions share the same document/page/zone structure (3 docs ? 3 pages each, 1–2 zones per page, 12 zones total).

---

## ?? Error Handling

- **Takeoff callbacks** are fire-and-forget — if Estimator is unreachable, a warning is logged and Takeoff continues
- **Post-deletion snapshot pulls** are best-effort — if Takeoff is unreachable, the error is logged but the deletion response is still successful
- All HTTP errors are logged with structured logging (`ILogger<T>`)

---

## ??? Technology Stack

- **.NET 9** — Latest long-term support version
- **ASP.NET Core Web API** — HTTP services
- **C# 13** — Modern language features
- **In-Memory Stores** — No external databases
- **jQuery, jsTree, Bootstrap 5** — Frontend (CDN)
- **JSON** — camelCase naming policy

---

## ?? Additional Resources

- **Integration API Spec** — See `docs/integration-api-specification.md` for production API details
- **Demo Spec** — See `docs/demo-application-specification.md` for in-memory stores and UI
- **Build Instructions** — See `docs/step-by-step-build-specification.md` to rebuild from scratch
- **Diagrams** — See `docs/contract-diagrams.md` for visual representations (Mermaid)

---

## ?? License

This is a demonstration solution. Modify and use as needed for your project.

---

## ?? Author

Built as a demonstration of bidirectional service integration with automatic summary aggregation and consistency.

---

## ?? Next Steps

1. **Run both services** — Follow "Getting Started" above
2. **Explore the UIs** — Try editing, adding, deleting data in Takeoff
3. **Watch the sync** — See changes reflected in Estimator within 500ms
4. **Review the specs** — Read the documentation in `docs/` for detailed API and architecture
5. **Adapt to your needs** — Use this as a template for your own integration scenarios