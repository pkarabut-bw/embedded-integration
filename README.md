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

Important API endpoints (Takeoff)
- `GET  /api/demo/projects` — list known project ids
- `GET  /api/demo/projects/{projectId}/conditions` — list conditions for a project (snapshot)
- `GET  /api/demo/projects/{projectId}/conditions/{conditionId}` — get single condition
- `POST /api/demo/conditions` — create condition
- `PUT  /api/demo/conditions/{conditionId}` — update condition
- `DELETE /api/demo/projects/{projectId}/conditions/{conditionId}` — delete condition
- `POST /api/demo/guids` — returns a new GUID (used by UIs)

Important API endpoints (Estimator)
- `POST /api/interactions/snapshot/pull` — request Takeoff snapshot for a project
- `POST /api/interactions/condition-changed` — receive single-condition callback from Takeoff
- `POST /api/interactions/condition-deleted` — receive delete callback

Contracts (important)
- `Condition` exposes `Measurements : List<Measurement>`.
- `Measurement` has:
  - `MeasurementName` (string)
  - `UnitsOfMeasurements` (string)
  - `Value` (double)

Estimator merge behavior
- When a `Condition` callback arrives at Estimator, Estimator replaces the `Measurements` on the existing condition with the set received from Takeoff (matching by `MeasurementName`). Measurements not present in the incoming payload are removed.

UI notes
- UIs are implemented with jQuery and `jsTree` and are served from each API's `wwwroot/index.html`.
- Project selection dropdowns are available; UIs can generate project GUIDs via the `POST /api/demo/guids` endpoint.

Developer tips
- If browser blocks local HTTPS, trust dev certs: `dotnet dev-certs https --trust`.
- Ports are configured in `Properties/launchSettings.json` for each API.

If you want, I can add examples of curl commands, Docker compose, or a developer script to run both services concurrently.