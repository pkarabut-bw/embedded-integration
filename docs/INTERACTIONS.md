# Interactions API (Takeoff & Estimator)

This document collects the integration endpoints exposed by both services for cross-service interactions (callbacks and snapshot reads). It documents API signatures and the contracts they use. Health endpoints are intentionally excluded.

---

## Common contracts
Both endpoints reference the shared contracts from the `Contracts` project. Key shapes used by these integration endpoints:

`Condition` (JSON)
```json
{
  "id": "{guid}",
  "projectId": "{guid}",
  "measurements": [
    {
      "measurementName": "string",
      "unitsOfMeasurements": "string",
      "value": 0.0
    }
  ]
}
```

`Measurement` (JSON)
```json
{
  "measurementName": "string",
  "unitsOfMeasurements": "string",
  "value": 0.0
}
```

Contracts referenced:
- `Contracts.Condition` (Id, ProjectId, Measurements)
- `Contracts.Measurement` (MeasurementName, UnitsOfMeasurements, Value)

---

## Takeoff (Takeoff.Api) — InteractionsController

Endpoint documented here is intended for programmatic consumption by peers (Estimator). It returns the current in-memory snapshot of `Condition` objects for a project.

### GET /api/interactions/projects/{projectId}/conditions
- Purpose: Return the current list of `Condition` instances stored in Takeoff for the given `projectId`.
- Method: `GET`
- Path parameters:
  - `projectId` (GUID)
- Response: `200 OK` with JSON array of `Condition` objects (see contract above).
- Used contracts:
  - `Contracts.Condition`
  - `Contracts.Measurement`
- Notes: The Takeoff UI loads local data using `GET /api/demo/projects/{projectId}/conditions`. This interactions endpoint is provided for cross-service reads.

---

## Estimator (Estimator.Api) — InteractionsController

These endpoints are callbacks and deletion notifications that Takeoff calls to keep Estimator in sync.

### POST /api/interactions/condition-changed
- Purpose: Receive a single `Condition` update (callback) from Takeoff and merge it into Estimator state.
- Method: `POST`
- Request body: `Condition` (application/json)
- Response: `200 OK` with the merged `Condition` JSON representing the stored condition after merge.
- Used contracts:
  - `Contracts.Condition`
  - `Contracts.Measurement`
- Merge semantics (current implementation):
  - Measurements are matched by `MeasurementName` (case-insensitive lookup).
  - For each incoming measurement, Estimator updates or adds the measurement value/units.
  - After processing, Estimator replaces the stored condition's `Measurements` collection with the incoming set (measurements not present in the incoming payload are removed).
- Notes: The caller should provide the desired post-update set when relying on the "replace missing" semantics.

### POST /api/interactions/condition-deleted
- Purpose: Notify Estimator to delete a condition by id.
- Method: `POST`
- Request body: JSON object with `projectId` and `conditionId`:
```json
{
  "projectId": "{guid}",
  "conditionId": "{guid}"
}
```
- Responses:
  - `204 No Content` — condition existed and was removed
  - `404 Not Found` — condition for given id not found
  - `400 Bad Request` — invalid request (empty GUIDs)
- Used contracts: this endpoint references `Condition` by id (no condition payload).

---

## Notes and compatibility
- All JSON payloads use camelCase property names when serialized via `System.Text.Json` (the projects configure camelCase naming).
- If you need backward compatibility with older clients using different endpoints, consider adding alias endpoints that forward to the documented routes.

---

Generated: merged documentation for Interactions endpoints (Takeoff and Estimator).