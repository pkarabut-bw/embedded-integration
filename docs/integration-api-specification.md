# Takeoff–Estimator Integration API Specification

## 1. Overview

This document specifies the integration contracts and interaction protocols between the **Takeoff** service and the **Estimator** service. These two services communicate over HTTP using JSON payloads. Takeoff is the authoritative source for condition data and quantity summaries. Estimator is a consumer that receives data from Takeoff and maintains a local copy.

### Key Principles

- **Takeoff** is the single source of truth for all condition data and computed summaries.
- **Estimator** does not compute summaries. It receives and stores them exactly as provided by Takeoff.
- Communication is **bidirectional**: 
  - Takeoff **pushes** change/deletion callbacks to Estimator (fire-and-forget)
  - Estimator **pulls** snapshots from Takeoff on demand or after deletions
- All endpoints use `application/json` with camelCase property naming.
- Deletion callbacks trigger **automatic snapshot pulls** to Estimator to ensure consistency.

---

## 2. Shared Contracts

All data exchanged between the services uses the following contract types, defined in the `Contracts` namespace.

### 2.1 Quantity

| Property | Type     | Description                          |
|----------|----------|--------------------------------------|
| `name`   | `string` | Name of the measured quantity        |
| `unit`   | `string` | Unit of measurement                  |
| `value`  | `double` | Numeric value                        |

### 2.2 ProjectConditionQuantities (Condition)

| Property                      | Type                                    | Description                                |
|-------------------------------|-----------------------------------------|--------------------------------------------|
| `conditionId`                 | `Guid`                                  | Unique condition identifier                |
| `projectId`                   | `Guid`                                  | Project this condition belongs to          |
| `quantities` (ProjectSummary) | `List<Quantity>`                        | Aggregated from documents                  |
| `documentConditionQuantities` | `List<DocumentConditionQuantities>`     | Documents within the condition             |

### 2.3 DocumentConditionQuantities (Document)

| Property                      | Type                                | Description                         |
|-------------------------------|-------------------------------------|-------------------------------------|
| `documentId`                  | `Guid`                              | Unique document identifier          |
| `quantities` (DocumentSummary)| `List<Quantity>`                    | Aggregated from pages               |
| `pageConditionQuantities`     | `List<PageConditionQuantities>`     | Pages within the document           |

### 2.4 PageConditionQuantities (Page)

| Property                         | Type                                        | Description                       |
|----------------------------------|--------------------------------------------|-----------------------------------|
| `pageId`                         | `Guid`                                      | Unique page identifier            |
| `pageNumber`                     | `int`                                       | Page number within the document   |
| `quantities` (PageSummary)       | `List<Quantity>`                            | Aggregated from zones             |
| `takeoffZoneConditionQuantities` | `List<TakeoffZoneConditionQuantities>`      | Zones on this page                |

### 2.5 TakeoffZoneConditionQuantities (Zone)

| Property      | Type             | Description                    |
|---------------|------------------|--------------------------------|
| `takeoffZoneId` | `Guid`         | Unique zone identifier         |
| `quantities`  | `List<Quantity>` | Raw quantities for this zone   |

### 2.6 Data Hierarchy

```
ProjectConditionQuantities (Condition)
├── Quantities (ProjectSummary - computed by Takeoff)
└── DocumentConditionQuantities[]
    ├── Quantities (DocumentSummary - computed by Takeoff)
    └── PageConditionQuantities[]
        ├── Quantities (PageSummary - computed by Takeoff)
        └── TakeoffZoneConditionQuantities[]
            └── Quantities (ZoneSummary - raw data)
```

---

## 3. Takeoff → Estimator Callbacks (Push)

### 3.1 Conditions Changed

- **Method**: POST
- **Path**: `/api/interactions/conditions-changed`
- **Request Body**: `List<ProjectConditionQuantities>`
- **Response**: `200 OK` with `List<ProjectConditionQuantities>`

**Takeoff**:
- Sends full condition on create.
- Sends diff on update (only changed branches).

