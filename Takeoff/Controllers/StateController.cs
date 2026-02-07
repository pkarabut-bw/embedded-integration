using Microsoft.AspNetCore.Mvc;
using Takeoff.StateManagement;
using Contracts;
using Validation;

namespace Takeoff.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StateController : ControllerBase
    {
        private readonly StateService _service;

        public StateController(StateService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<TakeoffState> Get()
        {
            var state = _service.GetState();
            return Ok(state);
        }

        [HttpPost("quantities")]
        public ActionResult AddQuantity([FromBody] Quantity quantity)
        {
            if (quantity == null) return BadRequest("Quantity is null");
            var validation = _service.AddQuantity(quantity);
            if (!validation.IsValid) return BadRequest(validation.Errors);
            return NoContent();
        }

        [HttpPut("quantities")]
        public ActionResult UpdateQuantity([FromBody] Quantity quantity)
        {
            if (quantity == null) return BadRequest("Quantity is null");
            var validation = _service.UpdateQuantity(quantity);
            if (!validation.IsValid) return BadRequest(validation.Errors);
            return NoContent();
        }

        [HttpDelete("quantities/{id}")]
        public ActionResult DeleteQuantity([FromRoute] Guid id)
        {
            var validation = _service.DeleteQuantity(id);
            if (!validation.IsValid) return BadRequest(validation.Errors);
            return NoContent();
        }

        [HttpPost("measurements")]
        public ActionResult AddMeasurement([FromBody] Measurement measurement)
        {
            if (measurement == null) return BadRequest("Measurement is null");
            var validation = _service.AddMeasurement(measurement);
            if (!validation.IsValid) return BadRequest(validation.Errors);
            return NoContent();
        }

        [HttpPut("measurements/{index}")]
        public ActionResult UpdateMeasurement([FromRoute] int index, [FromBody] Measurement measurement)
        {
            if (measurement == null) return BadRequest("Measurement is null");
            var validation = _service.UpdateMeasurement(index, measurement);
            if (!validation.IsValid) return BadRequest(validation.Errors);
            return NoContent();
        }

        [HttpDelete("measurements/{index}")]
        public ActionResult DeleteMeasurement([FromRoute] int index)
        {
            var validation = _service.DeleteMeasurement(index);
            if (!validation.IsValid) return BadRequest(validation.Errors);
            return NoContent();
        }

        [HttpPost("reset")]
        public ActionResult Reset()
        {
            var state = _service.ResetToDefault();
            return Ok(state);
        }
    }
}
