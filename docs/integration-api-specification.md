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
- **KEY CHANGE**: Deletion callbacks now trigger **automatic snapshot pulls** to Estimator to ensure consistency.

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

### 2.7 Summary Aggregation Rules (Takeoff Only)

Summaries are computed **bottom-up by Takeoff**:

1. **PageSummary** = sum of all ZoneSummary quantities across zones, grouped by `(Name, Unit)`.
2. **DocumentSummary** = sum of all PageSummary quantities across pages, grouped by `(Name, Unit)`.
3. **ProjectSummary** = sum of all DocumentSummary quantities across documents, grouped by `(Name, Unit)`.

**Estimator trusts summaries from Takeoff and never recomputes them.**

---

## 3. Takeoff → Estimator Callbacks (Push)

### 3.1 Conditions Changed

- **Method**: POST
- **Path**: `/api/interactions/conditions-changed`
- **Request Body**: `List<ProjectConditionQuantities>`
- **Response**: `200 OK` with `List<ProjectConditionQuantities>`

**KEY CHANGE**: Endpoint renamed from `/condition-changed` to `/conditions-changed`.

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

**KEY CHANGE**: 
- Renamed from `/condition-deleted` to `/conditions-deleted`
- **Now accepts lists of IDs** instead of single ID
- **Estimator pulls snapshot after deletion**

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

**KEY CHANGE**:
- Renamed from `/document-deleted` to `/documents-deleted`
- **Now accepts list of document IDs**
- **Estimator pulls snapshot after deletion**

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

**KEY CHANGE**:
- Renamed from `/page-deleted` to `/pages-deleted`
- **Now accepts list of page IDs**
- **Estimator pulls snapshot after deletion**

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

**KEY CHANGE**:
- Renamed from `/takeoffzone-deleted` to `/takeoffzones-deleted`
- **Now accepts list of zone IDs**
- **Estimator pulls snapshot after deletion**

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

**NEW ENDPOINT**

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

### 4.1 Get Conditions for a Project

- **Method**: GET
- **Path**: `/api/interactions/projects/{projectId}/conditions-all`
- **Response**: `200 OK` with `List<ProjectConditionQuantities>`

Returns all conditions for a project with summaries computed by Takeoff.

**Used by**: Post-deletion snapshot sync.

---

### 4.2 Get All Project IDs

- **Method**: GET
- **Path**: `/api/demo/projects`
- **Response**: `200 OK` with `List<Guid>`

Returns all project IDs from Takeoff.

**EXCEPTION**: Only Demo API endpoint called by Estimator's pull flow.

---

## 5. Health Check

- **Method**: GET
- **Path**: `/api/interactions/health`
- **Response**: `200 OK` with `"ok"`

---

## 6. Configuration

| Service | Key | Value |
|---------|-----|-------|
| Takeoff | `PeerServices:EstimatorBaseUrl` | `https://localhost:5002/` |
| Takeoff | `PeerServices:HttpTimeoutSeconds` | `10` |
| Estimator | `PeerServices:TakeoffBaseUrl` | `https://localhost:5001/` |
| Estimator | `PeerServices:HttpTimeoutSeconds` | `10` |

---

## 7. Error Handling

- **Callbacks are fire-and-forget**: Takeoff logs warnings if Estimator unreachable.
- **Post-deletion pulls are best-effort**: Estimator returns success regardless of pull outcome.
- All errors logged with `ILogger`.

---

## 8. Summary of Key Changes

| Feature | Previous | Current |
|---------|----------|---------|
| **Condition Changed** | `/condition-changed` | `/conditions-changed` |
| **Condition Deleted** | `/condition-deleted` (single ID) | `/conditions-deleted` (**list of IDs**) |
| **Document Deleted** | `/document-deleted` (single ID) | `/documents-deleted` (**list of IDs**) |
| **Page Deleted** | `/page-deleted` (single ID) | `/pages-deleted` (**list of IDs**) |
| **Zone Deleted** | `/takeoffzone-deleted` (single ID) | `/takeoffzones-deleted` (**list of IDs**) |
| **Project Deleted** | ❌ Not defined | ✅ `/project-deleted` (**NEW**) |
| **Post-Deletion Sync** | Not automatic | ✅ **Automatic snapshot pull** (except project deletion) |
