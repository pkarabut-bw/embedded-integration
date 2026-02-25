# Contract Data Model Diagrams

## 1. Class Diagram — Data Contracts and Relationships

```mermaid
classDiagram
    class Condition {
        -Guid id
        -Guid projectId
        -List~Quantity~ projectSummary
        -List~Document~ documents
    }
    
    class Document {
        -Guid id
        -List~Quantity~ documentSummary
        -List~Page~ pages
    }
    
    class Page {
        -Guid id
        -int pageNumber
        -List~Quantity~ pageSummary
        -List~TakeoffZone~ takeoffZones
    }
    
    class TakeoffZone {
        -Guid id
        -List~Quantity~ zoneSummary
    }
    
    class Quantity {
        -string name
        -string unit
        -double value
    }
    
    Condition "1" --> "*" Document
    Document "1" --> "*" Page
    Page "1" --> "*" TakeoffZone
    
    Condition "1" --> "*" Quantity : projectSummary
    Document "1" --> "*" Quantity : documentSummary
    Page "1" --> "*" Quantity : pageSummary
    TakeoffZone "1" --> "*" Quantity : zoneSummary
```

---

## 2. Data Hierarchy — Tree Structure

```mermaid
graph TD
    A["Condition<br/>ID: {guid}<br/>ProjectID: {guid}"] --> B["ProjectSummary<br/>List~Quantity~<br/>(aggregated)"]
    A --> C["Documents"]
    
    C --> D["Document 1<br/>ID: {guid}"]
    C --> E["Document 2<br/>ID: {guid}"]
    C --> F["Document N<br/>ID: {guid}"]
    
    D --> D1["DocumentSummary<br/>List~Quantity~<br/>(aggregated)"]
    D --> D2["Pages"]
    
    D2 --> D3["Page 1<br/>ID: {guid}<br/>Number: 1"]
    D2 --> D4["Page 2<br/>ID: {guid}<br/>Number: 2"]
    D2 --> D5["Page N<br/>ID: {guid}"]
    
    D3 --> D6["PageSummary<br/>List~Quantity~<br/>(aggregated)"]
    D3 --> D7["Zones"]
    
    D7 --> D8["Zone 1<br/>ID: {guid}"]
    D7 --> D9["Zone 2<br/>ID: {guid}"]
    D7 --> D10["Zone N<br/>ID: {guid}"]
    
    D8 --> D11["ZoneSummary<br/>List~Quantity~<br/>(raw data)"]
    D9 --> D12["ZoneSummary<br/>List~Quantity~<br/>(raw data)"]
    D10 --> D13["ZoneSummary<br/>List~Quantity~<br/>(raw data)"]
    
    style A fill:#e1f5ff
    style B fill:#fff3e0
    style D1 fill:#fff3e0
    style D6 fill:#fff3e0
    style D11 fill:#f3e5f5
    style D12 fill:#f3e5f5
    style D13 fill:#f3e5f5
```

---

## 3. Summary Aggregation Flow

```mermaid
graph LR
    subgraph Input["Input: Zone Data"]
        Z1["Zone 1<br/>Length: 100"]
        Z2["Zone 2<br/>Length: 50"]
    end
    
    subgraph Step1["Step 1: Aggregate Zones ? Page"]
        PS["PageSummary<br/>Length: 150<br/>(100 + 50)"]
    end
    
    subgraph Step2["Step 2: Aggregate Pages ? Document"]
        P1["Page 1<br/>Length: 150"]
        P2["Page 2<br/>Length: 75"]
        DS["DocumentSummary<br/>Length: 225<br/>(150 + 75)"]
    end
    
    subgraph Step3["Step 3: Aggregate Documents ? Condition"]
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

## 4. Quantity Aggregation Logic

```mermaid
graph TB
    subgraph Input["Zone Quantities (Raw)"]
        Q1["Quantity<br/>Name: Length<br/>Unit: ft<br/>Value: 100"]
        Q2["Quantity<br/>Name: Width<br/>Unit: ft<br/>Value: 50"]
        Q3["Quantity<br/>Name: Length<br/>Unit: ft<br/>Value: 150"]
    end
    
    subgraph Process["Aggregation by Name|Unit"]
        G1["Group 1: Length|ft<br/>Values: 100, 150"]
    end
    
    subgraph Output["Aggregated Result"]
        AGG["Quantity<br/>Name: Length<br/>Unit: ft<br/>Value: 250<br/>(100 + 150)"]
    end
    
    Q1 --> G1
    Q3 --> G1
    Q2 --> Q2_note["Different Unit<br/>stays separate"]
    G1 --> AGG
    
    style Q1 fill:#f3e5f5
    style Q2 fill:#f3e5f5
    style Q3 fill:#f3e5f5
    style AGG fill:#e8f5e9
```

---

## 5. JSON Structure Example

```mermaid
graph TD
    A["Condition<br/>{<br/>&nbsp;&nbsp;id: guid,<br/>&nbsp;&nbsp;projectId: guid,<br/>&nbsp;&nbsp;projectSummary: [],<br/>&nbsp;&nbsp;documents: []<br/>}"] --> B["Document<br/>{<br/>&nbsp;&nbsp;id: guid,<br/>&nbsp;&nbsp;documentSummary: [],<br/>&nbsp;&nbsp;pages: []<br/>}"]
    B --> C["Page<br/>{<br/>&nbsp;&nbsp;id: guid,<br/>&nbsp;&nbsp;pageNumber: int,<br/>&nbsp;&nbsp;pageSummary: [],<br/>&nbsp;&nbsp;takeoffZones: []<br/>}"]
    C --> D["TakeoffZone<br/>{<br/>&nbsp;&nbsp;id: guid,<br/>&nbsp;&nbsp;zoneSummary: []<br/>}"]
    D --> E["Quantity<br/>{<br/>&nbsp;&nbsp;name: string,<br/>&nbsp;&nbsp;unit: string,<br/>&nbsp;&nbsp;value: double<br/>}"]
    
    style A fill:#e1f5ff
    style B fill:#e1f5ff
    style C fill:#e1f5ff
    style D fill:#e1f5ff
    style E fill:#fff3e0
