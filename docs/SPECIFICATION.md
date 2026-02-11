## 1) Interaction overview

### 1.1 Responsibility boundaries
- **Takeoff** is the *system of record* for authoring and changing *Conditions*.
- **Estimator** is a downstream consumer that keeps its own internal model synchronized with Takeoff changes.

### 1.2 Synchronization mechanisms
Synchronization between Takeoff and Estimator consists of:

1. **Snapshot read** (Takeoff ? Estimator pull)
   - Estimator obtains the full current set of `Condition` entries for a given `ProjectId` from Takeoff.
   - Estimator replaces its stored state for that `ProjectId` with the received snapshot.

2. **Incremental change propagation** (Takeoff ? Estimator push)
   - When Takeoff detects a change to a `Condition`, it sends the changed `Condition` to Estimator.
   - After receiving the change, Estimator applies the update to its internal domain model.

3. **Deletion propagation** (Takeoff ? Estimator push)
   - When Takeoff deletes a `Condition`, it notifies Estimator.
   - After receiving the deletion notification, Estimator removes the condition from its internal domain model.

---

## 2) Contracts

### 2.1 `Condition`
Business meaning:
- Represents a single logical condition associated with a specific project.
- `Measurements` represent the measurable attributes of the condition.

Fields:
- `Id` : `Guid`
  - Unique identifier of the condition.
  
- `ProjectId` : `Guid`
  - Identifier of the owning project.
  
- `Measurements` : `List<Measurement>`
  - The set of measurement entries that belong to the condition.

### 2.2 `Measurement`
Business meaning:
- Represents a single named measurement entry for a condition.

Fields:
- `MeasurementName` : `string`
  - Human-meaningful identifier of the measurement.
  - Used for matching/merging measurement entries across updates.
- `UnitsOfMeasurements` : `string`
  - Unit label for interpretation (e.g., "m", "ft", "ea").
- `Value` : `double`
  - Numeric value for the measurement.

---

## 3) Server methods (Estimator ? Takeoff)

### 3.1 Takeoff ? Estimator server methods (implemented by Estimator)

#### `ConditionChanged(Condition condition) : Condition`
Purpose:
- Receive notification that a condition was created or updated in Takeoff.

Input:
- `condition` (`Contracts.Condition`): the condition state provided by Takeoff.

Output:
- Returns a `Contracts.Condition` representing the resulting stored/processed state after Estimator applies the change.

Behavior (abstract):
- Estimator validates the input identifiers.
- Estimator applies the change to its domain model.

#### `ConditionDeleted(DeleteRequest req) : IActionResult`
Purpose:
- Receive notification that a condition was deleted in Takeoff.

Input:
- `req` (request DTO):
  - `ProjectId : Guid`
  - `ConditionId : Guid`

Output:
- Returns a result indicating whether the deletion was applied.

Behavior (abstract):
- Estimator validates the identifiers.
- Estimator removes the corresponding condition from its domain model for the specified project.

### 3.2 Estimator ? Takeoff server methods (implemented by Takeoff)

#### `GetAll(Guid projectId) : List<Condition>`
Purpose:
- Provide a snapshot of all conditions in Takeoff for a given project.

Input:
- `projectId : Guid`

Output:
- `List<Contracts.Condition>` representing the current complete state for that project.

Behavior (abstract):
- Takeoff validates the project identifier.
- Takeoff retrieves the current set of conditions for the project.
- Takeoff returns the snapshot.

