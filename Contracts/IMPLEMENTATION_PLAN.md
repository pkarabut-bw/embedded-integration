# Step-by-step implementation plan for `DETAILED_SPEC.md`

This plan assumes the current solution only contains `Contracts/Contracts.csproj` and needs two additional ASP.NET Core Web API projects targeting **.NET 9**:
- `Takeoff.Api`
- `Estimator.Api`

The plan is intentionally explicit for backend + frontend developers.

---

## 1. Inspect current solution layout
1. Locate solution file (`*.sln`). If missing, create one at repo root (e.g., `EmbeddedIntegration.sln`).
2. Verify `Contracts` builds for `net9.0`.
3. Confirm there are no existing `Takeoff` / `Estimator` projects (avoid name collisions).

Deliverables:
- A clean baseline build.

---

## 2. Create `Takeoff.Api` project
1. Create a new ASP.NET Core Web API project targeting `net9.0`:
   - Project name: `Takeoff.Api`
   - Template: Web API (controllers enabled)
2. Ensure `Program.cs` uses controllers:
   - `builder.Services.AddControllers()`
   - `app.MapControllers()`
3. Enable static file hosting:
   - `app.UseStaticFiles()`
   - (optional) `app.UseDefaultFiles()`

Deliverables:
- `Takeoff.Api/Takeoff.Api.csproj`
- `Takeoff.Api/Program.cs` configured for controllers and `wwwroot`.

---

## 3. Create `Estimator.Api` project
Repeat Step 2 but with:
- Project name: `Estimator.Api`

Deliverables:
- `Estimator.Api/Estimator.Api.csproj`
- `Estimator.Api/Program.cs`

---

## 4. Add references and shared conventions
1. Add project reference from both API projects to `Contracts`:
   - `Takeoff.Api` -> `Contracts`
   - `Estimator.Api` -> `Contracts`
2. Configure JSON serialization options in both `Program.cs`:
   - camelCase naming
   - property name case-insensitive
3. (Optional but recommended) Add Swagger for both apps for manual testing.

Deliverables:
- Updated `.csproj` + `Program.cs` in both apps.

---

## 5. Implement configuration for peer URLs
In each API project:
1. Add `appsettings.json` keys:
   - `Takeoff.Api`: `PeerServices:EstimatorBaseUrl`, `PeerServices:HttpTimeoutSeconds`
   - `Estimator.Api`: `PeerServices:TakeoffBaseUrl`, `PeerServices:HttpTimeoutSeconds`
2. Create options classes:
   - `PeerServicesOptions` in each API project (can be same name, different namespace)
3. Wire options binding in `Program.cs` using `IOptions<PeerServicesOptions>`.

Deliverables:
- `Takeoff.Api/appsettings.json`
- `Estimator.Api/appsettings.json`
- `Takeoff.Api/Options/PeerServicesOptions.cs`
- `Estimator.Api/Options/PeerServicesOptions.cs`

---

## 6. Implement in-memory data stores (singleton services)

### 6.1 Takeoff: `TakeoffDataStore`
Location: `Takeoff.Api/Services/TakeoffDataStore.cs`

1. Implement backing storage:
   - `Dictionary<Guid, List<Condition>> _data`
   - `object _gate` lock
2. Implement methods:
   - `GetAll(projectId)`
   - `Get(projectId, conditionId)`
   - `Add(condition)`
   - `Update(condition)`
   - `Delete(projectId, conditionId)`
3. Ensure:
   - project-scoped storage
   - predictable behavior when project key does not exist

### 6.2 Estimator: `EstimatorDataStore`
Location: `Estimator.Api/Services/EstimatorDataStore.cs`

1. Implement backing storage same as Takeoff.
2. Implement methods:
   - `GetAll(projectId)`
   - `Get(projectId, conditionId)`
   - `ReplaceAll(projectId, snapshot)`
   - `UpsertByCallback(changedCondition)`
   - `Delete(projectId, conditionId)`
3. Implement merge algorithm exactly per `DETAILED_SPEC.md`:
   - replace `Metadata`
   - update/add measurement values by `MeasurementId`

Deliverables:
- Store services registered as singletons in both `Program.cs`.

---

## 7. Implement typed peer HTTP clients

### 7.1 Takeoff -> Estimator: `EstimatorClient`
Location: `Takeoff.Api/Services/EstimatorClient.cs`

1. Configure typed `HttpClient` with base address from `PeerServicesOptions`.
2. Implement:
   - `SendConditionChangedAsync(Condition condition)` -> `POST /api/interactions/condition-changed`
   - `SendConditionDeletedAsync(Guid projectId, Guid conditionId)` -> `POST /api/interactions/condition-deleted`
3. Create request DTO for delete:
   - `ConditionDeleteRequest { Guid ProjectId; Guid ConditionId; }`

### 7.2 Estimator -> Takeoff: `TakeoffClient`
Location: `Estimator.Api/Services/TakeoffClient.cs`

1. Configure typed `HttpClient` similarly.
2. Implement:
   - `GetAllConditionsAsync(Guid projectId)` -> `GET /api/demo/projects/{projectId}/conditions`

Deliverables:
- Typed clients registered via `AddHttpClient<TClient>()`.
- Basic logging and timeout handling.

---

## 8. Implement controllers

### 8.1 Takeoff controllers

