# Embedded Integration

Small sample solution demonstrating two cooperating ASP.NET Core Web APIs:
- `Takeoff.Api` — owns `TakeoffData` and provides a jQuery UI for CRUD on `Condition` objects.
- `Estimator.Api` — owns `EstimatorData` and can pull snapshots from Takeoff or receive callbacks.

Targets: .NET 9

Quick start
- Run both projects (in Visual Studio choose each project and Start) or run them from the project folders.
- Default local HTTPS ports used by the sample:
  - Takeoff: `https://localhost:5001/` (HTTP `http://localhost:5000/`)
  - Estimator: `https://localhost:5002/` (HTTP `http://localhost:5003/`)

UIs
- Takeoff UI: `https://localhost:5001/index.html`
- Estimator UI: `https://localhost:5002/index.html`

Configuration
- Peer service base URLs are configured in each app `appsettings.json` under the `PeerServices` section:
  - `Takeoff.Api/appsettings.json` contains `PeerServices:EstimatorBaseUrl` (defaults to `https://localhost:5002/`).
  - `Estimator.Api/appsettings.json` contains `PeerServices:TakeoffBaseUrl` (defaults to `https://localhost:5001/`).

API endpoints (grouped by controller)

Takeoff.Api

- `DemoController`
  - `GET  /api/demo/projects` — list known project ids
  - `GET  /api/demo/projects/{projectId}/conditions` — list conditions for a project (snapshot)
  - `GET  /api/demo/projects/{projectId}/conditions/{conditionId}` — get single condition
  - `POST /api/demo/conditions` — create condition
  - `PUT  /api/demo/conditions/{conditionId}` — update condition
  - `DELETE /api/demo/projects/{projectId}/conditions/{conditionId}` — delete condition
  - `POST /api/demo/guids` — returns a new GUID (used by UIs)

- `InteractionsController` (programmatic cross-service endpoints)
  - `GET  /api/interactions/projects/{projectId}/conditions` — return the current list of `Condition` instances for the specified project (used by Estimator snapshot pulls)
  - `GET  /api/interactions/health` — simple health check

Estimator.Api

- `DemoController` (Estimator local/read APIs and snapshot pull)
  - `GET  /api/demo/projects` — list project ids known to Estimator
  - `GET  /api/demo/projects/{projectId}/conditions` — return Estimator-stored conditions for a project
  - `GET  /api/demo/projects/{projectId}/conditions/{conditionId}` — return single condition from Estimator store
  - `POST /api/demo/snapshot/pull` — Estimator initiates a snapshot pull from Takeoff for a project

- `InteractionsController` (callbacks from Takeoff)
  - `POST /api/interactions/condition-changed` — receive single-condition callback from Takeoff
  - `POST /api/interactions/condition-deleted` — receive delete callback from Takeoff
  - `GET  /api/interactions/health` — simple health check

Contracts
- `Condition` exposes `Measurements : List<Measurement>`.
- `Measurement` has:
  - `MeasurementName` (string)
  - `UnitsOfMeasurements` (string)
  - `Value` (double)

Docs and diagrams
- Interaction API docs: `docs/INTERACTIONS.md`
- Production-oriented spec: `docs/PRODUCTION_SPEC.md`
- Contracts diagrams: `docs/CONTRACTS_DIAGRAMS.md`
- Demo project spec: `Contracts/sample_project_spec.md`

Estimator merge behavior
- When a `Condition` callback arrives at Estimator, Estimator updates its condition measurements by matching `MeasurementName`. The current demo implementation replaces the stored condition's `Measurements` with the incoming set (measurements not present in the incoming payload are removed). Production implementations should consider idempotency and ordering.