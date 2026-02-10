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
  - `DemoController.cs`
  - `InteractionsController.cs`
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
    public List<MeasurementMetadata> Metadata { get; set; }
    public List<MeasurementValue> MeasurementValues { get; set; }
}
```

**Invariants**
- `Id` is immutable after creation.
- `ProjectId` groups conditions; snapshot sync is always **by `ProjectId`**.
- `Metadata` is the full set of possible measurements for the condition.
- `MeasurementValues`:
  - In **snapshot**: contains all possible measurements.
  - In **callback/incremental update**: can contain only changed measurements.

### 2.2 `MeasurementMetadata`
File: `Contracts/MeasurementMetadata.cs`
```csharp
public class MeasurementMetadata
{
    public Guid MeasurementId { get; set; }
    public string MeasurementName { get; set; }
    public string UnitsOfMeasurements { get; set; }
}
```

### 2.3 `MeasurementValue`
File: `Contracts/MeasurementValue.cs`
```csharp
public class MeasurementValue
{
    public Guid MeasurementId { get; set; }
    public double Value { get; set; }
}
```

### 2.4 Serialization
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

## 4. Takeoff backend (`Takeoff.Api`)

### 4.1 Responsibilities
- Owns `TakeoffData` (in memory).
- Supports CRUD operations over `Condition`.
- After each **user-initiated** change (create/update), sends the changed `Condition` to Estimator.
- After each **user-initiated** delete, sends delete command to Estimator.

### 4.2 Services
#### 4.2.1 `TakeoffDataStore` (singleton)
- Stores: `Dictionary<Guid /*ProjectId*/, List<Condition>>`

Methods (thread-safe):
- `IReadOnlyList<Condition> GetAll(Guid projectId)`
- `Condition? Get(Guid projectId, Guid conditionId)`
- `Condition Add(Condition condition)`
- `Condition Update(Condition condition)`
- `bool Delete(Guid projectId, Guid conditionId)`

Concurrency:
- Use a private lock (e.g., `private readonly object _gate = new();`).
- Return deep copies or treat DTOs as mutable but only mutated under lock.

#### 4.2.2 `EstimatorClient` (typed `HttpClient`)
Purpose: send callbacks to Estimator.

Methods:
- `Task SendConditionChangedAsync(Condition condition, CancellationToken ct)`
- `Task SendConditionDeletedAsync(Guid projectId, Guid conditionId, CancellationToken ct)`

HTTP:
- `POST {EstimatorBaseUrl}/api/interactions/condition-changed`
- `POST {EstimatorBaseUrl}/api/interactions/condition-deleted`

Payloads:
- For condition change: body = `Condition`.
- For delete: body = `{ projectId, conditionId }` (API-local DTO).

### 4.3 Controllers

#### 4.3.1 `DemoController` (Takeoff)
Route base: `api/demo`

Endpoints:
1. `GET api/demo/projects/{projectId:guid}/conditions`
   - Returns: `List<Condition>` (snapshot for that project).

2. `GET api/demo/projects/{projectId:guid}/conditions/{conditionId:guid}`
   - Returns: `Condition` or `404`.

3. `POST api/demo/conditions`
   - Body: `Condition`
   - Server behavior:
     - If `condition.Id == Guid.Empty`, generate new `Id`.
     - If `condition.ProjectId == Guid.Empty`, reject (`400`).
     - Ensure `Metadata` and `MeasurementValues` are non-null lists.
   - Stores into `TakeoffDataStore`.
   - Invokes `EstimatorClient.SendConditionChangedAsync(createdCondition)`.
   - Returns: created `Condition`.

4. `PUT api/demo/conditions/{conditionId:guid}`
   - Body: `Condition`
   - Server behavior:
     - Ensure route `conditionId` matches body `Id` (or overwrite body `Id`).
     - Update in `TakeoffDataStore`.
   - Invokes `EstimatorClient.SendConditionChangedAsync(updatedCondition)`.
   - Returns: updated `Condition`.

5. `DELETE api/demo/projects/{projectId:guid}/conditions/{conditionId:guid}`
   - Deletes from `TakeoffDataStore`.
   - Invokes `EstimatorClient.SendConditionDeletedAsync(projectId, conditionId)`.
   - Returns: `204` if deleted, `404` if not found.

6. `POST api/demo/guids`
   - Returns: a new `Guid`.
   - Used by UI to generate GUIDs for `ProjectId` and nested `MeasurementId`s.


#### 4.3.2 `InteractionsController` (Takeoff)
Route base: `api/interactions`

Purpose: endpoints Takeoff needs for peer-to-peer interactions (optional in this direction).

Endpoints (optional but reserved):
- `GET api/interactions/health` -> `200 OK`

> Primary direction in requirements is Takeoff ? Estimator callbacks; Takeoff does not need to receive updates.

---

## 5. Estimator backend (`Estimator.Api`)

### 5.1 Responsibilities
- Owns `EstimatorData` (in memory).
- Supports:
  1) Pull full list of conditions from Takeoff **by project id** (snapshot sync).
  2) Receive incremental updates from Takeoff (condition changed callback).
  3) Receive delete commands from Takeoff.
- Exposes read-only API for UI.

### 5.2 Services

#### 5.2.1 `EstimatorDataStore` (singleton)
Stores: `Dictionary<Guid /*ProjectId*/, List<Condition>>`

Methods:
- `IReadOnlyList<Condition> GetAll(Guid projectId)`
- `Condition? Get(Guid projectId, Guid conditionId)`
- `void ReplaceAll(Guid projectId, List<Condition> snapshot)`
- `Condition UpsertByCallback(Condition changedCondition)`
- `bool Delete(Guid projectId, Guid conditionId)`

Concurrency:
- Same locking strategy as Takeoff.

#### 5.2.2 `TakeoffClient` (typed `HttpClient`)
Purpose: request snapshot.

Methods:
- `Task<List<Condition>> GetAllConditionsAsync(Guid projectId, CancellationToken ct)`

HTTP:
- `GET {TakeoffBaseUrl}/api/demo/projects/{projectId}/conditions`

### 5.3 Controllers

#### 5.3.1 `DemoController` (Estimator)
Route base: `api/demo`

Endpoints (read-only):
1. `GET api/demo/projects/{projectId:guid}/conditions`
2. `GET api/demo/projects/{projectId:guid}/conditions/{conditionId:guid}`

#### 5.3.2 `InteractionsController` (Estimator)
Route base: `api/interactions`

Endpoints:
1. `POST api/interactions/snapshot/pull`
   - Body: `{ projectId }`
   - Algorithm: call Takeoff snapshot endpoint; replace EstimatorData for that project.
   - Returns: `List<Condition>` (the resulting snapshot that is stored).

2. `POST api/interactions/condition-changed`
   - Body: `Condition` (incremental update)
   - Algorithm: merge into `EstimatorData` (see section 6.2).
   - Returns: merged `Condition`.

3. `POST api/interactions/condition-deleted`
   - Body: `{ projectId, conditionId }`
   - Algorithm: delete by id.
   - Returns: `204` if existed, `404` if not found.

4. `GET api/interactions/health`

---

## 6. Algorithms (exact behavior)

### 6.1 Snapshot sync (Estimator pulls from Takeoff)
Trigger: user clicks “Get All Conditions from Takeoff” in Estimator UI.

Input:
- `projectId`

Algorithm:
1. `Estimator.Api` receives `POST /api/interactions/snapshot/pull`.
2. Validate `projectId != Guid.Empty`, else `400`.
3. `TakeoffClient.GetAllConditionsAsync(projectId)`.
4. If Takeoff returns non-2xx, propagate as `502 BadGateway`.
5. `EstimatorDataStore.ReplaceAll(projectId, snapshot)`:
   - If no entry exists for `projectId`, create it.
   - Replace the list entirely with the snapshot list (no merge).
6. Return the stored snapshot.

Properties:
- Operation is idempotent: same snapshot produces same state.

### 6.2 Incremental condition update merge (Takeoff ? Estimator)
Trigger: Takeoff user creates/edits a condition.

Input:
- `changedCondition` (a `Condition` instance)

Algorithm in `EstimatorDataStore.UpsertByCallback(changedCondition)`:
1. Validate:
   - `changedCondition.Id != Guid.Empty`
   - `changedCondition.ProjectId != Guid.Empty`
   - `changedCondition.Metadata != null` (if null, treat as empty list)
   - `changedCondition.MeasurementValues != null` (if null, treat as empty list)
2. Locate project list: `list = _data[projectId]` (create if missing).
3. Find existing condition by `Id`.
4. If not found:
   - Add `changedCondition` to the list.
   - Return `changedCondition`.
5. If found `current`:
   - Replace `current.Metadata` entirely with `changedCondition.Metadata`.
   - For each `mv` in `changedCondition.MeasurementValues`:
     - Find `existingMv` in `current.MeasurementValues` by `MeasurementId`.
     - If found: set `existingMv.Value = mv.Value`.
     - If not found: add `mv` (upsert semantics).
   - (Optional) Ensure `current.MeasurementValues` contains at least the set of `Metadata.MeasurementId` by adding missing values with default `0`.
   - Return `current`.

Properties:
- Idempotent per measurement value: repeating the same update yields same values.
- Supports partial updates (only changed measurements sent).

### 6.3 Delete condition (Takeoff ? Estimator)
Input:
- `projectId`, `conditionId`

Algorithm:
1. Validate both non-empty GUIDs.
2. Find condition list for project.
3. Remove first matching element by `Id`.
4. Return `204` if removed, else `404`.

### 6.4 Takeoff CRUD (user initiated)
**Create**
1. UI posts condition to `Takeoff.Api`.
2. Takeoff stores it.
3. Takeoff calls Estimator callback (`condition-changed`).

**Update**
1. UI PUTs condition.
2. Takeoff stores it.
3. Takeoff calls Estimator callback (`condition-changed`).

**Delete**
1. UI issues DELETE.
2. Takeoff removes it.
3. Takeoff calls Estimator callback (`condition-deleted`).

Error handling:
- If callback to Estimator fails:
  - Return `202 Accepted` (optional) or still return `200` and include `syncStatus` field in a UI-only response model.
  - Minimum: log the failure using `ILogger`.

---

## 7. UI – Common requirements
- Use forms (inline panels) and input controls, **no modal dialogs**.
- Use jQuery to call APIs via AJAX.
- Use an open-source jQuery TreeView control.

### 7.1 Selected frontend libraries (exact)
Use CDN references (or local copies in `wwwroot/lib/`):
- `jquery` **3.7.1**
  - `https://cdn.jsdelivr.net/npm/jquery@3.7.1/dist/jquery.min.js`
