# Demo Application Specification

## 1. Overview

This document specifies the demo application that exercises the Takeoff–Estimator integration. The demo provides an in-memory data store, sample data, a web UI for each service, and additional REST endpoints that support interactive testing. None of the components described here are part of the integration API itself; they exist solely to make the integration observable and testable.

---

## 2. Solution Structure

```
EmbeddedIntegration/
??? Contracts/                        # Shared data contracts (see Integration API Specification)
??? Takeoff.Api/
?   ??? Controllers/
?   ?   ??? DemoController.cs         # Demo REST endpoints for Takeoff UI
?   ?   ??? InteractionsController.cs # Integration API endpoints (see Integration API Specification)
?   ??? Services/
?   ?   ??? TakeoffDataStore.cs       # In-memory store with sample data and summary computation
?   ?   ??? EstimatorClient.cs        # HTTP client for sending callbacks to Estimator
?   ??? Options/
?   ?   ??? PeerServicesOptions.cs    # Configuration model
?   ??? wwwroot/
?   ?   ??? index.html                # Takeoff UI (SPA)
?   ??? Program.cs                    # Host configuration
?   ??? appsettings.json              # Configuration
?   ??? Properties/launchSettings.json
??? Estimator.Api/
?   ??? Controllers/
?   ?   ??? DemoController.cs         # Demo REST endpoints for Estimator UI
?   ?   ??? InteractionsController.cs # Integration API endpoints (see Integration API Specification)
?   ??? Services/
?   ?   ??? EstimatorDataStore.cs     # In-memory store
?   ?   ??? TakeoffClient.cs          # HTTP client for pulling data from Takeoff
?   ??? Options/
?   ?   ??? PeerServicesOptions.cs    # Configuration model
?   ??? wwwroot/
?   ?   ??? index.html                # Estimator UI (SPA)
?   ??? Program.cs                    # Host configuration
?   ??? appsettings.json              # Configuration
?   ??? Properties/launchSettings.json
??? docs/
```

### Technology Stack

- .NET 9, ASP.NET Core Web API
- No external databases — all data is in-memory (`Dictionary`-based stores)
- Frontend: single `index.html` per service using jQuery, jsTree, Bootstrap 5 (loaded from CDN)
- JSON serialization with camelCase naming policy

---

## 3. Network Configuration

| Service   | HTTPS             | HTTP              |
|-----------|-------------------|-------------------|
| Takeoff   | `localhost:5001`  | `localhost:5000`  |
| Estimator | `localhost:5002`  | `localhost:5003`  |

Takeoff points to Estimator at `https://localhost:5002/`.
Estimator points to Takeoff at `https://localhost:5001/`.

---

## 4. Takeoff Demo

### 4.1 Sample Data (TakeoffDataStore)

On startup, `TakeoffDataStore` initializes one project containing **4 conditions**. All conditions share the same document/page/zone ID structure (3 documents ? 3 pages each, with 1–2 zones per page, 12 zones total).

Each condition uses a unique set of quantity names and units:

| Condition | Quantities |
|-----------|-----------|
| 1 | Length (ft), Width (ft), Height (ft), Area (ft?) |
| 2 | Volume (ft?), SurfaceArea (ft?), Weight (lbs), Density (lbs/ft?) |
| 3 | Distance (ft), Time (hours), Rate (ft/hr), Cost ($) |
| 4 | Quantity (units), UnitCost ($), TotalCost ($), Markup (%) |

Zone quantity values are generated using: `value = (baseMultiplier + (quantityIndex * 10)) * scaleFactor`, rounded to 2 decimal places. `baseMultiplier` increases by 25 per condition (50, 75, 100, 125). `scaleFactor` varies per zone (0.6–1.3).

After creating all conditions, `ComputeSummaries()` is called on each to calculate page, document, and project summaries bottom-up.

### 4.2 Summary Computation (ComputeSummaries)

Located in `TakeoffDataStore`. Called after:
- Initial sample data creation
- Any `Update()` call (zone quantity changes)
- Any `DeleteDocument()`, `DeletePage()`, or `DeleteZone()` call

Algorithm:
1. For each page: aggregate all zone summaries grouped by `(Name, Unit)`, summing values ? `PageSummary`
2. For each document: aggregate all page summaries ? `DocumentSummary`
3. For the condition: aggregate all document summaries ? `ProjectSummary`

