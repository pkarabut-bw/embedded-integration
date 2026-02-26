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
    participant Takeoff
    participant Estimator
    
    Takeoff->>Takeoff: Update condition
    Takeoff->>Takeoff: CalculateQuantitiesSummaries()
    
    Note over Takeoff: Fire-and-forget callback
    Takeoff->>Estimator: POST /api/interactions/conditions-changed
    Takeoff-->>Takeoff: Return immediately
    
    Estimator->>Estimator: UpsertByCallback(conditions)
    Estimator-->>Takeoff: 200 OK
```

### 2.2 Deletion with Post-Deletion Snapshot Sync

```mermaid
sequenceDiagram
    participant Takeoff
    participant Estimator
    
    Takeoff->>Takeoff: DeleteEntity(entityId)
    
    Note over Takeoff: Fire-and-forget callback
    Takeoff->>Estimator: POST /api/interactions/entities-deleted
    Takeoff-->>Takeoff: Return immediately
    
    Estimator->>Estimator: DeleteEntity(entityId)
    
    Note over Estimator: AUTOMATIC POST-DELETION SYNC
    Estimator->>Takeoff: GET /api/interactions/projects/{pid}/conditions-all
    Takeoff-->>Estimator: Fresh snapshot with updated summaries
    Estimator->>Estimator: ReplaceAll(snapshot)
    Estimator-->>Takeoff: 204 No Content
```

### 2.3 Pull Project Snapshot

```mermaid
sequenceDiagram
    participant Estimator
    participant Takeoff
    
    Estimator->>Takeoff: GET /api/interactions/projects/{projectId}/conditions-all
    Takeoff-->>Estimator: All conditions for project
    
    Estimator->>Estimator: Store all conditions
```

---

## 3. Batch Deletion Flow

```mermaid
sequenceDiagram
    participant Takeoff
    participant Estimator
    
    Takeoff->>Takeoff: DeleteMultiple([id1, id2, id3])
    Takeoff->>Takeoff: ComputeSummaries()
    
    Note over Takeoff: Fire-and-forget callback with list of IDs
    Takeoff->>Estimator: POST /api/interactions/entities-deleted
    Takeoff->>Estimator: Body: {projectId, entityIds: [id1, id2, id3]}
    Takeoff-->>Takeoff: Return immediately
    
    Estimator->>Estimator: For each ID: DeleteEntity(id)
    
    Note over Estimator: AUTOMATIC SNAPSHOT PULL
    Estimator->>Takeoff: GET /api/interactions/projects/{pid}/conditions-all
    Takeoff-->>Estimator: Fresh snapshot
    Estimator->>Estimator: ReplaceAll(snapshot)
    Estimator-->>Takeoff: 204 No Content
```

---

## 4. Integration Patterns

### 4.1 Callback Pattern (Takeoff → Estimator)

```mermaid
graph LR
    A["Takeoff<br/>Create/Update/Delete"] -->|Fire-and-forget<br/>POST| B["Estimator<br/>Callback Endpoint"]
    B -->|Process<br/>Locally| C["Estimator<br/>Local Store"]
    B -->|Post-Deletion:<br/>Pull Snapshot| D["Takeoff<br/>GET Snapshot"]
    D -->|Fresh Data| E["Estimator<br/>Sync"]
    
    style A fill:#e3f2fd
    style B fill:#fff3e0
    style C fill:#f3e5f5
    style D fill:#e3f2fd
    style E fill:#f3e5f5
```

### 4.2 Pull Pattern (Estimator → Takeoff)

```mermaid
graph LR
    A["Estimator<br/>Pull Snapshot"] -->|Step 1| B["Takeoff<br/>Get Project IDs"]
    B -->|Step 2<br/>For each ID| C["Takeoff<br/>Get Conditions"]
    C -->|Accumulate| D["Estimator<br/>Local Store"]
    
    style A fill:#e8f5e9
    style B fill:#e3f2fd
    style C fill:#e3f2fd
    style D fill:#f3e5f5
```

---

## 5. Endpoint Summary

| Operation | From | To | Route | Method |
|-----------|------|----|----|--------|
| Conditions Changed | Takeoff | Estimator | `/api/interactions/conditions-changed` | POST |
| Batch Delete Entities | Takeoff | Estimator | `/api/interactions/{entity}-deleted` | POST |
| Get Conditions for Project | Estimator | Takeoff | `/api/interactions/projects/{id}/conditions-all` | GET |
