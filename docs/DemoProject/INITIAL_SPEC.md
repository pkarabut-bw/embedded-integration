# Sample Project Specification

This repository contains two web applications that interact using a shared `Contracts` library:

- `Takeoff.Api` — owns `TakeoffData` and provides a jQuery UI for CRUD on `Condition` objects.
- `Estimator.Api` — owns `EstimatorData` and can pull snapshots from Takeoff or receive callbacks.

Both projects:
- Target .NET 9
- Use controllers-based Web APIs
- Host small jQuery-based UIs in `wwwroot`
- Store peer base URLs in configuration for inter-service HTTP calls
- Store data in-memory via singleton services (no database)

## Data model
Shared DTOs live in the `Contracts` project. Important types:

`Condition`
```csharp
public class Condition
{
  public Guid Id { get; set; }
  public Guid ProjectId { get; set; }
  public List<Measurement> Measurements { get; set; }
}
```

`Measurement`
```csharp
public class Measurement
{
  public string MeasurementName { get; set; }
  public string UnitsOfMeasurements { get; set; }
  public double Value { get; set; }
}
```

## Interactions summary
1. Estimator can request a full list of `Condition` instances from Takeoff (snapshot) by `ProjectId`.
   The returned list replaces Estimator's in-memory list for that project.
2. Takeoff sends single `Condition` callbacks to Estimator when a condition changes.
   Estimator merges the incoming `Measurements` into the stored condition:
   measurements are matched by `MeasurementName` and Estimator replaces the stored measurements with the incoming set
   (measurements not present in the incoming payload are removed).
3. Takeoff can instruct Estimator to delete a `Condition` by id.

## Controllers & Routes
- `DemoController` — local/demo UI endpoints (CRUD, GUID generation). On Estimator it also exposes the snapshot-pull action.
  - Takeoff (example):
    - `GET  /api/demo/projects` — list project ids
    - `GET  /api/demo/projects/{projectId}/conditions` — list conditions for a project (snapshot)
    - `GET  /api/demo/projects/{projectId}/conditions/{conditionId}` — single condition
    - `POST /api/demo/conditions` — create condition
    - `PUT  /api/demo/conditions/{conditionId}` — update condition
    - `DELETE /api/demo/projects/{projectId}/conditions/{conditionId}` — delete condition
    - `POST /api/demo/guids` — generate new GUID
  - Estimator (additional):
    - `POST /api/demo/snapshot/pull` — Estimator triggers snapshot pull from Takeoff and stores it

- `InteractionsController` — cross-service endpoints and health
  - Takeoff:
    - `GET  /api/interactions/projects/{projectId}/conditions` — optional programmatic snapshot read
    - `GET  /api/interactions/health`
  - Estimator:
    - `POST /api/interactions/condition-changed` — callback to merge single condition
    - `POST /api/interactions/condition-deleted` — notify deletion
    - `GET  /api/interactions/health`

## UI requirements
- Use jQuery + jsTree + Bootstrap
- Takeoff UI: full-screen tree + inline form editor (create/edit/delete)
- Estimator UI: read-only tree + "Get All Conditions from Takeoff" button
- Client JSON uses camelCase property names (System.Text.Json default configuration)

## Notes
- GUIDs are generated server-side via `POST /api/demo/guids` and not typed by users.
- Estimator merge semantics remove measurements not present in incoming callback; callers should include desired post-update set when necessary.

---

Date: 2026-02-10