### 4.3 Demo REST API (Takeoff DemoController)

Route prefix: `/api/demo`

| Method   | Path                                             | Description |
|----------|--------------------------------------------------|-------------|
| `GET`    | `/projects`                                      | List all project IDs |
| `GET`    | `/projects/all-with-data`                        | All projects with their conditions |
| `GET`    | `/projects/all`                                  | Same as above (alias) |
| `GET`    | `/projects/{projectId}/conditions`               | All conditions for a project |
| `GET`    | `/projects/{projectId}/conditions/{conditionId}` | Single condition |
| `POST`   | `/conditions`                                    | Create a condition (+ callback to Estimator) |
| `PUT`    | `/conditions/{conditionId}`                      | Update a condition (+ callback to Estimator) |
| `DELETE` | `/projects/{projectId}/conditions/{conditionId}` | Delete condition (+ callback to Estimator) |
| `DELETE` | `/projects/{projectId}/documents/{documentId}`   | Delete document from all conditions (+ callback) |
| `DELETE` | `/projects/{projectId}/pages/{pageId}`           | Delete page from all conditions (+ callback) |
| `DELETE` | `/projects/{projectId}/zones/{zoneId}`           | Delete zone from all conditions (+ callback) |
| `POST`   | `/guids`                                         | Generate a new GUID |

All mutation endpoints trigger fire-and-forget callbacks to Estimator via `EstimatorClient`.

### 4.4 Takeoff UI

Single-page HTML application served from `wwwroot/index.html`.

**Layout:** Two-column — left panel is a jsTree tree view, right panel is a form/detail panel.

**Tree structure:**
```
?? Project
  ?? Condition 1
    ?? Document 1
      ?? Page 1
        ?? Zone 1
        ?? Zone 2
      ?? Page 2
      ...
    ?? Document 2
    ...
  ?? Condition 2
  ...
```

**Features:**
- Project selector dropdown (auto-selects first project on load)
- Click any tree node to see its details in the right panel
- **Condition panel:** Shows ID, ProjectId, ProjectSummary (read-only table). Buttons: Add Document, Delete Condition.
- **Document panel:** Shows ID, DocumentSummary (read-only table). Buttons: Add Page, Delete Document.
- **Page panel:** Shows ID, PageNumber, PageSummary (read-only table). Buttons: Add Zone, Delete Page.
- **Zone panel:** Shows ID, ZoneSummary (editable table with Name, Unit, Value columns + remove row button). Buttons: Add Row, Save Zone, Delete Zone.
- Stable numbering: Documents, Pages, and Zones get client-side sequential numbers that persist across tree refreshes.
- Spinner overlay during API calls.

**Data flow on Save Zone:**
1. Collect zone summary rows from the form
2. Fetch the full condition from the server
3. Replace the zone's `zoneSummary` in the condition
4. `PUT /api/demo/conditions/{id}` — server recomputes all summaries and sends callback to Estimator
5. Reload the tree

---

## 5. Estimator Demo

### 5.1 Data Store (EstimatorDataStore)

In-memory store. Starts empty. Populated by:
- **Pull Snapshot**: Fetches all data from Takeoff on demand
- **Callbacks**: Receives incremental updates from Takeoff

Key operations:
- `ReplaceAllProjects(List<Condition>)` — clears and replaces all data
- `ReplaceAll(projectId, List<Condition>)` — replaces data for a single project
- `UpsertByCallback(List<Condition>)` — merges incoming condition data by ID at every level (condition ? document ? page ? zone); always overwrites `ProjectSummary` from the callback
- `Delete`, `DeleteDocument`, `DeletePage`, `DeleteTakeoffZone` — delete by ID, searching through all parent entities (no hierarchy path needed)

### 5.2 TakeoffClient

HTTP client used by Estimator to pull data from Takeoff.

| Method | Description |
|--------|-------------|
| `GetAllProjectIdsAsync()` | `GET /api/demo/projects` ? `List<Guid>` |
| `GetAllConditionsAsync(projectId)` | `GET /api/demo/projects/{projectId}/conditions` ? `List<Condition>` |
| `PullSnapshotAsync()` | Gets all project IDs, then fetches conditions for each project |
| `PullProjectSnapshotAsync(projectId)` | Fetches conditions for a single project |