```

---

## 6. Integration Message Flows

### Condition Changed (Create/Update)

```mermaid
sequenceDiagram
    participant Takeoff UI
    participant Takeoff API
    participant Takeoff Store
    participant Estimator API
    participant Estimator Store
    
    Takeoff UI->>Takeoff API: PUT /api/demo/conditions/{id}
    Takeoff API->>Takeoff Store: Update(condition)
    Takeoff Store->>Takeoff Store: ComputeSummaries()
    Takeoff Store-->>Takeoff API: Updated Condition
    Takeoff API-->>Takeoff UI: 200 OK
    
    Takeoff API->>Estimator API: POST /api/interactions/condition-changed<br/>(fire-and-forget)
    Note over Takeoff API: Returns immediately
    
    Estimator API->>Estimator Store: UpsertByCallback(conditions)
    Estimator Store-->>Estimator API: Merged result
    Estimator API-->>Takeoff API: 200 OK (async)
```

### Deletion with Snapshot Sync

```mermaid
sequenceDiagram
    participant Takeoff UI
    participant Takeoff API
    participant Takeoff Store
    participant Estimator API
    participant Estimator Store
    participant Estimator UI
    
    Takeoff UI->>Takeoff API: DELETE /api/demo/zones/{id}
    Takeoff API->>Takeoff Store: DeleteZone(zoneId)
    Takeoff Store->>Takeoff Store: ComputeSummaries()
    Takeoff Store-->>Takeoff API: bool success
    Takeoff API-->>Takeoff UI: 204 No Content
    
    Takeoff API->>Estimator API: POST /api/interactions/takeoffzone-deleted<br/>(fire-and-forget)
    Note over Takeoff API: Returns immediately
    
    Estimator API->>Estimator Store: DeleteTakeoffZone(zoneId)
    Estimator API->>Takeoff API: GET /api/interactions/projects/{pid}/conditions
    Takeoff API-->>Estimator API: Updated Conditions
    Estimator API->>Estimator Store: ReplaceAll(conditions)
    Estimator API-->>Takeoff API: 204 No Content (async)
    
    Note over Estimator UI: Polls every 500ms
    Estimator UI->>Estimator API: GET /api/demo/all-conditions
    Estimator API-->>Estimator UI: Refreshed data
    Estimator UI->>Estimator UI: Refresh tree
```

### Pull Snapshot Flow

```mermaid
sequenceDiagram
    participant Estimator UI
    participant Estimator API
    participant Takeoff API
    participant Takeoff Store
    
    Estimator UI->>Estimator API: POST /api/demo/pull-snapshot
    
    Estimator API->>Takeoff API: GET /api/demo/projects
    Takeoff API->>Takeoff Store: GetProjectIds()
    Takeoff Store-->>Takeoff API: List~Guid~
    Takeoff API-->>Estimator API: [projectId1, projectId2, ...]
    
    loop For each project
        Estimator API->>Takeoff API: GET /api/demo/projects/{pid}/conditions
        Takeoff API->>Takeoff Store: GetAll(projectId)
        Takeoff Store-->>Takeoff API: List~Condition~
        Takeoff API-->>Estimator API: [condition1, condition2, ...]
        Estimator API->>Estimator API: Collect all conditions
    end
    
    Estimator API->>Estimator Store: ReplaceAllProjects(allConditions)
    Estimator Store-->>Estimator API: Done
    Estimator API-->>Estimator UI: 200 OK + metadata
    
    Estimator UI->>Estimator API: GET /api/demo/all-conditions
    Estimator API->>Estimator Store: GetAll() across all projects
    Estimator Store-->>Estimator API: List~Condition~
    Estimator API-->>Estimator UI: All conditions
    Estimator UI->>Estimator UI: Render tree
```

---

## 7. Callback Type Summary

```mermaid
graph TB
    subgraph CreateUpdate["Create/Update Operations"]
        CU["POST /api/interactions/condition-changed<br/>Payload: List~Condition~<br/>- Full condition with all summaries<br/>- Used for create, update, or any change"]
    end
    
    subgraph DeleteOps["Delete Operations"]
        CD["POST /api/interactions/condition-deleted<br/>Payload: { projectId, conditionId }"]
        DD["POST /api/interactions/document-deleted<br/>Payload: { projectId, documentId }"]
        PD["POST /api/interactions/page-deleted<br/>Payload: { projectId, pageId }"]
        ZD["POST /api/interactions/takeoffzone-deleted<br/>Payload: { projectId, zoneId }"]
    end
    
    subgraph Behavior["Estimator Behavior"]
        B1["For Create/Update:<br/>Merge by ID at each level<br/>Overwrite summaries"]
        B2["For Deletes:<br/>Delete by ID (search-based)<br/>Pull full project snapshot<br/>Replace all local data"]
    end
    
    CU --> B1
    CD --> B2
    DD --> B2
    PD --> B2
    ZD --> B2
    
    style CU fill:#e3f2fd
    style CD fill:#ffebee
    style DD fill:#ffebee
    style PD fill:#ffebee
    style ZD fill:#ffebee
    style B1 fill:#e8f5e9
    style B2 fill:#fff3e0
```