- `jstree` **3.3.16** (TreeView)
  - `https://cdn.jsdelivr.net/npm/jstree@3.3.16/dist/jstree.min.js`
  - `https://cdn.jsdelivr.net/npm/jstree@3.3.16/dist/themes/default/style.min.css`
- `bootstrap` **5.3.3** (layout + form styling)
  - `https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css`
  - `https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js`

Optional (only if needed):
- `lodash` 4.17.21 for deep cloning and helpers.

### 7.2 Tree rendering model
Tree nodes represent:
- Project
  - Condition
    - `Id`
    - `ProjectId`
    - `Metadata`
      - Metadata item
        - `MeasurementId`, `MeasurementName`, `UnitsOfMeasurements`
    - `MeasurementValues`
      - Value item
        - `MeasurementId`, `Value`

Tree behavior:
- Expand / collapse.
- In Takeoff: selecting a `Condition` node loads it into the edit form.
- In Estimator: read-only selection only.

---

## 8. UI – Takeoff

### 8.1 Page layout / appearance
Single page: `Takeoff.Api/wwwroot/index.html` (or `takeoff/index.html`).

Layout (full screen):
1. Top toolbar (fixed height, full width):
   - Left: buttons
     - `AddCondition` (primary)
     - `EditCondition`
     - `DeleteCondition` (danger)
   - Right: project selector (optional) + status text
     - `ProjectId` input (readonly or editable depending on testing needs)
     - `Load` button to reload tree
     - Status area: “Saved”, “Sync failed: …”, etc.

