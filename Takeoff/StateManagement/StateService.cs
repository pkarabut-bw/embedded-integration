using Contracts;
using System;
using Validation;

namespace Takeoff.StateManagement
{
    public class StateService
    {
        private readonly TakeoffValidator _validator = new TakeoffValidator();
        private readonly object _lock = new object();
        private TakeoffState _state;

        // Keep an in-memory actions list for recording user-driven changes
        private readonly TakeoffActionsList _actionsList = new TakeoffActionsList
        {
            ProjectId = Guid.Empty,
            Actions = new System.Collections.Generic.List<TakeoffAction>()
        };

        public StateService()
        {
            // Initialize in-memory state at construction time
            _state = GetDefaultTakeoffState();
        }

        // Return the current state (not a deep copy)
        public TakeoffState GetState()
        {
            lock (_lock)
            {
                return _state;
            }
        }

        // Return actions list (caller should not modify)
        public TakeoffActionsList GetActionsList()
        {
            lock (_lock)
            {
                return _actionsList;
            }
        }

        // helper to record an action. Returns validation result; does not throw on validation failures.
        private ValidationResult RecordAction(string actionName, string entityType, Quantity quantity = null, Measurement measurement = null)
        {
            lock (_lock)
            {
                var action = new TakeoffAction
                {
                    ActionName = actionName,
                    EntityType = entityType,
                    OrderNumber = (_actionsList.Actions?.Count ?? 0) + 1,
                    Quantity = quantity,
                    Measurement = measurement
                };

                // Validate the single action and the resulting actions list before committing
                var singleValidation = _validator.ValidateTakeoffAction(action);
                var combined = new ValidationResult { IsValid = true, Errors = new System.Collections.Generic.List<string>() };

                if (!singleValidation.IsValid)
                {
                    combined.IsValid = false;
                    combined.Errors.AddRange(singleValidation.Errors ?? new System.Collections.Generic.List<string>());
                }

                // Build a prospective actions list with the new action and validate it
                var prospective = new TakeoffActionsList
                {
                    ProjectId = _actionsList.ProjectId,
                    Actions = new System.Collections.Generic.List<TakeoffAction>(_actionsList.Actions ?? new System.Collections.Generic.List<TakeoffAction>())
                };
                prospective.Actions.Add(action);

                var listValidation = _validator.ValidateTakeoffActionsList(prospective);
                if (!listValidation.IsValid)
                {
                    combined.IsValid = false;
                    combined.Errors.AddRange(listValidation.Errors ?? new System.Collections.Generic.List<string>());
                }

                if (!combined.IsValid)
                {
                    return combined; // do not mutate _actionsList
                }

                _actionsList.Actions.Add(action);
                return combined; // valid (IsValid true)
            }
        }

        

        // Reset to generated default state
        public TakeoffState ResetToDefault()
        {
            var defaultState = GetDefaultTakeoffState();
            lock (_lock)
            {
                _state = defaultState;
                _actionsList.Actions.Clear();
            }

            return _state;
        }

        // Add a quantity to the in-memory state
        public ValidationResult AddQuantity(Quantity quantity)
        {
            if (quantity == null) throw new ArgumentNullException(nameof(quantity));
            ValidationResult stateValidation;
            lock (_lock)
            {
                var copy = new TakeoffState
                {
                    Quantities = new System.Collections.Generic.List<Quantity>(_state.Quantities ?? new System.Collections.Generic.List<Quantity>()),
                    Measurements = new System.Collections.Generic.List<Measurement>(_state.Measurements ?? new System.Collections.Generic.List<Measurement>())
                };
                copy.Quantities.Add(quantity);
                stateValidation = _validator.ValidateTakeoffState(copy);
                if (!stateValidation.IsValid) return stateValidation;
                _state = copy;
            }

            // Record action outside lock to avoid deadlock; return any action validation errors to caller
            var actionResult = RecordAction("create", "Quantity", quantity: quantity);
            if (!actionResult.IsValid) return actionResult;
            return stateValidation;
        }

        // Update existing quantity by Id
        public ValidationResult UpdateQuantity(Quantity quantity)
        {
            if (quantity == null) throw new ArgumentNullException(nameof(quantity));
            ValidationResult stateValidation;
            lock (_lock)
            {
                var quantities = new System.Collections.Generic.List<Quantity>(_state.Quantities ?? new System.Collections.Generic.List<Quantity>());
                var idx = quantities.FindIndex(q => q != null && q.Id == quantity.Id);
                if (idx < 0)
                {
                    var result = new ValidationResult { IsValid = false, Errors = new System.Collections.Generic.List<string> { "Quantity not found" } };
                    return result;
                }
                quantities[idx] = quantity;
                var copy = new TakeoffState { Quantities = quantities, Measurements = new System.Collections.Generic.List<Measurement>(_state.Measurements ?? new System.Collections.Generic.List<Measurement>()) };
                stateValidation = _validator.ValidateTakeoffState(copy);
                if (!stateValidation.IsValid) return stateValidation;
                _state = copy;
            }

            var actionResult = RecordAction("update", "Quantity", quantity: quantity);
            if (!actionResult.IsValid) return actionResult;
            return stateValidation;
        }

