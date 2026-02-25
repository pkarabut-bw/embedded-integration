# Takeoff–Estimator Integration API Specification

## 1. Overview

This document specifies the integration contracts and interaction protocols between the **Takeoff** service and the **Estimator** service. These two services communicate over HTTP using JSON payloads. Takeoff is the authoritative source for condition data and quantity summaries. Estimator is a consumer that receives data from Takeoff and displays it.

### Key Principles

- **Takeoff is the single source of truth** for all condition data and computed summaries.
- **Estimator does not compute summaries.** It receives and stores them exactly as provided by Takeoff.
- Communication is bidirectional: Takeoff pushes change/deletion callbacks to Estimator; Estimator can pull snapshots from Takeoff.
- All endpoints use `application/json` with camelCase property naming.

---

## 2. Shared Contracts

All data exchanged between the services uses the following contract types, defined in the `Contracts` namespace.

### 2.1 Quantity

| Property | Type     | Description                          |
|----------|----------|--------------------------------------|
| `name`   | `string` | Name of the measured quantity        |
| `unit`   | `string` | Unit of measurement                  |
| `value`  | `double` | Numeric value                        |

### 2.2 TakeoffZone

| Property      | Type             | Description                              |
|---------------|------------------|------------------------------------------|
| `id`          | `Guid`           | Unique zone identifier                   |
| `zoneSummary` | `List<Quantity>` | Quantities measured within this zone     |

### 2.3 Page

| Property       | Type               | Description                              |
|----------------|---------------------|------------------------------------------|
| `id`           | `Guid`             | Unique page identifier                   |
| `pageNumber`   | `int`              | Page number within the document          |
| `pageSummary`  | `List<Quantity>`   | Aggregated quantities across all zones on this page |
| `takeoffZones` | `List<TakeoffZone>`| Zones on this page                       |

### 2.4 Document

| Property          | Type            | Description                              |
|-------------------|-----------------|------------------------------------------|
| `id`              | `Guid`          | Unique document identifier               |
| `documentSummary` | `List<Quantity>`| Aggregated quantities across all pages   |
| `pages`           | `List<Page>`    | Pages within the document                |

### 2.5 Condition

| Property         | Type             | Description                              |
|------------------|------------------|------------------------------------------|
| `id`             | `Guid`           | Unique condition identifier              |
| `projectId`      | `Guid`           | Project this condition belongs to        |
| `projectSummary` | `List<Quantity>` | Aggregated quantities across all documents |
| `documents`      | `List<Document>` | Documents within the condition           |

### 2.6 Data Hierarchy

```
Condition
??? ProjectSummary (aggregated from documents)
??? Documents[]
    ??? DocumentSummary (aggregated from pages)
    ??? Pages[]
        ??? PageSummary (aggregated from zones)
        ??? TakeoffZones[]
            ??? ZoneSummary (raw quantities)
```

### 2.7 Summary Aggregation Rules

Summaries are computed bottom-up by Takeoff:

1. **PageSummary** = sum of all `ZoneSummary` quantities across zones on the page, grouped by `(Name, Unit)`.
2. **DocumentSummary** = sum of all `PageSummary` quantities across pages in the document, grouped by `(Name, Unit)`.
3. **ProjectSummary** = sum of all `DocumentSummary` quantities across documents in the condition, grouped by `(Name, Unit)`.

Estimator **never** recomputes these summaries. It trusts the values provided by Takeoff.

---

## 3. Takeoff ? Estimator Callbacks (Push)

Takeoff sends notifications to Estimator whenever data changes. These are fire-and-forget calls — Takeoff does not wait for a successful response before returning to its own caller.

### 3.1 Condition Changed

Sent when a condition is created or updated (including any changes to zones, pages, or documents within it).

| Property | Value |
|----------|-------|
| **Method** | `POST` |
| **Path** | `/api/interactions/condition-changed` |
| **Request Body** | `List<Condition>` — full condition objects with all summaries recomputed |
| **Success Response** | `200 OK` with `List<Condition>` — the merged result |

