using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Validation
{
    public class TakeoffValidator
    {
        // Measurement type constants
        private const string MT_LENGTH = "Length";
        private const string MT_AREA = "Area";
        private const string MT_PERIMETER = "Perimeter";
        private const string MT_COUNT = "Count";

        // Condition type constants
        private const string CT_LINEAR = "Linear";
        private const string CT_AREA = "Area";
        private const string CT_COUNT = "Count";

        // UOM constants
        private const string UOM_LF = "lf";
        private const string UOM_SF = "sf";
        private const string UOM_EA = "ea";

        // Entity type constants
        private const string ENTITY_QUANTITY = "Quantity";
        private const string ENTITY_MEASUREMENT = "Measurement";

        // Action name constants
        private const string ACTION_CREATE = "create";
        private const string ACTION_UPDATE = "update";
        private const string ACTION_DELETE = "delete";

        // Allowed sets (case-insensitive)
        private static readonly HashSet<string> AllowedMeasurementTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            MT_LENGTH,
            MT_AREA,
            MT_PERIMETER,
            MT_COUNT
        };

        private static readonly HashSet<string> AllowedConditionTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            CT_LINEAR,
            CT_AREA,
            CT_COUNT
        };

        private static readonly HashSet<string> AllowedUoms = new(StringComparer.OrdinalIgnoreCase)
        {
            UOM_LF,
            UOM_SF,
            UOM_EA
        };

        private static readonly HashSet<string> AllowedEntityTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ENTITY_QUANTITY,
            ENTITY_MEASUREMENT
        };

        private static readonly HashSet<string> AllowedActionNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ACTION_CREATE,
            ACTION_UPDATE,
            ACTION_DELETE
        };

        private static readonly Dictionary<string, string> MeasurementToUom = new(StringComparer.OrdinalIgnoreCase)
        {
            {MT_LENGTH, UOM_LF},
            {MT_PERIMETER, UOM_LF},
            {MT_AREA, UOM_SF},
            {MT_COUNT, UOM_EA}
        };

        private static readonly Dictionary<string, string> ConditionToUom = new(StringComparer.OrdinalIgnoreCase)
        {
            {CT_LINEAR, UOM_LF},
            {CT_AREA, UOM_SF},
            {CT_COUNT, UOM_EA}
        };

        public ValidationResult ValidateQuantity(Contracts.Quantity quantity)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };

            if (quantity == null)
            {
                result.IsValid = false;
                result.Errors.Add("quantity is null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(quantity.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Name is required");
            }

            if (string.IsNullOrWhiteSpace(quantity.Uom))
            {
                result.IsValid = false;
                result.Errors.Add("Uom is required");
            }

            if (quantity.ConditionId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("ConditionId is required");
            }

            // If UOM indicates integer (each - 'ea'), the Value must be an integer
            if (!string.IsNullOrWhiteSpace(quantity.Uom) && string.Equals(quantity.Uom, UOM_EA, StringComparison.OrdinalIgnoreCase))
            {
                if (quantity.Value % 1 != 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("For UOM 'ea' the Value must be an integer");
                }
            }

            return result;
        }

        public ValidationResult ValidateMeasurement(Contracts.Measurement measurement)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };

            if (measurement == null)
            {
                result.IsValid = false;
                result.Errors.Add("measurement is null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(measurement.MeasurementType))
            {
                result.IsValid = false;
                result.Errors.Add("MeasurementType is required");
            }
            else if (!AllowedMeasurementTypes.Contains(measurement.MeasurementType))
            {
                result.IsValid = false;
                result.Errors.Add($"MeasurementType must be one of: {string.Join(", ", AllowedMeasurementTypes)}");
            }

            if (string.IsNullOrWhiteSpace(measurement.Uom))
            {
                result.IsValid = false;
                result.Errors.Add("Uom is required");
            }
            else if (!AllowedUoms.Contains(measurement.Uom))
            {
                result.IsValid = false;
                result.Errors.Add($"Uom must be one of: {string.Join(", ", AllowedUoms)}");
            }

            if (measurement.ConditionId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("ConditionId is required");
            }

            if (measurement.Value < 0)
            {
                result.IsValid = false;
                result.Errors.Add("Value must be non-negative");
            }

            // New rule: if measurement type is Count, value must be an integer (no tolerance)
            if (!string.IsNullOrWhiteSpace(measurement.MeasurementType) &&
                string.Equals(measurement.MeasurementType, MT_COUNT, StringComparison.OrdinalIgnoreCase))
            {
                if (measurement.Value % 1 != 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("For measurement type 'Count' the Value must be an integer");
                }
            }

            if (string.IsNullOrWhiteSpace(measurement.ConditionType))
            {
                result.IsValid = false;
                result.Errors.Add("ConditionType is required");
            }
            else if (!AllowedConditionTypes.Contains(measurement.ConditionType))
            {
                result.IsValid = false;
                result.Errors.Add($"ConditionType must be one of: {string.Join(", ", AllowedConditionTypes)}");
            }

            // Additional relation rules:
            if (!string.IsNullOrWhiteSpace(measurement.ConditionType) &&
                !string.IsNullOrWhiteSpace(measurement.MeasurementType))
            {
                if (string.Equals(measurement.ConditionType, CT_LINEAR, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(measurement.MeasurementType, MT_LENGTH, StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsValid = false;
                        result.Errors.Add("For condition type 'Linear' the measurement type must be 'Length'");
                    }
                }
                else if (string.Equals(measurement.ConditionType, CT_AREA, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(measurement.MeasurementType, MT_AREA, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(measurement.MeasurementType, MT_PERIMETER, StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsValid = false;
                        result.Errors.Add(
                            "For condition type 'Area' the measurement type must be 'Area' or 'Perimeter'");
                    }
                }
                else if (string.Equals(measurement.ConditionType, CT_COUNT, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(measurement.MeasurementType, MT_COUNT, StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsValid = false;
                        result.Errors.Add("For condition type 'Count' the measurement type must be 'Count'");
                    }
                }
            }

            // Relation logic: measurementType -> UOM, fallback to conditionType -> UOM
            if (!string.IsNullOrWhiteSpace(measurement.MeasurementType))
            {
                if (MeasurementToUom.TryGetValue(measurement.MeasurementType, out var expectedUomByMeasurement))
                {
                    if (!string.Equals(measurement.Uom, expectedUomByMeasurement, StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsValid = false;
                        result.Errors.Add(
                            $"Uom '{measurement.Uom}' does not match expected '{expectedUomByMeasurement}' for measurement type '{measurement.MeasurementType}'");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(measurement.ConditionType) && ConditionToUom.TryGetValue(measurement.ConditionType, out var expectedUomByCondition))
                {
                    if (!string.Equals(measurement.Uom, expectedUomByCondition, StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsValid = false;
                        result.Errors.Add(
                            $"Uom '{measurement.Uom}' does not match expected '{expectedUomByCondition}' for condition type '{measurement.ConditionType}'");
                    }
                }
            }

            return result;
        }

        public ValidationResult ValidateListOfQuantitiesForState(List<Contracts.Quantity> quantities)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };
            if (quantities == null)
            {
                result.IsValid = false;
                result.Errors.Add("quantities list is null");
                return result;
            }

            for (int i = 0; i < quantities.Count; i++)
            {
                var qtyResult = ValidateQuantity(quantities[i]);
                if (!qtyResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(qtyResult.Errors.Select(e => $"Quantity[{i}]: {e}"));
                }
            }

            // Check that no more than 3 quantities share the same ConditionId (ignore empty Guid)
            var grouped = quantities
                .Where(q => q != null && q.ConditionId != Guid.Empty)
                .GroupBy(q => q.ConditionId)
                .Select(g => new
                {
                    ConditionId = g.Key,
                    Count = g.Select(q => q?.Id ?? Guid.Empty).Where(id => id != Guid.Empty).Distinct().Count(),
                    Items = g.ToList()
                })
                .Where(x => x.Count > 3)
                .ToList();

            foreach (var g in grouped)
            {
                result.IsValid = false;
                result.Errors.Add($"ConditionId {g.ConditionId} is used by {g.Count} quantities; maximum allowed is 3");
            }

            // Check that quantities with the same ConditionId have unique names (case-insensitive, trimmed)
            var groupsByCondition = quantities
                .Where(q => q != null && q.ConditionId != Guid.Empty)
                .GroupBy(q => q.ConditionId);

            foreach (var grp in groupsByCondition)
            {
                // Group by trimmed, case-insensitive name
                var nameGroups = grp
                    .Where(q => !string.IsNullOrWhiteSpace(q.Name))
                    .GroupBy(q => q.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var ng in nameGroups)
                {
                    // Count distinct Quantity.Id values for this name. Only consider it a conflict
                    // if the same name is used by more than one distinct Id.
                    var count = ng.Select(q => q.Id).Count();
                    if (count > 1)
                    {
                        result.IsValid = false;
                        result.Errors.Add(
                            $"ConditionId {grp.Key} has duplicate quantity name '{ng.Key}' used by {count}");
                    }
                }
            }

            return result;
        }

        // For quantities extracted from actions
        public ValidationResult ValidateListOfQuantitiesForActions(List<Contracts.Quantity> quantities)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };
            if (quantities == null)
            {
                result.IsValid = false;
                result.Errors.Add("quantities list is null");
                return result;
            }

            for (int i = 0; i < quantities.Count; i++)
            {
                var qtyResult = ValidateQuantity(quantities[i]);
                if (!qtyResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(qtyResult.Errors.Select(e => $"Quantity[{i}]: {e}"));
                }
            }

            return result;
        }
        
        public ValidationResult ValidateListOfMeasurementsForState(List<Contracts.Measurement> measurements)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };
            if (measurements == null)
            {
                result.IsValid = false;
                result.Errors.Add("measurements list is null");
                return result;
            }

            for (int i = 0; i < measurements.Count; i++)
            {
                var measResult = ValidateMeasurement(measurements[i]);
                if (!measResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(measResult.Errors.Select(e => $"Measurement[{i}]: {e}"));
                }
            }

            // Group measurements by ConditionId (ignore empty Guid)
            var groups = measurements
                .Where(m => m != null && m.ConditionId != Guid.Empty)
                .GroupBy(m => m.ConditionId)
                .ToList();

            foreach (var grp in groups)
            {
                var condId = grp.Key;
                // Collect distinct condition types present for this ConditionId
                var condTypes = grp
                    .Select(m => (m?.ConditionType ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (condTypes.Count > 1)
                {
                    result.IsValid = false;
                    result.Errors.Add(
                        $"ConditionId {condId} contains multiple ConditionType values: {string.Join(", ", condTypes)}");
                    // continue to next group after reporting inconsistent condition types
                    continue;
                }

                var condType = condTypes.FirstOrDefault();
                int count = grp.Count();

                if (string.Equals(condType, CT_LINEAR, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(condType, CT_COUNT, StringComparison.OrdinalIgnoreCase))
                {
                    if (count > 1)
                    {
                        result.IsValid = false;
                        result.Errors.Add(
                            $"ConditionId {condId} with condition type '{condType}' must have at most one measurement but has {count}");
                    }
                }
                else if (string.Equals(condType, CT_AREA, StringComparison.OrdinalIgnoreCase))
                {
                    if (count > 2)
                    {
                        result.IsValid = false;
                        result.Errors.Add(
                            $"ConditionId {condId} with condition type 'Area' must have at most two measurements but has {count}");
                    }

                    // If there are up to two measurements, ensure their MeasurementType values are different
                    if (count == 2)
                    {
                        var measurementTypes = grp.Select(m => m.MeasurementType)
                            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        if (measurementTypes.Count != 2)
                        {
                            result.IsValid = false;
                            result.Errors.Add(
                                $"ConditionId {condId} with condition type 'Area' must have different measurement types for its two measurements");
                        }
                    }
                }
            }

            return result;
        }

        // For measurements extracted from actions
        public ValidationResult ValidateListOfMeasurementsForActions(List<Contracts.Measurement> measurements)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };
            if (measurements == null)
            {
                result.IsValid = false;
                result.Errors.Add("measurements list is null");
                return result;
            }

            for (int i = 0; i < measurements.Count; i++)
            {
                var measResult = ValidateMeasurement(measurements[i]);
                if (!measResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(measResult.Errors.Select(e => $"Measurement[{i}]: {e}"));
                }
            }

            return result;
        }

        public ValidationResult ValidateTakeoffState(Contracts.TakeoffState state)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };

            if (state == null)
            {
                result.IsValid = false;
                result.Errors.Add("TakeoffState is null");
                return result;
            }

            var quantitiesResult = ValidateListOfQuantitiesForState(state.Quantities);

            if (!quantitiesResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(quantitiesResult.Errors.Select(e => $"Quantities: {e}"));
            }

            // Check that Quantity.Id values are unique (ignore empty Guid)
            if (state.Quantities != null)
            {
                var idDuplicates = state.Quantities
                    .Where(q => q != null && q.Id != Guid.Empty)
                    .GroupBy(q => q.Id)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .Where(x => x.Count > 1)
                    .ToList();

                foreach (var d in idDuplicates)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Quantity Id {d.Id} is used by {d.Count} quantities; ids must be unique");
                }
            }

            var measurementsResult = ValidateListOfMeasurementsForState(state.Measurements);

            if (!measurementsResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(measurementsResult.Errors.Select(e => $"Measurements: {e}"));
            }

            return result;
        }

        public ValidationResult ValidateTakeoffAction(Contracts.TakeoffAction action)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };
            if (action == null)
            {
                result.IsValid = false;
                result.Errors.Add("TakeoffAction is null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(action.ActionName))
            {
                result.IsValid = false;
                result.Errors.Add("ActionName is required");
            }
            else if (!AllowedActionNames.Contains(action.ActionName))
            {
                result.IsValid = false;
                result.Errors.Add($"ActionName must be one of: {string.Join(", ", AllowedActionNames)}");
            }

            if (string.IsNullOrWhiteSpace(action.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add("EntityType is required");
            }
            else if (!AllowedEntityTypes.Contains(action.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add($"EntityType must be one of: {string.Join(", ", AllowedEntityTypes)}");
            }

            if (action.OrderNumber < 0)
            {
                result.IsValid = false;
                result.Errors.Add("OrderNumber must be non-negative");
            }

            // Enforce exclusivity: Quantity actions must carry Quantity only; Measurement actions must carry Measurement only
            var isQuantityEntity = !string.IsNullOrWhiteSpace(action.EntityType) && string.Equals(action.EntityType, ENTITY_QUANTITY, StringComparison.OrdinalIgnoreCase);
            var isMeasurementEntity = !string.IsNullOrWhiteSpace(action.EntityType) && string.Equals(action.EntityType, ENTITY_MEASUREMENT, StringComparison.OrdinalIgnoreCase);

            if (isQuantityEntity)
            {
                if (action.Quantity == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("EntityType 'Quantity' requires a non-null Quantity");
                }
                if (action.Measurement != null)
                {
                    result.IsValid = false;
                    result.Errors.Add("EntityType 'Quantity' requires Measurement to be null");
                }
            }
            else if (isMeasurementEntity)
            {
                if (action.Measurement == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("EntityType 'Measurement' requires a non-null Measurement");
                }
                if (action.Quantity != null)
                {
                    result.IsValid = false;
                    result.Errors.Add("EntityType 'Measurement' requires Quantity to be null");
                }
            }

            // Validate contained entities only if present (to avoid duplicate null errors)
            if (action.Measurement != null)
            {
                var measResult = ValidateMeasurement(action.Measurement);
                if (!measResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(measResult.Errors.Select(e => $"Measurement: {e}"));
                }
            }

            if (action.Quantity != null)
            {
                var qtyResult = ValidateQuantity(action.Quantity);
                if (!qtyResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(qtyResult.Errors.Select(e => $"Quantity: {e}"));
                }
            }

            return result;
        }

        public ValidationResult ValidateListOfActions(List<Contracts.TakeoffAction> actions)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };

            if (actions == null)
            {
                result.IsValid = false;
                result.Errors.Add("actions list is null");
                return result;
            }

            // Validate each action individually
            for (int i = 0; i < actions.Count; i++)
            {
                var act = actions[i];
                var actResult = ValidateTakeoffAction(act);
                if (!actResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(actResult.Errors.Select(e => $"Action[{i}]: {e}"));
                }
            }

            // Validate order numbers: must be >= 1, unique, and form a contiguous sequence 1..N
            var orderNumbers = actions.Select(a => a?.OrderNumber ?? -1).ToList();

            // Check for invalid (non-positive) numbers
            var invalidNumbers = orderNumbers
                .Select((num, idx) => new { Num = num, Index = idx })
                .Where(x => x.Num < 1)
                .ToList();

            if (invalidNumbers.Any())
            {
                result.IsValid = false;
                foreach (var inv in invalidNumbers)
                {
                    result.Errors.Add($"Action[{inv.Index}] has invalid OrderNumber {inv.Num}; must be >= 1");
                }
            }

            // Check for duplicates
            var duplicates = orderNumbers
                .Where(n => n >= 1)
                .GroupBy(n => n)
                .Select(g => new { Value = g.Key, Count = g.Count() })
                .Where(x => x.Count > 1)
                .ToList();

            if (duplicates.Any())
            {
                result.IsValid = false;
                foreach (var d in duplicates)
                {
                    result.Errors.Add($"OrderNumber {d.Value} is used {d.Count} times; order numbers must be unique");
                }
            }

            // Check contiguous sequence 1..N
            var positiveSet = orderNumbers.Where(n => n >= 1).Distinct().OrderBy(n => n).ToList();
            int expectedCount = actions.Count;
            var expectedSequence = Enumerable.Range(1, expectedCount).ToList();
            if (!positiveSet.SequenceEqual(expectedSequence))
            {
                result.IsValid = false;
                result.Errors.Add($"OrderNumbers must form a contiguous sequence from 1 to {expectedCount}");
            }

            // Extract quantities and measurements from actions and validate them
            var quantities = actions
                .Where(a => a != null && string.Equals(a.EntityType, ENTITY_QUANTITY, StringComparison.OrdinalIgnoreCase) && a.Quantity != null)
                .Select(a => a.Quantity)
                .ToList();

            var measurements = actions
                .Where(a => a != null && string.Equals(a.EntityType, ENTITY_MEASUREMENT, StringComparison.OrdinalIgnoreCase) && a.Measurement != null)
                .Select(a => a.Measurement)
                .ToList();

            var qtyResult = ValidateListOfQuantitiesForActions(quantities);
            if (!qtyResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(qtyResult.Errors.Select(e => $"Quantities: {e}"));
            }

            var measResult = ValidateListOfMeasurementsForActions(measurements);
            if (!measResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(measResult.Errors.Select(e => $"Measurements: {e}"));
            }

            return result;
        }

        public ValidationResult ValidateTakeoffActionsList(Contracts.TakeoffActionsList actionsList)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>()
            };

            if (actionsList == null)
            {
                result.IsValid = false;
                result.Errors.Add("TakeoffActionsList is null");
                return result;
            }

            if (actionsList.Actions == null)
            {
                result.IsValid = false;
                result.Errors.Add("Actions list is null");
                return result;
            }

            // Validate actions themselves (includes extracted quantities/measurements validation)
            var actionsListResult = ValidateListOfActions(actionsList.Actions);
            if (!actionsListResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(actionsListResult.Errors.Select(e => $"Actions: {e}"));
            }

            return result;
        }
    }
}
