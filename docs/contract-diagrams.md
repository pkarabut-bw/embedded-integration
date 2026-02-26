# Contract Data Model Diagrams

## 1. Class Diagram — Data Contracts and Relationships

```mermaid
classDiagram
    class ProjectConditionQuantities {
        -Guid conditionId
        -Guid projectId
        -List~Quantity~ quantities
        -List~DocumentConditionQuantities~ documentConditionQuantities
    }
    
    class DocumentConditionQuantities {
        -Guid documentId
        -List~Quantity~ quantities
        -List~PageConditionQuantities~ pageConditionQuantities
    }
    
    class PageConditionQuantities {
        -Guid pageId
        -int pageNumber
        -List~Quantity~ quantities
        -List~TakeoffZoneConditionQuantities~ takeoffZoneConditionQuantities
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

### 2.1 Conditions Changed (Create/Update)

```mermaid
sequenceDiagram
    participant TakeoffAPI as Takeoff API
    participant TakeoffStore as Takeoff Store
    participant EstimatorAPI as Estimator API
    participant EstimatorStore as Estimator Store
    
    TakeoffAPI->>TakeoffStore: Update(condition)
    TakeoffStore->>TakeoffStore: ComputeSummaries()
    TakeoffStore-->>TakeoffAPI: Updated Condition
    
    Note over TakeoffAPI: Fire-and-forget callback
    TakeoffAPI->>EstimatorAPI: POST /api/interactions/conditions-changed
    TakeoffAPI-->>TakeoffStore: Return immediately
    
    EstimatorAPI->>EstimatorStore: UpsertByCallback(conditions)
    EstimatorStore-->>EstimatorAPI: Merged result
    EstimatorAPI-->>TakeoffAPI: 200 OK
```

### 2.2 Deletion with Post-Deletion Snapshot Sync

```mermaid
sequenceDiagram
    participant TakeoffAPI as Takeoff API
    participant TakeoffStore as Takeoff Store
    participant EstimatorAPI as Estimator API
    participant EstimatorStore as Estimator Store
    
    TakeoffAPI->>TakeoffStore: DeleteEntity(entityId)
    TakeoffStore->>TakeoffStore: ComputeSummaries()
    TakeoffStore-->>TakeoffAPI: bool success
    
    Note over TakeoffAPI: Fire-and-forget callback
    TakeoffAPI->>EstimatorAPI: POST /api/interactions/entities-deleted
    TakeoffAPI-->>TakeoffStore: Return immediately
    
    EstimatorAPI->>EstimatorStore: DeleteEntity(entityId)
    
    Note over EstimatorAPI: AUTOMATIC POST-DELETION SYNC
    EstimatorAPI->>TakeoffAPI: GET /api/interactions/projects/{pid}/conditions-all
    TakeoffAPI-->>EstimatorAPI: Fresh snapshot with updated summaries
    EstimatorAPI->>EstimatorStore: ReplaceAll(snapshot)
    EstimatorAPI-->>TakeoffAPI: 204 No Content
```

### 2.3 Pull Snapshot Flow

```mermaid
sequenceDiagram
    participant EstimatorAPI as Estimator API
    participant TakeoffAPI as Takeoff API
    
    EstimatorAPI->>TakeoffAPI: GET /api/demo/projects
    TakeoffAPI-->>EstimatorAPI: List of project IDs
    
    Note over EstimatorAPI: For each project ID
    loop Project iteration
        EstimatorAPI->>TakeoffAPI: GET /api/interactions/projects/{pid}/conditions-all
        TakeoffAPI-->>EstimatorAPI: All conditions for project
        EstimatorAPI->>EstimatorAPI: Accumulate conditions
    end
    
    EstimatorAPI->>EstimatorAPI: ReplaceAllProjects(allConditions)
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

## 4. Integration Patterns

### 4.1 Callback Pattern (Takeoff → Estimator)

```mermaid
graph LR
    A["Takeoff API<br/>Create/Update/Delete"] -->|Fire-and-forget<br/>POST| B["Estimator API<br/>Callback Endpoint"]
    B -->|Process<br/>Locally| C["Estimator Store"]
    B -->|Post-Deletion:<br/>Pull Snapshot| D["Takeoff API<br/>GET Snapshot"]
    D -->|Fresh Data| E["Estimator Store<br/>Sync"]
    
    style A fill:#e3f2fd
    style B fill:#fff3e0
    style C fill:#f3e5f5
    style D fill:#e3f2fd
    style E fill:#f3e5f5
```

### 4.2 Pull Pattern (Estimator → Takeoff)

```mermaid
graph LR
    A["Estimator API<br/>On Demand"] -->|GET /api/demo/projects| B["Takeoff API<br/>Project IDs"]
    B -->|List of IDs| C["Estimator API<br/>For Each ID"]
    C -->|GET /api/interactions/projects/{id}/conditions-all| D["Takeoff API<br/>Get Conditions"]
    D -->|Full Conditions| E["Estimator Store"]
    
    style A fill:#e8f5e9
    style B fill:#e3f2fd
    style C fill:#fff3e0
    style D fill:#e3f2fd
    style E fill:#f3e5f5
```

---

## 5. Endpoint Summary

| Operation | From | To | Route | Method |
|-----------|------|----|----|--------|
| Conditions Changed | Takeoff | Estimator | `/api/interactions/conditions-changed` | POST |
| Batch Delete Entities | Takeoff | Estimator | `/api/interactions/{entity}-deleted` | POST |
| Get Conditions for Project | Estimator | Takeoff | `/api/interactions/projects/{id}/conditions-all` | GET |