**Behavior on Estimator side:**
- If the condition does not exist, insert it.
- If the condition exists, merge documents, pages, and zones by ID.
- Always overwrite the condition's `ProjectSummary` from the callback payload.

### 3.2 Condition Deleted

| Property | Value |
|----------|-------|
| **Method** | `POST` |
| **Path** | `/api/interactions/condition-deleted` |
| **Request Body** | `{ "projectId": Guid, "conditionId": Guid }` |
| **Success Response** | `204 No Content` |

**Behavior on Estimator side:**
- Delete the condition from local store.
- Pull a fresh snapshot of the project from Takeoff to ensure consistency.

### 3.3 Document Deleted

| Property | Value |
|----------|-------|
| **Method** | `POST` |
| **Path** | `/api/interactions/document-deleted` |
| **Request Body** | `{ "projectId": Guid, "documentId": Guid }` |
| **Success Response** | `204 No Content` |

**Behavior on Estimator side:**
- Delete the document (by ID) from all conditions within the project.
- Pull a fresh snapshot of the project from Takeoff to ensure consistency.

### 3.4 Page Deleted

| Property | Value |
|----------|-------|
| **Method** | `POST` |
| **Path** | `/api/interactions/page-deleted` |
| **Request Body** | `{ "projectId": Guid, "pageId": Guid }` |
| **Success Response** | `204 No Content` |

**Behavior on Estimator side:**
- Delete the page (by ID) from all documents in all conditions within the project.
- Pull a fresh snapshot of the project from Takeoff to ensure consistency.

### 3.5 Takeoff Zone Deleted

| Property | Value |
|----------|-------|
| **Method** | `POST` |
| **Path** | `/api/interactions/takeoffzone-deleted` |
| **Request Body** | `{ "projectId": Guid, "zoneId": Guid }` |
| **Success Response** | `204 No Content` |

**Behavior on Estimator side:**
- Delete the zone (by ID) from all pages in all documents in all conditions within the project.
- Pull a fresh snapshot of the project from Takeoff to ensure consistency.

### 3.6 Deletion Callback Design Notes

- Deletion payloads are intentionally flat: only `projectId` and the target entity ID are required. The hierarchy path (conditionId, documentId, etc.) is not needed because Estimator searches by ID across all parent entities.
- After handling a deletion locally, Estimator pulls the full project snapshot from Takeoff. This ensures that recalculated summaries (which Estimator cannot compute) are up to date.

---

## 4. Estimator ? Takeoff (Pull)

Estimator can query Takeoff's interaction API to retrieve condition data.

### 4.1 Get Conditions for a Project

| Property | Value |
|----------|-------|
| **Method** | `GET` |
| **Path** | `/api/interactions/projects/{projectId}/conditions` |
| **Response** | `200 OK` with `List<Condition>` |

Returns all conditions for the given project, with all summaries already computed.

---

## 5. Health Check

Both services expose a health endpoint.

| Property | Value |
|----------|-------|
| **Method** | `GET` |
| **Path** | `/api/interactions/health` |
| **Response** | `200 OK` with body `"ok"` |

---

## 6. Configuration

Each service requires the base URL of the other service.

**Takeoff** configuration (`PeerServices` section):

| Key                  | Type     | Description                     | Default |
|----------------------|----------|---------------------------------|---------|
| `EstimatorBaseUrl`   | `string` | Base URL of Estimator service   | —       |
| `HttpTimeoutSeconds` | `int`    | HTTP client timeout in seconds  | `10`    |

**Estimator** configuration (`PeerServices` section):

| Key                  | Type     | Description                     | Default |
|----------------------|----------|---------------------------------|---------|
| `TakeoffBaseUrl`     | `string` | Base URL of Takeoff service     | —       |
| `HttpTimeoutSeconds` | `int`    | HTTP client timeout in seconds  | `10`    |

---

## 7. Error Handling

- Takeoff callbacks are fire-and-forget. If Estimator is unreachable, Takeoff logs a warning and continues.
- Estimator's post-deletion snapshot pull is best-effort. If Takeoff is unreachable during the pull, Estimator logs the error but still returns the deletion response successfully.
- All HTTP client errors are logged with structured logging (`ILogger`).
