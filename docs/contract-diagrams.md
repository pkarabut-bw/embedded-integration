# Contract Data Model Diagrams

## 1. Class Diagram — Data Contracts and Relationships

```mermaid
classDiagram
    class ProjectConditionQuantities {
        -Guid conditionId
        -Guid projectId
        -List~Quantity~ quantities
        -List~DocumentConditionQuantities~ documents
    }
    
    class DocumentConditionQuantities {
        -Guid documentId
        -List~Quantity~ quantities
        -List~PageConditionQuantities~ pages
    }
    
    class PageConditionQuantities {
        -Guid pageId
        -int pageNumber
        -List~Quantity~ quantities
        -List~TakeoffZoneConditionQuantities~ zones
    }
    
    class TakeoffZoneConditionQuantities {
        -Guid takeoffZoneId
        -List~Quantity~ quantities
    }
    
    class Quantity {
        -string name
        -string unit
        -double value
    }
    
    ProjectConditionQuantities "1" --> "*" DocumentConditionQuantities
    DocumentConditionQuantities "1" --> "*" PageConditionQuantities
    PageConditionQuantities "1" --> "*" TakeoffZoneConditionQuantities
    
    ProjectConditionQuantities "1" --> "*" Quantity: quantities
    DocumentConditionQuantities "1" --> "*" Quantity: quantities
    PageConditionQuantities "1" --> "*" Quantity: quantities
    TakeoffZoneConditionQuantities "1" --> "*" Quantity: quantities
```

---

## 2. Integration Message Flows

### 2.1 Condition Changed (Create/Update)

```mermaid
sequenceDiagram
    participant TakeoffUI as Takeoff UI
    participant TakeoffAPI as Takeoff API
    participant TakeoffStore as Takeoff Store
    participant EstimatorAPI as Estimator API
    participant EstimatorStore as Estimator Store
    
    TakeoffUI->>TakeoffAPI: PUT /api/demo/conditions/{id}
    TakeoffAPI->>TakeoffStore: Update(condition)
    TakeoffStore->>TakeoffStore: ComputeSummaries()
    TakeoffStore-->>TakeoffAPI: Updated Condition
    TakeoffAPI-->>TakeoffUI: 200 OK
    
    Note over TakeoffAPI: Fire-and-forget
    TakeoffAPI->>EstimatorAPI: POST /api/interactions/conditions-changed
    TakeoffAPI-->>TakeoffUI: Return immediately
    
    EstimatorAPI->>EstimatorStore: UpsertByCallback(conditions)
    EstimatorStore-->>EstimatorAPI: Merged result
    EstimatorAPI-->>TakeoffAPI: 200 OK (async)
```

### 2.2 Deletion with Post-Deletion Snapshot Sync

```mermaid
sequenceDiagram
    participant TakeoffUI as Takeoff UI
    participant TakeoffAPI as Takeoff API
    participant TakeoffStore as Takeoff Store
    participant EstimatorAPI as Estimator API
    participant EstimatorStore as Estimator Store
    
    TakeoffUI->>TakeoffAPI: DELETE /api/demo/zones/{id}
    TakeoffAPI->>TakeoffStore: DeleteZone(zoneId)
    TakeoffStore->>TakeoffStore: ComputeSummaries()
    TakeoffStore-->>TakeoffAPI: bool success
    TakeoffAPI-->>TakeoffUI: 204 No Content
    
    Note over TakeoffAPI: Fire-and-forget
    TakeoffAPI->>EstimatorAPI: POST /api/interactions/takeoffzones-deleted
    TakeoffAPI-->>TakeoffUI: Return immediately
    
    EstimatorAPI->>EstimatorStore: DeleteTakeoffZone(zoneId)
    
    Note over EstimatorAPI: AUTOMATIC POST-DELETION SYNC
    EstimatorAPI->>TakeoffAPI: GET /api/interactions/projects/{pid}/conditions
    TakeoffAPI-->>EstimatorAPI: Fresh Conditions (with updated summaries)
    EstimatorAPI->>EstimatorStore: ReplaceAll(conditions)
    EstimatorAPI-->>TakeoffAPI: 204 No Content (async)
```

### 2.3 Pull Snapshot Flow

```mermaid
sequenceDiagram
    participant EstimatorUI as Estimator UI
    participant EstimatorAPI as Estimator API
    participant TakeoffAPI as Takeoff API
    participant TakeoffStore as Takeoff Store
    
    EstimatorUI->>EstimatorAPI: POST /api/demo/pull-snapshot
    
    Note over EstimatorAPI: Step 1: Get all project IDs
    EstimatorAPI->>TakeoffAPI: GET /api/demo/projects
    TakeoffAPI->>TakeoffStore: GetProjectIds()
    TakeoffStore-->>TakeoffAPI: List~Guid~
    TakeoffAPI-->>EstimatorAPI: [projectId1, projectId2, ...]
    
    Note over EstimatorAPI: Step 2: Get conditions for each project
    loop For each project ID
        EstimatorAPI->>TakeoffAPI: GET /api/interactions/projects/{pid}/conditions-all
        TakeoffAPI->>TakeoffStore: GetAll(projectId)
        TakeoffStore-->>TakeoffAPI: List~Condition~
        TakeoffAPI-->>EstimatorAPI: [cond1, cond2, ...]
        EstimatorAPI->>EstimatorAPI: Accumulate conditions
    end
    
    EstimatorAPI->>EstimatorStore: ReplaceAllProjects(allConditions)
    EstimatorStore-->>EstimatorAPI: Done
    EstimatorAPI-->>EstimatorUI: 200 OK
```

