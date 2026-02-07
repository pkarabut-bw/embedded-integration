using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _estimatorBaseUrl;

        public StateController(StateService service, IHttpClientFactory clientFactory, IConfiguration config)
        {
            _service = service;
            _clientFactory = clientFactory;
            _estimatorBaseUrl = config["Estimator:BaseUrl"] ?? string.Empty;
        }

        [HttpGet]
        public ActionResult<TakeoffState> Get()
        {
            var state = _service.GetState();
            return Ok(state);
        }

        [HttpGet("actions")]
        public ActionResult<TakeoffActionsList> GetActions()
        {
            var actions = _service.GetActionsList();
            return Ok(actions);
        }

        [HttpPost("sendToEstimator")]
        public async Task<ActionResult> SendToEstimator()
        {
            if (string.IsNullOrWhiteSpace(_estimatorBaseUrl)) return BadRequest("Estimator base URL is not configured");
            var actions = _service.GetActionsList();
            if (actions == null) return BadRequest("No actions to send");

            var client = _clientFactory.CreateClient();
            try
            {
                client.BaseAddress = new Uri(_estimatorBaseUrl);
                var res = await client.PostAsJsonAsync("/Quantities/PostQuantities", actions);
                if (!res.IsSuccessStatusCode)
                {
                    var txt = await res.Content.ReadAsStringAsync();
                    return StatusCode((int)res.StatusCode, txt);
                }

                // Clear recorded actions after successful send
                _service.ClearActions();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
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

        // Generate a new ConditionId (GUID) for the client to use when creating quantities or measurements
        [HttpGet("condition/new")]
        public ActionResult<Guid> NewConditionId()
        {
            return Ok(Guid.NewGuid());
        }
    }
}