#### 8.1.1 `DemoController`
Location: `Takeoff.Api/Controllers/DemoController.cs`
Routes base: `api/demo`

Implement endpoints:
1. `GET /api/demo/projects/{projectId}/conditions`
2. `GET /api/demo/projects/{projectId}/conditions/{conditionId}`
3. `POST /api/demo/conditions`
4. `PUT /api/demo/conditions/{conditionId}`
5. `DELETE /api/demo/projects/{projectId}/conditions/{conditionId}`
6. `POST /api/demo/guids`

Algorithm requirements:
- On create/update: call `EstimatorClient.SendConditionChangedAsync` after store update.
- On delete: call `EstimatorClient.SendConditionDeletedAsync` after store delete.
- Return appropriate 200/201/204/404/400.

#### 8.1.2 `InteractionsController`
Location: `Takeoff.Api/Controllers/InteractionsController.cs`
Routes base: `api/interactions`

Implement:
- `GET /api/interactions/health`


### 8.2 Estimator controllers

#### 8.2.1 `DemoController`
Location: `Estimator.Api/Controllers/DemoController.cs`
Routes base: `api/demo`

Implement endpoints (read-only):
1. `GET /api/demo/projects/{projectId}/conditions`
2. `GET /api/demo/projects/{projectId}/conditions/{conditionId}`

#### 8.2.2 `InteractionsController`
Location: `Estimator.Api/Controllers/InteractionsController.cs`
Routes base: `api/interactions`

Implement endpoints:
1. `POST /api/interactions/snapshot/pull` (body `{ projectId }`)
2. `POST /api/interactions/condition-changed` (body `Condition`)
3. `POST /api/interactions/condition-deleted` (body `{ projectId, conditionId }`)
4. `GET /api/interactions/health`

Algorithm requirements:
- `snapshot/pull` calls `TakeoffClient.GetAllConditionsAsync` then `EstimatorDataStore.ReplaceAll`.
- `condition-changed` invokes `EstimatorDataStore.UpsertByCallback`.
- `condition-deleted` invokes `EstimatorDataStore.Delete`.

Deliverables:
- All controllers compile and routes match the spec.

---

## 9. Add static UI assets

### 9.1 Common approach
- Use `wwwroot` for each API project.
- Reference libraries via CDN with pinned versions:
  - `jquery@3.7.1`
  - `jstree@3.3.16`
  - `bootstrap@5.3.3`


### 9.2 Takeoff UI
Location:
- `Takeoff.Api/wwwroot/index.html`
- `Takeoff.Api/wwwroot/css/site.css`
- `Takeoff.Api/wwwroot/js/site.js`

Implement:
1. Full-screen layout using Bootstrap flex:
   - Top toolbar with buttons: Add/Edit/Delete
   - Main area split: Tree (left) + Form (right)
2. TreeView:
   - Use `jsTree` to show `TakeoffData` graph
   - Selecting a condition populates the form
3. Forms (no dialogs):
   - Create/Edit modes
   - Metadata table with add/remove measurement rows
   - Measurement values table aligned to metadata
4. GUID handling:
   - Never allow typing GUIDs
   - Call `POST /api/demo/guids` to generate `Id`, `MeasurementId`, and optionally `ProjectId`
5. API calls:
   - Load: `GET /api/demo/projects/{projectId}/conditions`
   - Create: `POST /api/demo/conditions`
   - Update: `PUT /api/demo/conditions/{id}`
   - Delete: `DELETE /api/demo/projects/{projectId}/conditions/{id}`


### 9.3 Estimator UI
Location:
- `Estimator.Api/wwwroot/index.html`
- `Estimator.Api/wwwroot/css/site.css`
- `Estimator.Api/wwwroot/js/site.js`

Implement:
1. Top toolbar:
   - “Get All Conditions from Takeoff” button
   - ProjectId display/selector
   - Status banner
2. Read-only TreeView:
   - Uses `jsTree`
3. Sync behavior:
   - On button click: `POST /api/interactions/snapshot/pull` with `{ projectId }`
   - Reload tree from response

Deliverables:
- Both apps serve UI at `/`.

---

## 10. Local run configuration
1. Configure HTTPS ports so both apps can run simultaneously.
2. Set peer URLs:
   - Takeoff points to Estimator
   - Estimator points to Takeoff
3. (Optional) Add a `launchSettings.json` for each project with fixed ports.

Deliverables:
- Developer can start both services and use UIs.

---

## 11. Validation / test checklist

### 11.1 Build validation
1. Build entire solution.
2. Ensure no missing references.

### 11.2 API interaction validation
1. Start both apps.
2. In Takeoff UI:
   - Create a condition
   - Verify Estimator receives and stores via callback
3. Modify condition metadata + some measurement values:
   - Verify Estimator merges correctly (metadata replaced, values updated by measurement id)
4. Delete a condition:
   - Verify Estimator removes it
5. In Estimator UI:
   - Click “Get All Conditions from Takeoff”
   - Verify snapshot replace

### 11.3 UI validation
- Tree shows full nested graph.
- No GUID entry is possible in form fields.
- Forms are inline (no modal dialogs).

---

## 12. Optional hardening (post-MVP)
1. Add basic retry/backoff for peer HTTP calls.
2. Add idempotency handling/logging (ignore duplicate callbacks).
3. Add minimal unit tests for `EstimatorDataStore.UpsertByCallback` merge behavior.