        // Delete quantity by Id
        public ValidationResult DeleteQuantity(Guid quantityId)
        {
            ValidationResult stateValidation;
            Quantity existing;
            lock (_lock)
            {
                var quantities = new System.Collections.Generic.List<Quantity>(_state.Quantities ?? new System.Collections.Generic.List<Quantity>());
                // find existing item to include in action
                existing = quantities.Find(q => q != null && q.Id == quantityId);
                if (existing == null)
                {
                    var result = new ValidationResult { IsValid = false, Errors = new System.Collections.Generic.List<string> { "Quantity not found" } };
                    return result;
                }
                var removed = quantities.RemoveAll(q => q != null && q.Id == quantityId);
                if (removed == 0)
                {
                    var result = new ValidationResult { IsValid = false, Errors = new System.Collections.Generic.List<string> { "Quantity not found" } };
                    return result;
                }
                var copy = new TakeoffState { Quantities = quantities, Measurements = new System.Collections.Generic.List<Measurement>(_state.Measurements ?? new System.Collections.Generic.List<Measurement>()) };
                stateValidation = _validator.ValidateTakeoffState(copy);
                if (!stateValidation.IsValid) return stateValidation;
                _state = copy;
            }

            var actionResult = RecordAction("delete", "Quantity", quantity: existing);
            if (!actionResult.IsValid) return actionResult;
            return stateValidation;
        }

        // Add a measurement
        public ValidationResult AddMeasurement(Measurement measurement)
        {
            if (measurement == null) throw new ArgumentNullException(nameof(measurement));
            ValidationResult stateValidation;
            lock (_lock)
            {
                var copy = new TakeoffState
                {
                    Quantities = new System.Collections.Generic.List<Quantity>(_state.Quantities ?? new System.Collections.Generic.List<Quantity>()),
                    Measurements = new System.Collections.Generic.List<Measurement>(_state.Measurements ?? new System.Collections.Generic.List<Measurement>())
                };
                copy.Measurements.Add(measurement);
                stateValidation = _validator.ValidateTakeoffState(copy);
                if (!stateValidation.IsValid) return stateValidation;
                _state = copy;
            }

            var actionResult = RecordAction("create", "Measurement", measurement: measurement);
            if (!actionResult.IsValid) return actionResult;
            return stateValidation;
        }

        // Update measurement by matching all properties? use index by Guid if present - Measurement class has no Id; use object identity via index
        public ValidationResult UpdateMeasurement(int index, Measurement measurement)
        {
            if (measurement == null) throw new ArgumentNullException(nameof(measurement));
            ValidationResult stateValidation;
            lock (_lock)
            {
                var measurements = new System.Collections.Generic.List<Measurement>(_state.Measurements ?? new System.Collections.Generic.List<Measurement>());
                if (index < 0 || index >= measurements.Count)
                {
                    var res = new ValidationResult { IsValid = false, Errors = new System.Collections.Generic.List<string> { "Measurement index out of range" } };
                    return res;
                }
                measurements[index] = measurement;
                var copy = new TakeoffState { Quantities = new System.Collections.Generic.List<Quantity>(_state.Quantities ?? new System.Collections.Generic.List<Quantity>()), Measurements = measurements };
                stateValidation = _validator.ValidateTakeoffState(copy);
                if (!stateValidation.IsValid) return stateValidation;
                _state = copy;
            }

            var actionResult = RecordAction("update", "Measurement", measurement: measurement);
            if (!actionResult.IsValid) return actionResult;
            return stateValidation;
        }

        // Delete measurement by index
        public ValidationResult DeleteMeasurement(int index)
        {
            ValidationResult stateValidation;
            Measurement removedItem;
            lock (_lock)
            {
                var measurements = new System.Collections.Generic.List<Measurement>(_state.Measurements ?? new System.Collections.Generic.List<Measurement>());
                if (index < 0 || index >= measurements.Count)
                {
                    var res = new ValidationResult { IsValid = false, Errors = new System.Collections.Generic.List<string> { "Measurement index out of range" } };
                    return res;
                }
                removedItem = measurements[index];
                measurements.RemoveAt(index);
                var copy = new TakeoffState { Quantities = new System.Collections.Generic.List<Quantity>(_state.Quantities ?? new System.Collections.Generic.List<Quantity>()), Measurements = measurements };
                stateValidation = _validator.ValidateTakeoffState(copy);
                if (!stateValidation.IsValid) return stateValidation;
                _state = copy;
            }

            var actionResult = RecordAction("delete", "Measurement", measurement: removedItem);
            if (!actionResult.IsValid) return actionResult;
            return stateValidation;
        }

