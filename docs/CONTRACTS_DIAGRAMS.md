# Contracts Mermaid Diagrams

This file contains Mermaid diagrams visualizing the Contracts used in the solution (`Contracts` project) and simple interaction sequences that reference the contract shapes.

## Class diagram

```mermaid
classDiagram
    %% Contracts classes
    class Condition {
        Guid Id
        Guid ProjectId
        List<Measurement> Measurements
    }

    class Measurement {
        string MeasurementName
        string UnitsOfMeasurements
        double Value
    }

    %% Relationship
    Condition "1" o-- "*" Measurement : has
```

## Sequence: Snapshot pull (Estimator requests full Condition list from Takeoff)

```mermaid
sequenceDiagram
    participant Estimator
    participant Takeoff
    Estimator->>Takeoff: GetAll(projectId)
    Note right of Takeoff: returns List<Condition>
    Takeoff-->>Estimator: [Condition{Id, ProjectId, Measurements[...]}, ...]
    Note left of Estimator: Estimator.ReplaceAll(projectId, snapshot)
```

## Sequence: Change propagation (Takeoff ? Estimator)

```mermaid
sequenceDiagram
    participant Takeoff
    participant Estimator
    Takeoff->>Estimator: ConditionChanged(condition)
    Note right of Estimator: Estimator.UpsertByCallback(condition)
    Estimator-->>Takeoff: 200 OK (merged Condition)
```

## Sequence: Deletion propagation (Takeoff ? Estimator)

```mermaid
sequenceDiagram
    participant Takeoff
    participant Estimator
    Takeoff->>Estimator: ConditionDeleted({ projectId, conditionId })
    Note right of Estimator: Estimator.Delete(projectId, conditionId)
    Estimator-->>Takeoff: 204 NoContent / 404 NotFound
```

## JSON example (for reference)

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "projectId": "11111111-1111-1111-1111-111111111111",
  "measurements": [
    {
      "measurementName": "Length",
      "unitsOfMeasurements": "m",
      "value": 12.5
    },
    {
      "measurementName": "Weight",
      "unitsOfMeasurements": "kg",
      "value": 3.2
    }
  ]
}
```


---

Place these diagrams into a Markdown file or render them in any Mermaid-capable viewer to visualize the Contracts and example interactions.