**Estimator**:
- Inserts new condition if absent.
- Merges documents, pages, zones by ID if condition exists.
- **Always overwrites ProjectSummary** from callback.

---

### 3.2 Conditions Deleted

- **Method**: POST
- **Path**: `/api/interactions/conditions-deleted`
- **Request Body**: `{ "projectId": Guid, "conditionIds": List<Guid> }`
- **Response**: `204 No Content`

**Estimator**:
1. Delete all conditions locally.
2. **Pull fresh project snapshot** from Takeoff.
3. Return success.

---

### 3.3 Documents Deleted

- **Method**: POST
- **Path**: `/api/interactions/documents-deleted`
- **Request Body**: `{ "projectId": Guid, "documentIds": List<Guid> }`
- **Response**: `204 No Content`

**Estimator**:
1. Delete all documents locally.
2. **Pull fresh project snapshot** from Takeoff to reconcile recalculated summaries.
3. Return success.

---

### 3.4 Pages Deleted

- **Method**: POST
- **Path**: `/api/interactions/pages-deleted`
- **Request Body**: `{ "projectId": Guid, "pageIds": List<Guid> }`
- **Response**: `204 No Content`

**Estimator**:
1. Delete all pages locally.
2. **Pull fresh project snapshot** from Takeoff.
3. Return success.

---

### 3.5 TakeoffZones Deleted

- **Method**: POST
- **Path**: `/api/interactions/takeoffzones-deleted`
- **Request Body**: `{ "projectId": Guid, "zoneIds": List<Guid> }`
- **Response**: `204 No Content`

**Estimator**:
1. Delete all zones locally.
2. **Pull fresh project snapshot** from Takeoff.
3. Return success.

---

### 3.6 Project Deleted

- **Method**: POST
- **Path**: `/api/interactions/project-deleted`
- **Request Body**: `{ "projectId": Guid }`
- **Response**: `204 No Content`

**Estimator**:
- Delete the entire project locally.
- **No snapshot pull** (entire project is removed).

---

### 3.7 Deletion Pattern: Automatic Snapshot Pull

**After any deletion (except project deletion), Estimator**:

1. Deletes the entity/entities locally
2. **Immediately pulls the full project snapshot** from Takeoff
3. Replaces all local data for that project with the fresh snapshot

**Why**: Ensures parent summaries (Document, Page, Project) are recalculated correctly by Takeoff and match exactly.

---

## 4. Estimator → Takeoff (Pull)

Estimator can query Takeoff's API to retrieve condition data.

### 4.1 Get Conditions for a Project (Single Project Snapshot)

- **Method**: GET
- **Path**: `/api/interactions/projects/{projectId}/conditions-all`
- **Response**: `200 OK` with `List<ProjectConditionQuantities>`

Returns all conditions for the given project, with summaries already computed by Takeoff.

**Used by**:
- Estimator after handling deletion callbacks to pull fresh project snapshot.
- Initial snapshot pull when needed.

---

## 5. Health Check

Both services expose a health endpoint.

- **Method**: GET
- **Path**: `/api/interactions/health`
- **Response**: `200 OK` with `"ok"`

---

## 6. Configuration

Each service requires the base URL of the other service (configured under `PeerServices`).

**Takeoff configuration keys**:
- `EstimatorBaseUrl` (string): Base URL of Estimator service
- `HttpTimeoutSeconds` (int): HTTP client timeout in seconds (default 10)

**Estimator configuration keys**:
- `TakeoffBaseUrl` (string): Base URL of Takeoff service
- `HttpTimeoutSeconds` (int): HTTP client timeout in seconds (default 10)

---

## 7. Error Handling

- **Takeoff callbacks are fire-and-forget**: If Estimator is unreachable, Takeoff logs a warning and continues.
- **Estimator's post-deletion snapshot pull is best-effort**: If Takeoff is unreachable during the pull, Estimator logs the error but still returns the deletion response successfully.
- All HTTP client errors are logged with structured logging (`ILogger`).