2. Main area (flex, fills remaining screen):
   - Left: TreeView (`jsTree`) taking ~60% width.
   - Right: Form panel taking ~40% width.

3. Form panel (no dialogs):
   - Shown as inline card.
   - Has two modes:
     - Create mode
     - Edit mode

### 8.2 Controls (detailed)
#### TreeView
- Element: `#takeoffTree`
- Uses `jsTree` with `themes`, `wholerow` plugins.
- Node icons:
  - Project: folder icon
  - Condition: file icon
  - Collections: list icon
  - Field leaves: dot icon

#### Buttons
- `#btnAddCondition`:
  - Clears form.
  - Requests GUID(s) from server if needed.
  - Switches form to create mode.

- `#btnEditCondition`:
  - Requires a condition selected.
  - Loads selected condition into form.
  - Switches form to edit mode.

- `#btnDeleteCondition`:
  - Requires a condition selected.
  - Calls DELETE endpoint.
  - On success: refresh tree.

#### Form fields (Condition)
- `Id` (readonly text)
- `ProjectId` (readonly text; can be chosen from top toolbar)

##### Metadata editor (repeatable rows)
Represented as a table:
- Columns: `MeasurementId (readonly)`, `MeasurementName`, `UnitsOfMeasurements`
- Buttons:
  - `Add Measurement` row
  - `Remove Measurement` row

