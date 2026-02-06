using System;
using Microsoft.AspNetCore.Mvc;
using Contracts;

namespace Takeoff.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TakeoffController : ControllerBase
    {
        [HttpGet]
        public ActionResult<TakeoffState> GetQuantities()
        {
            return GetRandom();
        }
        
        private ActionResult<TakeoffState> GetRandom()
        {
            var rnd = new Random();

            // Define semantic sets
            var measurementTypes = new[] { "Length", "Area", "Perimeter", "Count" };
            var conditionTypes = new[] { "Linear", "Area", "Count" };

            // Map measurement/condition types to appropriate UOMs
            var measurementToUom = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Length", "lf" },
                { "Perimeter", "lf" },
                { "Area", "sf" },
                { "Count", "ea" }
            };

            var conditionToUom = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Linear", "lf" },
                { "Area", "sf" },
                { "Count", "ea" }
            };

            // Map UOM to typical measurable quantity names
            var uomToNames = new System.Collections.Generic.Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "lf", new[] { "Length", "Perimeter", "Width", "Height", "Depth" } },
                { "sf", new[] { "Area", "Floor Area", "Surface Area", "Wall Area" } },
                { "ea", new[] { "Count", "Pieces", "Units", "Items" } }
            };

            // Generate required counts
            int quantitiesCount = rnd.Next(7, 16); // 7..15
            int measurementsCount = rnd.Next(3, 7); // 3..6

            var quantities = new System.Collections.Generic.List<Quantity>();
            var measurements = new System.Collections.Generic.List<Measurement>();

            // Create quantity instances
            for (int i = 0; i < quantitiesCount; i++)
            {
                var cond = conditionTypes[rnd.Next(conditionTypes.Length)];
                var uom = conditionToUom.TryGetValue(cond, out var cu) ? cu : "ea";

                // pick a name that matches the UOM
                string nameBase;
                if (uomToNames.TryGetValue(uom, out var nameOptions) && nameOptions.Length > 0)
                {
                    nameBase = nameOptions[rnd.Next(nameOptions.Length)];
                }
                else
                {
                    nameBase = "Quantity";
                }

                var qty = new Quantity
                {
                    Id = Guid.NewGuid(),
                    Name = $"{nameBase} {i + 1}",
                    Uom = uom,
                    ConditionId = Guid.NewGuid(),
                    Value = Math.Round(rnd.NextDouble() * 1000, 2)
                };

                quantities.Add(qty);
            }

            // Create measurement instances
            for (int i = 0; i < measurementsCount; i++)
            {
                var mType = measurementTypes[rnd.Next(measurementTypes.Length)];
                var cond = conditionTypes[rnd.Next(conditionTypes.Length)];

                var uom = measurementToUom.TryGetValue(mType, out var mu)
                    ? mu
                    : (conditionToUom.TryGetValue(cond, out var cu) ? cu : "ea");

                var meas = new Measurement
                {
                    MeasurementType = mType,
                    ConditionType = cond,
                    Uom = uom,
                    ConditionId = Guid.NewGuid(),
                    Value = Math.Round(rnd.NextDouble() * 1000, 2)
                };

                measurements.Add(meas);
            }

            var result = new TakeoffState
            {
                Quantities = quantities,
                Measurements = measurements
            };

            return Ok(result);
        }
    }
}