---

## 3. Batch Deletion Flow

```mermaid
sequenceDiagram
    participant TakeoffAPI as Takeoff API
    participant TakeoffStore as Takeoff Store
    participant EstimatorAPI as Estimator API
    participant EstimatorStore as Estimator Store
    
    TakeoffAPI->>TakeoffStore: DeleteMultiple([id1, id2, id3])
    TakeoffStore->>TakeoffStore: For each: Remove by ID
    TakeoffStore->>TakeoffStore: ComputeSummaries()
    TakeoffStore-->>TakeoffAPI: bool success
    
    Note over TakeoffAPI: Fire-and-forget callback with list of IDs
    TakeoffAPI->>EstimatorAPI: POST /api/interactions/entities-deleted<br/>Body: {projectId, entityIds: [id1, id2, id3]}
    TakeoffAPI-->>TakeoffStore: Return immediately
    
    EstimatorAPI->>EstimatorStore: For each ID: DeleteEntity(id)
    
    Note over EstimatorAPI: AUTOMATIC SNAPSHOT PULL
    EstimatorAPI->>TakeoffAPI: GET /api/interactions/projects/{pid}/conditions-all
    TakeoffAPI-->>EstimatorAPI: Fresh snapshot
    EstimatorAPI->>EstimatorStore: ReplaceAll(snapshot)
    EstimatorAPI-->>TakeoffAPI: 204 No Content
```

---

## 4. Summary Aggregation Logic (Takeoff Only)

```mermaid
graph TB
    subgraph Input["Zone Data (Raw)"]
        Z1["Zone 1<br/>Length: 100"]
        Z2["Zone 2<br/>Length: 50"]
    end
    
    subgraph Step1["Step 1: Zones → Page (Aggregate)"]
        PS["PageSummary<br/>Length: 150<br/>(100 + 50)"]
    end
    
    subgraph Step2["Step 2: Pages → Document (Aggregate)"]
        P1["Page 1<br/>Length: 150"]
        P2["Page 2<br/>Length: 75"]
        DS["DocumentSummary<br/>Length: 225<br/>(150 + 75)"]
    end
    
    subgraph Step3["Step 3: Documents → Condition (Aggregate)"]
        D1["Doc 1<br/>Length: 225"]
        D2["Doc 2<br/>Length: 100"]
        COND["ProjectSummary<br/>Length: 325<br/>(225 + 100)"]
    end
    
    Z1 --> PS
    Z2 --> PS
    PS --> Step2
    P1 --> DS
    P2 --> DS
    DS --> Step3
    D1 --> COND
    D2 --> COND
    
    style Z1 fill:#f3e5f5
    style Z2 fill:#f3e5f5
    style PS fill:#fff3e0
    style DS fill:#fff3e0
    style COND fill:#e8f5e9
```

---

## 5. Key Integration Patterns

### 5.1 Callback Pattern (Takeoff → Estimator)

```mermaid
graph LR
    A["Takeoff<br/>Create/Update/Delete"] -->|Fire-and-forget<br/>POST| B["Estimator<br/>Callback Endpoint"]
    B -->|Process<br/>Locally| C["Estimator<br/>Local Store"]
    B -->|Post-Deletion:<br/>Pull Snapshot| D["Takeoff<br/>GET Snapshot"]
    D -->|Fresh Data| E["Estimator<br/>Local Store<br/>Sync"]
    
    style A fill:#e3f2fd
    style B fill:#fff3e0
    style C fill:#f3e5f5
    style D fill:#e3f2fd
    style E fill:#f3e5f5
```

### 5.2 Pull Pattern (Estimator → Takeoff)

```mermaid
graph LR
    A["Estimator<br/>On Demand"] -->|GET /api/demo/projects| B["Takeoff<br/>Project IDs"]
    B -->|List of IDs| C["Estimator<br/>For Each ID"]
    C -->|GET /api/interactions/projects/{id}/conditions| D["Takeoff<br/>Get Conditions"]
    D -->|Full Snapshot| E["Estimator<br/>Local Store"]
    
    style A fill:#e8f5e9
    style B fill:#e3f2fd
    style C fill:#fff3e0
    style D fill:#e3f2fd
    style E fill:#f3e5f5
```

---

## 6. Endpoint Route Summary

| Operation | From | To | Route | Method | Notes |
|-----------|------|----|----|--------|-------|
| **Change** | Takeoff | Estimator | `/api/interactions/conditions-changed` | POST | Full condition or diff |
| **Batch Delete** | Takeoff | Estimator | `/api/interactions/{entities}-deleted` | POST | List of IDs in payload |
| **Post-Delete Sync** | Estimator | Takeoff | `/api/interactions/projects/{id}/conditions-all` | GET | Automatic after deletion |
| **Pull Snapshot** | Estimator | Takeoff | `/api/demo/projects` | GET | Step 1: get IDs |
| **Pull Snapshot** | Estimator | Takeoff | `/api/interactions/projects/{id}/conditions-all` | GET | Step 2: get conditions |
