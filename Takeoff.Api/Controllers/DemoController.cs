using Contracts;
using Microsoft.AspNetCore.Mvc;
using Takeoff.Api.Services;

namespace Takeoff.Api.Controllers
{
    [ApiController]
    [Route("api/demo")]
    public class DemoController : ControllerBase
    {
        private readonly TakeoffDataStore _store;
        private readonly EstimatorClient _client;

        public DemoController(TakeoffDataStore store, EstimatorClient client)
        {
            _store = store;
            _client = client;
        }

        [HttpGet("projects")]
        public ActionResult<List<Guid>> GetProjects()
        {
            var ids = _store.GetProjectIds();
            return Ok(ids);
        }

        [HttpGet("projects/{projectId:guid}/conditions")]
        public ActionResult<List<Condition>> GetAll(Guid projectId)
        {
            var list = _store.GetAll(projectId);
            return Ok(list);
        }

        [HttpGet("projects/{projectId:guid}/conditions/{conditionId:guid}")]
        public ActionResult<Condition> Get(Guid projectId, Guid conditionId)
        {
            var c = _store.Get(projectId, conditionId);
            if (c is null) return NotFound();
            return Ok(c);
        }

        [HttpPost("conditions")]
        public async Task<ActionResult<Condition>> Create([FromBody] Condition condition)
        {
            if (condition.ProjectId == Guid.Empty) return BadRequest("ProjectId required");
            if (condition.Id == Guid.Empty) condition.Id = Guid.NewGuid();
            condition.Metadata ??= new List<MeasurementMetadata>();
            condition.MeasurementValues ??= new List<MeasurementValue>();

            var created = _store.Add(condition);
            _ = _client.SendConditionChangedAsync(created);
            return Ok(created);
        }

        [HttpPut("conditions/{conditionId:guid}")]
        public async Task<ActionResult<Condition>> Update(Guid conditionId, [FromBody] Condition condition)
        {
            if (conditionId != condition.Id) condition.Id = conditionId;
            condition.Metadata ??= new List<MeasurementMetadata>();
            condition.MeasurementValues ??= new List<MeasurementValue>();

            var updated = _store.Update(condition);
            _ = _client.SendConditionChangedAsync(updated);
            return Ok(updated);
        }

        [HttpDelete("projects/{projectId:guid}/conditions/{conditionId:guid}")]
        public async Task<IActionResult> Delete(Guid projectId, Guid conditionId)
        {
            var deleted = _store.Delete(projectId, conditionId);
            if (!deleted) return NotFound();
            _ = _client.SendConditionDeletedAsync(projectId, conditionId);
            return NoContent();
        }

        [HttpPost("guids")]
        public ActionResult<Guid> NewGuid()
        {
            return Ok(Guid.NewGuid());
        }
    }
}