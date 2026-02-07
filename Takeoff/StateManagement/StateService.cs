using System;
using Contracts;
using Validation;

namespace Takeoff.StateManagement
{
    public class StateService
    {
        private readonly TakeoffValidator _validator = new TakeoffValidator();

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