        // Existing generator method returns a validated default state or throws
        public TakeoffState GetDefaultTakeoffState()
        {
            // Create a static default state
            var quantities = new System.Collections.Generic.List<Quantity>();
            // Prepare 7 condition ids distributing 20 quantities: 3,3,3,3,3,3,2 = 20
            var quantityConditionIds = new System.Collections.Generic.List<Guid>();
            for (int ci = 0; ci < 7; ci++) quantityConditionIds.Add(Guid.NewGuid());

            int[] perCondition = new[] { 3, 3, 3, 3, 3, 3, 2 };

            // Name pools by UOM
            var linearNames = new System.Collections.Generic.Queue<string>(new[] { "Length", "Width", "Total Height", "Rim Length", "Border Length", "Trim Length", "Base Length" });
            var areaNames = new System.Collections.Generic.Queue<string>(new[] { "Surface Area", "Floor Area", "Wall Area", "Ceiling Area", "Roof Area", "Window Area", "Door Area" });
            var countNames = new System.Collections.Generic.Queue<string>(new[] { "Windows Count", "Doors Count", "Plugs Count", "Fixtures Count", "Items Count", "Units Count", "Pieces Count" });

            for (int ci = 0; ci < quantityConditionIds.Count; ci++)
            {
                var condId = quantityConditionIds[ci];
                var count = perCondition[ci];

                // For each condition, pick names so they match UOMs and are unique within the condition
                for (int j = 0; j < count; j++)
                {
                    // Choose UOM by a simple rotation to get a mix, but names will match the chosen UOM
                    var uoms = new[] { "lf", "sf", "ea" };
                    var uom = uoms[(ci + j) % uoms.Length];

                    double value;
                    string baseName;

                    if (string.Equals(uom, "ea", StringComparison.OrdinalIgnoreCase))
                    {
                        // For 'each' units, value must be integer
                        value = (ci + 1) * 10 + j; // integer
                        baseName = countNames.Count > 0 ? countNames.Dequeue() : $"Count{ci + 1}-{j + 1}";
                    }
                    else if (string.Equals(uom, "sf", StringComparison.OrdinalIgnoreCase))
                    {
                        value = Math.Round((ci + 1) * 10 + j + 0.5, 2);
                        baseName = areaNames.Count > 0 ? areaNames.Dequeue() : $"Area{ci + 1}-{j + 1}";
                    }
                    else
                    {
                        // lf
                        value = Math.Round((ci + 1) * 10 + j + 0.5, 2);
                        baseName = linearNames.Count > 0 ? linearNames.Dequeue() : $"Length{ci + 1}-{j + 1}";
                    }

                    var qty = new Quantity
                    {
                        Id = Guid.NewGuid(),
                        Name = baseName,
                        Uom = uom,
                        ConditionId = condId,
                        Value = value
                    };

                    quantities.Add(qty);
                }
            }

            // Create measurements: ensure conditions use a mix of types (Area, Linear, Count)
            var measurements = new System.Collections.Generic.List<Measurement>();

            for (int idx = 0; idx < quantityConditionIds.Count; idx++)
            {
                var condId = quantityConditionIds[idx];

                if (idx < 3)
                {
                    // Area condition: add Area + Perimeter
                    double areaVal = Math.Round(80.0 + idx * 11.37 + 1.23, 2);
                    double perimVal = Math.Round(30.0 + idx * 4.29 + 0.77, 2);

                    measurements.Add(new Measurement
                    {
                        MeasurementType = "Area",
                        ConditionType = "Area",
                        Uom = "sf",
                        ConditionId = condId,
                        Value = areaVal
                    });

                    measurements.Add(new Measurement
                    {
                        MeasurementType = "Perimeter",
                        ConditionType = "Area",
                        Uom = "lf",
                        ConditionId = condId,
                        Value = perimVal
                    });
                }
                else if (idx >= 3 && idx < 5)
                {
                    // Linear condition: single Length measurement
                    double lengthVal = Math.Round(12.0 + idx * 2.5 + 0.34, 2);
                    measurements.Add(new Measurement
                    {
                        MeasurementType = "Length",
                        ConditionType = "Linear",
                        Uom = "lf",
                        ConditionId = condId,
                        Value = lengthVal
                    });
                }
                else
                {
                    // Count condition: single Count measurement with integer value
                    int cnt = 5 + idx;
                    measurements.Add(new Measurement
                    {
                        MeasurementType = "Count",
                        ConditionType = "Count",
                        Uom = "ea",
                        ConditionId = condId,
                        Value = cnt
                    });
                }
            }

            var state = new TakeoffState
            {
                Quantities = quantities,
                Measurements = measurements
            };

            // Validate state
            var validationResult = _validator.ValidateTakeoffState(state);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException("Default TakeoffState is invalid: " + string.Join("; ", validationResult.Errors ?? new System.Collections.Generic.List<string>()));
            }

            return state;
        }
    }
}