### 5.3 Demo REST API (Estimator DemoController)

Route prefix: `/api/demo`

| Method | Path                                             | Description |
|--------|--------------------------------------------------|-------------|
| `GET`  | `/projects`                                      | List all project IDs in local store |
| `GET`  | `/projects/{projectId}/conditions`               | All conditions for a project from local store |
| `GET`  | `/projects/{projectId}/conditions/{conditionId}` | Single condition from local store |
| `POST` | `/snapshot/pull`                                 | Pull snapshot for one project from Takeoff |
| `POST` | `/pull-snapshot`                                 | Pull full snapshot (all projects) from Takeoff |
| `GET`  | `/all-conditions`                                | All conditions across all projects from local store |

### 5.4 Estimator UI

Single-page HTML application served from `wwwroot/index.html`.

**Layout:** Two-column — left panel is a read-only jsTree tree view, right panel is a read-only detail panel.

**Tree structure:**
```
All Projects
  ?? Project: {guid}
    ?? Condition 1
      ?? Document 1
        ?? Page 1
          ?? Zone 1
          ...
```

- Root node "All Projects" has no icon.
- All other nodes use the same emoji icons as Takeoff.

**Features:**
- **Pull Snapshot** button: calls `POST /api/demo/pull-snapshot`, then refreshes tree from `GET /api/demo/all-conditions`.
- **Polling**: Every 500ms, polls `GET /api/demo/all-conditions`. Compares full JSON string to detect any change. If changed, refreshes the tree and updates `allConditionsData` reference.
- **Detail panel (read-only):**
  - Condition: ID, ProjectId, ProjectSummary table
  - Document: ID, DocumentSummary table
  - Page: ID, PageNumber, PageSummary table
  - Zone: ID, ZoneSummary table
- Click a node in the tree to display its details in the right panel.
- Stable numbering for documents, pages, and zones (same approach as Takeoff UI).

---

## 6. Host Configuration

### Takeoff (Program.cs)

- Registers `TakeoffDataStore` as singleton
- Registers `EstimatorClient` as typed HTTP client with configured base URL and timeout
- Configures JSON serialization: camelCase, case-insensitive deserialization
- Serves static files from `wwwroot` with cache-busting headers for `index.html`
- Maps controllers and falls back to `index.html` for SPA routing

### Estimator (Program.cs)

- Registers `EstimatorDataStore` as singleton
- Registers `TakeoffClient` as typed HTTP client with configured base URL and timeout
- Configures JSON serialization: camelCase, case-insensitive deserialization
- Serves static files from `wwwroot`
- Maps controllers and falls back to `index.html` for SPA routing

---

## 7. Callback Flow Summary

### On Condition Create/Update (Takeoff)

```
Takeoff UI ? PUT /api/demo/conditions/{id}
  ? TakeoffDataStore.Update() ? Clone + ComputeSummaries
  ? Return updated condition to UI
  ? Fire-and-forget: EstimatorClient.SendConditionChangedAsync()
    ? POST /api/interactions/condition-changed to Estimator
      ? EstimatorDataStore.UpsertByCallback() (merge + overwrite ProjectSummary)
```

### On Deletion (Takeoff)

```
Takeoff UI ? DELETE /api/demo/projects/{pid}/zones/{zid}
  ? TakeoffDataStore.DeleteZone() ? Remove zone + ComputeSummaries
  ? Return 204 to UI
  ? Fire-and-forget: EstimatorClient.SendTakeoffZoneDeletedAsync()
    ? POST /api/interactions/takeoffzone-deleted to Estimator
      ? EstimatorDataStore.DeleteTakeoffZone()
      ? TakeoffClient.PullProjectSnapshotAsync() ? GET /api/interactions/projects/{pid}/conditions
      ? EstimatorDataStore.ReplaceAll() with fresh snapshot
```

### On Pull Snapshot (Estimator)

```
Estimator UI ? POST /api/demo/pull-snapshot
  ? TakeoffClient.PullSnapshotAsync()
    ? GET /api/demo/projects ? list of project IDs
    ? For each project: GET /api/demo/projects/{pid}/conditions
  ? EstimatorDataStore.ReplaceAllProjects()
  ? Return { success, conditionCount }
```
