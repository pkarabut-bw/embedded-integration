# EmbeddedIntegration – Detailed Specification (Takeoff + Estimator)

## 0. Scope
Two ASP.NET Core Web API applications interact with each other:
- `Takeoff.Api` (owns `TakeoffData`)
- `Estimator.Api` (owns `EstimatorData`)

Both applications:
- Target **.NET 9**.
- Reference the shared class library `Contracts`.
- Expose **Web API controllers** (no Minimal APIs).
- Host a small **jQuery-based UI** served as static files (no SPA frameworks).
- Store the other application’s base URL in configuration to enable HTTP calls.

Data is stored **in-memory** in singleton services (no database).

---

## 1. Repository / Solution structure
Create a solution (or extend existing) with the following projects:

### 1.1 Projects
1. `Contracts` (**existing**, class library)
   - Path: `Contracts/Contracts.csproj`
   - Contains shared DTOs.

2. `Takeoff.Api` (new, ASP.NET Core Web API)
   - Path: `Takeoff.Api/Takeoff.Api.csproj`
   - References `Contracts`.
   - Serves UI: `wwwroot/takeoff/index.html` (or `wwwroot/index.html`).

3. `Estimator.Api` (new, ASP.NET Core Web API)
   - Path: `Estimator.Api/Estimator.Api.csproj`
   - References `Contracts`.
   - Serves UI: `wwwroot/estimator/index.html` (or `wwwroot/index.html`).

### 1.2 Internal layering (per API project)
Use a simple folder structure:
- `Controllers/`
  - `DemoController.cs` (local/demo UI endpoints and snapshot pull on Estimator)
  - `InteractionsController.cs` (cross-service callbacks and health)
- `Services/`
  - `TakeoffDataStore.cs` / `EstimatorDataStore.cs`
  - `PeerClient.cs` (typed HTTP client)
- `Models/` (only if API-specific request DTOs are needed)

---

## 2. Shared contracts (`Contracts` project)
Namespace: `Contracts`

### 2.1 `Condition`
File: `Contracts/Condition.cs`
```csharp
public class Condition
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public List<Measurement> Measurements { get; set; }
}
```

**Invariants**
- `Id` is immutable after creation.
- `ProjectId` groups conditions; snapshot sync is always **by `ProjectId`**.
- `Measurements` is the collection of measurement entries for the condition. Each `Measurement` contains name, units and value. For snapshots the list typically contains the full set; for callbacks it may contain only changed measurements.

### 2.2 `Measurement`
File: `Contracts/Measurement.cs`
```csharp
public class Measurement
{
    public string MeasurementName { get; set; }
    public string UnitsOfMeasurements { get; set; }
    public double Value { get; set; }
}
```

### 2.3 Serialization
- Use `System.Text.Json`.
- Configure JSON options:
  - `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
  - `PropertyNameCaseInsensitive = true`
- Request/response payloads are UTF-8 JSON.

---

## 3. Configuration
Each service stores the peer base URL.

### 3.1 `appsettings.json`
#### `Takeoff.Api`
- `PeerServices:EstimatorBaseUrl` (e.g., `https://localhost:5002/`)

#### `Estimator.Api`
- `PeerServices:TakeoffBaseUrl` (e.g., `https://localhost:5001/`)

Common:
- `PeerServices:HttpTimeoutSeconds` (default: `10`)

### 3.2 Options classes
Per API project:
- `PeerServicesOptions`
  - `string EstimatorBaseUrl` (Takeoff only)
  - `string TakeoffBaseUrl` (Estimator only)
  - `int HttpTimeoutSeconds`

---

## 4. Controller responsibilities and routes

Both projects use two controllers with clear separation:

1) `DemoController` (local/demo UI and snapshot pull on Estimator)
- Takeoff.Api `DemoController` exposes CRUD and snapshot read endpoints used by UIs and peers:
  - `GET  /api/demo/projects` — list project ids
  - `GET  /api/demo/projects/{projectId}/conditions` — snapshot of conditions for a project
  - `GET  /api/demo/projects/{projectId}/conditions/{conditionId}` — single condition
  - `POST /api/demo/conditions` — create condition
  - `PUT  /api/demo/conditions/{conditionId}` — update condition
  - `DELETE /api/demo/projects/{projectId}/conditions/{conditionId}` — delete condition
  - `POST /api/demo/guids` — generate GUID
- Estimator.Api `DemoController` exposes:
  - `GET  /api/demo/projects` — list project ids (from EstimatorData)
  - `GET  /api/demo/projects/{projectId}/conditions` — return Estimator-stored conditions for that project
  - `GET  /api/demo/projects/{projectId}/conditions/{conditionId}` — single condition (Estimator)
  - `POST /api/demo/snapshot/pull` — (Estimator only) ask Takeoff for snapshot and replace Estimator data

2) `InteractionsController` (cross-service callbacks and health)
- Takeoff.Api `InteractionsController` (optional cross-service read) exposes:
  - `GET  /api/interactions/projects/{projectId}/conditions` (read-only mirror of Takeoff snapshot) — optional
  - `GET  /api/interactions/health`
- Estimator.Api `InteractionsController` exposes callback endpoints used by Takeoff:
  - `POST /api/interactions/condition-changed` — receive single condition updates
  - `POST /api/interactions/condition-deleted` — receive deletes
  - `GET  /api/interactions/health`

Notes:
- Snapshot pull is initiated on Estimator via `POST /api/demo/snapshot/pull` which uses `TakeoffClient` to call the Takeoff `GET /api/demo/projects/{projectId}/conditions` endpoint.
- Estimator stores snapshots in `EstimatorDataStore` via `ReplaceAll`.

---

## 5. Algorithms (summary)
(unchanged from previous spec except for endpoint locations)

---

## 6. UI bindings (summary)
- Takeoff UI uses `GET /api/demo/projects/{projectId}/conditions` and CRUD endpoints under `/api/demo`.
- Estimator UI triggers snapshot pull via `POST /api/demo/snapshot/pull` and reads stored Estimator data via `GET /api/demo/projects/{projectId}/conditions`.

---

## 7. Acceptance criteria
(unchanged)
