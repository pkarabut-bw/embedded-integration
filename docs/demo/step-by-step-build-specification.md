# Step-by-Step Build Specification

This document provides ordered instructions for generating the complete solution from scratch. Each step produces working, compilable code. Follow the steps in order.

---

## Step 1 — Create the Solution and Projects

Create a blank .NET solution with three projects:

```
EmbeddedIntegration.sln
- Contracts/          (Class Library, net9.0)
- Takeoff.Api/        (ASP.NET Core Web API, net9.0)
- Estimator.Api/      (ASP.NET Core Web API, net9.0)
```

Both API projects reference `Contracts`. Enable `ImplicitUsings` and `Nullable` in all three projects.

### Contracts.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### Takeoff.Api.csproj and Estimator.Api.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Contracts\Contracts.csproj" />
  </ItemGroup>
</Project>
```

---

## Step 2 — Define Contracts

Create the following files in the `Contracts` project. All classes are in the `Contracts` namespace.

### Quantity.cs

```csharp
public class Quantity
{
    public string Name { get; set; }
    public string Unit { get; set; }
    public double Value { get; set; }
}
```

### TakeoffZoneConditionQuantities.cs

```csharp
public class TakeoffZoneConditionQuantities
{
    public Guid TakeoffZoneId { get; set; }
    public List<Quantity> Quantities { get; set; } = new();
}
```

### PageConditionQuantities.cs

```csharp
public class PageConditionQuantities
{
    public Guid PageId { get; set; }
    public int PageNumber { get; set; }
    public List<Quantity> Quantities { get; set; } = new();
    public List<TakeoffZoneConditionQuantities> TakeoffZoneConditionQuantities { get; set; } = new();
}
```

### DocumentConditionQuantities.cs

```csharp
public class DocumentConditionQuantities
{
    public Guid DocumentId { get; set; }
    public List<Quantity> Quantities { get; set; } = new();
    public List<PageConditionQuantities> PageConditionQuantities { get; set; } = new();
}
```

### ProjectConditionQuantities.cs

```csharp
public class ProjectConditionQuantities
{
    public Guid ConditionId { get; set; }
    public Guid ProjectId { get; set; }
    public List<Quantity> Quantities { get; set; = new();
    public List<DocumentConditionQuantities> DocumentConditionQuantities { get; set; } = new();
}
```

**Verify:** The Contracts project compiles.

---

## Step 3 — Takeoff Configuration and Options

### Takeoff.Api/Options/PeerServicesOptions.cs

```csharp
namespace Takeoff.Api.Options
{
    public class PeerServicesOptions
    {
        public string EstimatorBaseUrl { get; set; }
        public int HttpTimeoutSeconds { get; set; } = 10;
    }
}
```

### Takeoff.Api/appsettings.json

```json
{
  "PeerServices": {
    "EstimatorBaseUrl": "https://localhost:5002/",
    "HttpTimeoutSeconds": 10
  }
}
```

### Takeoff.Api/Properties/launchSettings.json

```json
{
  "profiles": {
    "Takeoff.Api": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" },
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    }
  }
}
```

---

## Step 4 — Estimator Configuration and Options

### Estimator.Api/Options/PeerServicesOptions.cs

```csharp
namespace Estimator.Api.Options
{
    public class PeerServicesOptions
    {
        public string TakeoffBaseUrl { get; set; }
        public int HttpTimeoutSeconds { get; set; } = 10;
    }
}
```

### Estimator.Api/appsettings.json

```json
{
  "PeerServices": {
    "TakeoffBaseUrl": "https://localhost:5001/",
    "HttpTimeoutSeconds": 10
  }
}
```

### Estimator.Api/Properties/launchSettings.json

```json
{
  "profiles": {
    "Estimator.Api": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" },
      "applicationUrl": "https://localhost:5002;http://localhost:5003"
    }
  }
}
```

---

## Step 5 — Takeoff Data Store

Create `Takeoff.Api/Services/TakeoffDataStore.cs`.

This is a singleton, thread-safe (using `lock`) in-memory store backed by `Dictionary<Guid, List<Condition>>` (projectId → conditions).

### Public Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `GetAll` | `IReadOnlyList<Condition> GetAll(Guid projectId)` | Returns cloned list of conditions for a project |
| `Get` | `Condition? Get(Guid projectId, Guid conditionId)` | Returns a single cloned condition or null |
| `Add` | `Condition Add(Condition condition)` | Adds a cloned condition, returns clone |
| `Update` | `Condition Update(Condition condition)` | Replaces by ID (or adds), calls `ComputeSummaries`, returns clone |
| `Delete` | `bool Delete(Guid projectId, Guid conditionId)` | Removes by ID |
| `DeleteDocument` | `bool DeleteDocument(Guid projectId, Guid documentId)` | Removes document by ID from all conditions in the project, calls `ComputeSummaries` on affected conditions |
| `DeletePage` | `bool DeletePage(Guid projectId, Guid pageId)` | Removes page by ID from all docs/conditions, calls `ComputeSummaries` |
| `DeleteZone` | `bool DeleteZone(Guid projectId, Guid zoneId)` | Removes zone by ID from all pages/docs/conditions, calls `ComputeSummaries` |
| `GetProjectIds` | `List<Guid> GetProjectIds()` | Returns all project IDs |

### Private Methods

- `Clone(Condition)` — deep clone of entire condition hierarchy including all summaries and quantities
- `ComputeSummaries(Condition)` — recomputes PageSummary, DocumentSummary, ProjectSummary bottom-up by aggregating quantities grouped by `(Name, Unit)`

### Sample Data Initialization

In the constructor, call `InitializeSampleData()`:

1. Generate 1 project ID, 3 document ID's, 9 page ID's, 12 zone ID's.
2. Define 4 quantity sets (see Demo Application Specification §4.1).
3. Create 4 conditions, each sharing the same document/page/zone IDs but with different quantity names.
4. Each condition has 3 documents × 3 pages. Pages have 1 or 2 zones each. Zone values generated with `GenerateZoneQuantities(quantitySet, baseMultiplier, scaleFactor)`.
5. Call `ComputeSummaries()` on each condition.
6. Store all conditions under the project ID.

**Verify:** Takeoff.Api compiles (will need stub Program.cs).

---

## Step 6 — EstimatorClient (Takeoff → Estimator HTTP Client)

Create `Takeoff.Api/Services/EstimatorClient.cs`.

Constructor takes `HttpClient` and `ILogger<EstimatorClient>`.

### Methods

All methods are fire-and-forget (catch exceptions, log, don't rethrow):

| Method | Sends To | Payload |
|--------|----------|---------|
| `SendConditionChangedAsync(List<ProjectConditionQuantities>)` | `POST /api/interactions/conditions-changed` | List of conditions with full data or diff |
| `SendConditionDeletedAsync(projectId, conditionIds)` | `POST /api/interactions/conditions-deleted` | `{ projectId, conditionIds: List<Guid> }` |
| `SendDocumentDeletedAsync(projectId, documentIds)` | `POST /api/interactions/documents-deleted` | `{ projectId, documentIds: List<Guid> }` |
| `SendPageDeletedAsync(projectId, pageIds)` | `POST /api/interactions/pages-deleted` | `{ projectId, pageIds: List<Guid> }` |
| `SendTakeoffZoneDeletedAsync(projectId, zoneIds)` | `POST /api/interactions/takeoffzones-deleted` | `{ projectId, zoneIds: List<Guid> }` |

**Verify:** Takeoff.Api compiles.

---

## Step 7 — Takeoff InteractionsController

Create `Takeoff.Api/Controllers/InteractionsController.cs`.

Route: `api/interactions`. Inject `TakeoffDataStore`.

| Method | Path | Action |
|--------|------|--------|
| `GET` | `projects/{projectId:guid}/conditions-all` | `_store.GetAll(projectId)` → return `List<ProjectConditionQuantities>` |
| `GET` | `health` | `Ok("ok")` |

**Verify:** Takeoff.Api compiles.

---

## Step 8 — Takeoff DemoController

Create `Takeoff.Api/Controllers/DemoController.cs`.

Route: `api/demo`. Inject `TakeoffDataStore` and `EstimatorClient`.

Implement all endpoints listed in Demo Application Specification §4.3. Every mutation (Create, Update, Delete*) must send a fire-and-forget callback to Estimator after updating the store.

Include nested DTOs: `ProjectRequest`, `ProjectDataDto`.

**Verify:** Takeoff.Api compiles.

---

## Step 9 — Takeoff Program.cs

Configure the host:

1. `Configure<PeerServicesOptions>` from `"PeerServices"` config section.
2. `AddControllers()` with `AddJsonOptions` — camelCase naming, case-insensitive deserialization.
3. `AddSingleton<TakeoffDataStore>()`.
4. `AddHttpClient<EstimatorClient>` configured with base URL and timeout from options. Use `.AddTypedClient` to construct with `ILogger`.
5. `UseDefaultFiles()`.
6. `UseStaticFiles()` with cache-busting for `index.html` (set `Cache-Control: no-store`, `Pragma: no-cache`, `Expires: -1`).
7. `MapControllers()`.
8. `MapFallbackToFile("index.html")`.

**Verify:** Takeoff.Api compiles and runs (will serve empty page).

---

## Step 10 — Estimator Data Store

Create `Estimator.Api/Services/EstimatorDataStore.cs`.

Singleton, thread-safe, `Dictionary<Guid, List<ProjectConditionQuantities>>`.

### Public Methods

| Method | Description |
|--------|-------------|
| `GetAll(projectId)` | Returns cloned conditions for a project |
| `Get(projectId, conditionId)` | Returns single cloned condition |
| `ReplaceAll(projectId, List<ProjectConditionQuantities>)` | Replaces all conditions for a project |
| `ReplaceAllProjects(List<ProjectConditionQuantities>)` | Clears all data, groups conditions by ProjectId |
| `UpsertByCallback(List<ProjectConditionQuantities>)` | Merge by ID at each level, **always overwrite Quantities** from callback |
| `Delete(projectId, conditionId)` | Delete by condition ID |
| `DeleteDocument(projectId, documentId)` | Search all conditions for document by ID |
| `DeletePage(projectId, pageId)` | Search all conditions/documents for page by ID |
| `DeleteTakeoffZone(projectId, zoneId)` | Search all conditions/documents/pages for zone by ID |
| `GetProjectIds()` | Return all project IDs |

### UpsertByCallback Merge Logic

For each incoming condition:
1. If condition doesn't exist locally → insert clone.
2. If condition exists → merge documents by ID:
   - New document → add clone.
   - Existing document → overwrite `Quantities`, merge pages by ID:
     - New page → add clone.
     - Existing page → overwrite `Quantities`, merge zones by ID:
       - New zone → add clone.
       - Existing zone → overwrite `Quantities`.
3. **Always** overwrite `existing.Quantities` (ProjectSummary) with `changed.Quantities`.

### Private Methods

- `Clone()` methods for deep cloning all contract types.

**Important:** EstimatorDataStore does NOT compute summaries. It trusts summaries from Takeoff.

**Verify:** Estimator.Api compiles.

---

## Step 11 — TakeoffClient (Estimator → Takeoff HTTP Client)

Create `Estimator.Api/Services/TakeoffClient.cs`.

Constructor takes `HttpClient` and `ILogger<TakeoffClient>`.

### Methods

| Method | Calls | Returns |
|--------|-------|---------|
| `GetAllConditionsAsync(projectId)` | `GET /api/interactions/projects/{projectId}/conditions-all` | `List<ProjectConditionQuantities>` |
| `GetAllProjectIdsAsync()` | `GET /api/demo/projects` | `List<Guid>` |
| `PullSnapshotAsync()` | Calls `GetAllProjectIdsAsync`, then `GetAllConditionsAsync` for each | `List<ProjectConditionQuantities>` |
| `PullProjectSnapshotAsync(projectId)` | Calls `GetAllConditionsAsync(projectId)` | `List<ProjectConditionQuantities>` |

All methods log errors and rethrow on failure.

**Verify:** Estimator.Api compiles.

---

## Step 12 — Estimator InteractionsController

Create `Estimator.Api/Controllers/InteractionsController.cs`.

Route: `api/interactions`. Inject `TakeoffClient` and `EstimatorDataStore`.

### Endpoints

| Method | Path | Action |
|--------|------|--------|
| `POST` | `conditions-changed` | Body: `List<ProjectConditionQuantities>` → `_store.UpsertByCallback()` → `Ok(result)` |
| `POST` | `conditions-deleted` | Body: `{ projectId, conditionIds }` → delete locally → pull project snapshot → replace local data |
| `POST` | `documents-deleted` | Body: `{ projectId, documentIds }` → delete locally → pull project snapshot |
| `POST` | `pages-deleted` | Body: `{ projectId, pageIds }` → delete locally → pull project snapshot |
| `POST` | `takeoffzones-deleted` | Body: `{ projectId, zoneIds }` → delete locally → pull project snapshot |
| `POST` | `project-deleted` | Body: `{ projectId }` → delete project locally (no snapshot pull) |
| `GET` | `health` | `Ok("ok")` |

**Post-deletion snapshot pull** is wrapped in try/catch — failure is logged but does not affect the deletion response.

**Verify:** Estimator.Api compiles.

---

## Step 13 — Estimator DemoController

Create `Estimator.Api/Controllers/DemoController.cs`.

Route: `api/demo`. Inject `EstimatorDataStore` and `TakeoffClient`.

Implement endpoints listed in Demo Application Specification §5.3.

**Verify:** Estimator.Api compiles.

---

## Step 14 — Estimator Program.cs

Configure the host:

1. `Configure<PeerServicesOptions>` from `"PeerServices"` config section.
2. `AddControllers()` with `AddJsonOptions` — camelCase naming, case-insensitive deserialization.
3. `AddSingleton<EstimatorDataStore>()`.
4. `AddHttpClient<TakeoffClient>` configured with base URL and timeout from options. Use `.AddTypedClient` to construct with `ILogger`.
5. `UseDefaultFiles()`.
6. `UseStaticFiles()`.
7. `MapControllers()`.
8. `MapFallbackToFile("index.html")`.

**Verify:** Both projects compile and run.

---

## Step 15 — Takeoff UI (wwwroot/index.html)

Create `Takeoff.Api/wwwroot/index.html`.

Build the Takeoff UI as specified in Demo Application Specification §4.4:

- Include Bootstrap 5, jQuery, jsTree from CDN.
- Two-column layout: tree (left), form panel (right).
- Spinner overlay for async operations.
- Project selector dropdown with auto-select first project.
- Tree with custom emoji icons via CSS `::before` pseudo-elements on `li_attr` classes: `project-node`, `condition-node`, `document-node`, `page-node`, `zone-node`.
- Stable client-side numbering for documents, pages, zones using ID-to-number dictionaries.
- Forms: Condition (read-only summary + Add Document / Delete), Document (read-only summary + Add Page / Delete), Page (read-only summary + Add Zone / Delete), Zone (editable summary table + Add Row / Save / Delete).
- Summary tables in Condition, Document, Page forms are read-only (display-only). Server computes all summaries.
- Zone summary table has editable Name, Unit, Value inputs plus a remove-row button per row.
- On Save Zone: fetch full condition, update zone's `zoneSummary`, PUT condition to server.
- All tree operations reload the project after mutation.

**Verify:** Takeoff runs, UI loads, tree renders with sample data.

---

## Step 16 — Estimator UI (wwwroot/index.html)

Create `Estimator.Api/wwwroot/index.html`.

Build the Estimator UI as specified in Demo Application Specification §5.4:

- Include Bootstrap 5, jQuery, jsTree from CDN.
- Two-column layout: tree (left), read-only info panel (right).
- "Pull Snapshot from Takeoff" button — `POST /api/demo/pull-snapshot`, then refresh from `GET /api/demo/all-conditions`.
- Auto-polling every 500ms on `GET /api/demo/all-conditions`. Change detection by comparing full `JSON.stringify` output.
- Tree with emoji icons. Root node "All Projects" has no icon (CSS override on `#all-projects`). Data grouped by project.
- On node select: show read-only detail card (Condition — ProjectSummary, Document — DocumentSummary, Page — PageSummary + PageNumber, Zone — ZoneSummary).
- Stable numbering using same client-side dictionary approach as Takeoff.

**Verify:** Both services run. Pull Snapshot works. Changes in Takeoff are reflected in Estimator via callbacks + polling.

---

## Step 17 — End-to-End Verification

Test the following scenarios manually:

1. **Initial load:** Takeoff UI shows 4 conditions with correct summaries.
2. **Pull Snapshot:** Estimator pulls data, tree matches Takeoff.
3. **Edit zone value:** In Takeoff, change a zone quantity value — Save. Takeoff summaries update. Estimator updates via callback + polling within 0.5s.
4. **Add quantity row:** In Takeoff, add a new row to a zone — Save. Summaries recalculate.
5. **Remove quantity row:** In Takeoff, remove a row — Save. Summaries recalculate.
6. **Delete zone:** In Takeoff, delete a zone. Summaries recalculate. Estimator receives callback, pulls snapshot, summaries match.
7. **Delete page:** Same flow as zone deletion.
8. **Delete document:** Same flow.
9. **Delete condition:** Estimator receives callback, pulls snapshot.
10. **Add condition/document/page/zone:** Created in Takeoff, callback sends to Estimator, polling reflects the change.