Rules:
- `MeasurementId` generated via server GUID endpoint.
- `MeasurementName` required.

##### MeasurementValues editor (repeatable rows)
Also as a table:
- Columns: `MeasurementId (dropdown or readonly)`, `Value (number)`

Rules:
- `MeasurementId` must correspond to one of the metadata items.
- For each metadata entry, UI shows a value row.
- If metadata changes, regenerate the value rows (preserve matching values if possible).

Validation (client side):
- `ProjectId` required.
- `MeasurementName` required.
- Numeric `Value` uses HTML `input type="number" step="any"`.

### 8.3 UI-to-API bindings
- Load tree:
  - `GET /api/demo/projects/{projectId}/conditions`
- Create:
  - `POST /api/demo/conditions`
- Update:
  - `PUT /api/demo/conditions/{id}`
- Delete:
  - `DELETE /api/demo/projects/{projectId}/conditions/{id}`
- Generate GUID:
  - `POST /api/demo/guids`

Refresh rules:
- After create/update/delete: reload snapshot from Takeoff and rerender tree.

---

## 9. UI – Estimator

### 9.1 Page layout / appearance
Single page: `Estimator.Api/wwwroot/index.html` (or `estimator/index.html`).

Layout (full screen):
1. Top toolbar:
   - Button: `Get All Conditions from Takeoff` (primary)
   - Project selector / input: `ProjectId`
   - Status banner to show last sync time and errors.

2. Main area:
   - Full width TreeView (`#estimatorTree`) read-only.

### 9.2 Controls (detailed)
#### TreeView
- Element: `#estimatorTree`
- Uses `jsTree` (same configuration).
- Read-only:
  - Disable context menus.
  - No editing.

#### Sync button
- `#btnPullSnapshot`
- On click:
  - `POST /api/interactions/snapshot/pull` with `{ projectId }`
  - Update status banner.
  - Re-render tree.

### 9.3 UI-to-API bindings
- Pull snapshot:
  - `POST /api/interactions/snapshot/pull`
- Load current state (optional refresh without pulling):
  - `GET /api/demo/projects/{projectId}/conditions`

---

## 10. Backend libraries (NuGet)
Per API project:
- `Microsoft.NET.Sdk.Web` (ASP.NET Core)

No additional packages are required.

Optional packages (only if needed):
- `Swashbuckle.AspNetCore` for Swagger UI.

---

## 11. Non-functional requirements
- Logging: use `ILogger<T>` in controllers and HTTP clients.
- Timeouts: enforced via `HttpClient.Timeout` using configuration.
- Health endpoints: simple `GET` returning `200`.
- CORS: enable only if UIs are hosted separately; if served by the same origin, CORS is not needed.

---

## 12. Acceptance criteria
- Takeoff CRUD modifies only `TakeoffData`.
- After each Takeoff CRUD change, Estimator state is updated via callbacks.
- Estimator “Get All Conditions from Takeoff” replaces the in-memory list for a project.
- Both UIs show full object graphs in TreeView.
- No user manually types GUIDs; they are generated by server endpoint(s).
