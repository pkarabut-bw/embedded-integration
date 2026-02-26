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

### 2.2 ProjectConditionQuantities

| Property                      | Type                                    | Description                                |
|-------------------------------|-----------------------------------------|--------------------------------------------|
| `conditionId`                 | `Guid`                                  | Unique condition identifier                |
| `projectId`                   | `Guid`                                  | Project this condition belongs to          |
| `quantities`                  | `List<Quantity>`                        | Aggregated from documents (computed by Takeoff) |
| `documentConditionQuantities` | `List<DocumentConditionQuantities>`     | Documents within the condition             |

### 2.3 DocumentConditionQuantities

| Property                      | Type                                | Description                         |
|-------------------------------|-------------------------------------|-------------------------------------|
| `documentId`                  | `Guid`                              | Unique document identifier          |
| `quantities`                  | `List<Quantity>`                    | Aggregated from pages (computed by Takeoff) |
| `pageConditionQuantities`     | `List<PageConditionQuantities>`     | Pages within the document           |

### 2.4 PageConditionQuantities

| Property                         | Type                                        | Description                       |
|----------------------------------|--------------------------------------------|-----------------------------------|
| `pageId`                         | `Guid`                                      | Unique page identifier            |
| `pageNumber`                     | `int`                                       | Page number within the document   |
| `quantities`                     | `List<Quantity>`                            | Aggregated from zones (computed by Takeoff) |
| `takeoffZoneConditionQuantities` | `List<TakeoffZoneConditionQuantities>`      | Zones on this page                |

### 2.5 TakeoffZoneConditionQuantities

| Property         | Type             | Description                    |
|------------------|------------------|--------------------------------|
| `takeoffZoneId`  | `Guid`           | Unique zone identifier         |
| `quantities`     | `List<Quantity>` | Raw quantities for this zone   |

### 2.6 Data Hierarchy

```
ProjectConditionQuantities
├── Quantities (computed by Takeoff)
└── DocumentConditionQuantities[]
    ├── Quantities (computed by Takeoff)
    └── PageConditionQuantities[]
        ├── Quantities (computed by Takeoff)
        └── TakeoffZoneConditionQuantities[]
            └── Quantities (raw data)
```

---

## 3. Integration Endpoints

### 3.1 Conditions Changed

- **Method**: POST
- **Path**: `/api/interactions/conditions-changed`
- **Request Body**: `List<ProjectConditionQuantities>`
- **Response**: `200 OK` with `List<ProjectConditionQuantities>`

**Behavior**:
- Takeoff sends full condition on create, or diff on update (only changed branches).
- Estimator inserts new condition if absent, or merges by ID if exists.
- Estimator always overwrites its local quantities with values from callback.

---

### 3.2 Conditions Deleted

- **Method**: POST
- **Path**: `/api/interactions/conditions-deleted`
- **Request Body**: `{ "projectId": Guid, "conditionIds": List<Guid> }`
- **Response**: `204 No Content`

**Behavior**:
1. Estimator deletes all conditions locally.
2. Estimator pulls fresh project snapshot from Takeoff.
3. Estimator replaces all local data for that project.

---

### 3.3 Documents Deleted

- **Method**: POST
- **Path**: `/api/interactions/documents-deleted`
- **Request Body**: `{ "projectId": Guid, "documentIds": List<Guid> }`
- **Response**: `204 No Content`

**Behavior**:
1. Estimator deletes all documents locally.
2. Estimator pulls fresh project snapshot from Takeoff.

---

### 3.4 Pages Deleted

- **Method**: POST
- **Path**: `/api/interactions/pages-deleted`
- **Request Body**: `{ "projectId": Guid, "pageIds": List<Guid> }`
- **Response**: `204 No Content`

**Behavior**:
1. Estimator deletes all pages locally.
2. Estimator pulls fresh project snapshot from Takeoff.

---

### 3.5 TakeoffZones Deleted

- **Method**: POST
- **Path**: `/api/interactions/takeoffzones-deleted`
- **Request Body**: `{ "projectId": Guid, "zoneIds": List<Guid> }`
- **Response**: `204 No Content`

**Behavior**:
1. Estimator deletes all zones locally.
2. Estimator pulls fresh project snapshot from Takeoff.

---

### 3.6 Project Deleted

- **Method**: POST
- **Path**: `/api/interactions/project-deleted`
- **Request Body**: `{ "projectId": Guid }`
- **Response**: `204 No Content`

**Behavior**:
- Estimator deletes the entire project locally.
- No snapshot pull (entire project is removed).

---

### 3.7 Get Conditions for a Project

- **Method**: GET
- **Path**: `/api/interactions/projects/{projectId}/conditions-all`
- **Response**: `200 OK` with `List<ProjectConditionQuantities>`

Returns all conditions for the given project, with summaries already computed by Takeoff.

---

## 4. Post-Deletion Synchronization

After any deletion (except project deletion), Estimator automatically pulls a fresh project snapshot from Takeoff using the **Get Conditions for a Project** endpoint. This ensures:

- Parent summaries are recalculated correctly by Takeoff
- Local data remains consistent with authoritative state
- No manual synchronization required

---

## 5. Communication Protocol

- **Transport**: HTTP/HTTPS
- **Serialization**: JSON with camelCase property naming
- **Error Handling**: 
  - Callbacks are fire-and-forget (Takeoff logs warnings if Estimator unreachable)
  - Snapshot pulls are best-effort (Estimator logs errors but continues)
  - All errors logged with structured logging
