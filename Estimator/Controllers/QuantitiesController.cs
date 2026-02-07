using System;
using System.Linq;
using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Estimator.StateManagement;

namespace Estimator.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class QuantitiesController : ControllerBase
    {
        private readonly ILogger<QuantitiesController> _logger;
        private readonly StateService _service;

        private const string EntityTypeQuantity = "Quantity";
        private const string EntityTypeMeasurement = "Measurement";
        private const string ActionCreate = "create";
        private const string ActionUpdate = "update";
        private const string ActionDelete = "delete";

        public QuantitiesController(ILogger<QuantitiesController> logger, StateService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost]
        public IActionResult PostQuantities([FromBody] TakeoffActionsList actionsList)
        {
            // Sort actions by OrderNumber
            var sortedActions = actionsList.Actions.OrderBy(action => action.OrderNumber);

            // Save information about quantities and measurements
            foreach (var action in sortedActions)
            {
                if (string.Equals(action.EntityType, EntityTypeQuantity, StringComparison.OrdinalIgnoreCase) && action.Quantity != null)
                {
                    // Save quantity information
                    HandleQuantityAction(action);
                }

                if (string.Equals(action.EntityType, EntityTypeMeasurement, StringComparison.OrdinalIgnoreCase) && action.Measurement != null)
                {
                    // Save measurement information
                    HandleMeasurementAction(action);
                }
            }

            return Ok(new { Message = "Quantities and measurements processed successfully!" });
        }

        // Provide a read-only endpoint returning the current TakeoffState stored in this service
        [HttpGet]
        public ActionResult<TakeoffState> GetState()
        {
            var state = _service.GetState();
            if (state == null) return NotFound();
            return Ok(state);
        }

        // Serve a minimal static viewer page
        [HttpGet]
        public ActionResult Viewer()
        {
            return PhysicalFile("wwwroot/state-viewer.html", "text/html");
        }

        private void HandleQuantityAction(TakeoffAction action)
        {
            switch (action.ActionName.ToLowerInvariant())
            {
                case ActionCreate:
                    CreateQuantity(action);
                    break;
                case ActionUpdate:
                    UpdateQuantity(action);
                    break;
                case ActionDelete:
                    DeleteQuantity(action);
                    break;
                default:
                    _logger.LogWarning("Unknown ActionName: {ActionName} for Quantity", action.ActionName);
                    break;
            }
        }

        private void CreateQuantity(TakeoffAction action)
        {
            _logger.LogInformation("Creating Quantity: [Id]: {Id}, [Name]: {Name}, [UOM]: {Uom}, [ConditionId]: {ConditionId}, [Value]: {Value}",
                action.Quantity.Id, action.Quantity.Name, action.Quantity.Uom, action.Quantity.ConditionId, action.Quantity.Value);
        }

        private void UpdateQuantity(TakeoffAction action)
        {
            _logger.LogInformation("Updating Quantity: [Id]: {Id}, [Name]: {Name}, [UOM]: {Uom}, [ConditionId]: {ConditionId}, [Value]: {Value}",
                action.Quantity.Id, action.Quantity.Name, action.Quantity.Uom, action.Quantity.ConditionId, action.Quantity.Value);
        }

        private void DeleteQuantity(TakeoffAction action)
        {
            _logger.LogInformation("Deleting Quantity: [Id]: {Id}", action.Quantity.Id);
        }

        private void HandleMeasurementAction(TakeoffAction action)
        {
            switch (action.ActionName.ToLowerInvariant())
            {
                case ActionCreate:
                    CreateMeasurement(action);
                    break;
                case ActionUpdate:
                    UpdateMeasurement(action);
                    break;
                case ActionDelete:
                    DeleteMeasurement(action);
                    break;
                default:
                    _logger.LogWarning("Unknown ActionName: {ActionName} for Measurement", action.ActionName);
                    break;
            }
        }

        private void CreateMeasurement(TakeoffAction action)
        {
            _logger.LogInformation("Creating Measurement: [MeasurementType]: {MeasurementType}, [ConditionType]: {ConditionType}, [UOM]: {Uom}, [ConditionId]: {ConditionId}, [Value]: {Value}",
                action.Measurement.MeasurementType, action.Measurement.ConditionType, action.Measurement.Uom, action.Measurement.ConditionId, action.Measurement.Value);
        }

        private void UpdateMeasurement(TakeoffAction action)
        {
            _logger.LogInformation("Updating Measurement: [MeasurementType]: {MeasurementType}, [ConditionType]: {ConditionType}, [UOM]: {Uom}, [ConditionId]: {ConditionId}, [Value]: {Value}",
                action.Measurement.MeasurementType, action.Measurement.ConditionType, action.Measurement.Uom, action.Measurement.ConditionId, action.Measurement.Value);
        }

        private void DeleteMeasurement(TakeoffAction action)
        {
            _logger.LogInformation("Deleting Measurement: [ConditionId]: {ConditionId}", action.Measurement.ConditionId);
        }
    }
}