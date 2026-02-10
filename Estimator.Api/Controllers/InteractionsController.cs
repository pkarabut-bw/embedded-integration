using Contracts;
using Estimator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estimator.Api.Controllers
{
    [ApiController]
    [Route("api/interactions")]
    public class InteractionsController : ControllerBase
    {
        private readonly TakeoffClient _client;
        private readonly EstimatorDataStore _store;

        public InteractionsController(TakeoffClient client, EstimatorDataStore store)
        {
            _client = client;
            _store = store;
        }

        [HttpPost("condition-changed")]
        public ActionResult<Condition> ConditionChanged([FromBody] Condition condition)
        {
            var res = _store.UpsertByCallback(condition);
            return Ok(res);
        }

        [HttpPost("condition-deleted")]
        public IActionResult ConditionDeleted([FromBody] DeleteRequest req)
        {
            if (req.ProjectId == Guid.Empty || req.ConditionId == Guid.Empty) return BadRequest();
            var deleted = _store.Delete(req.ProjectId, req.ConditionId);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok("ok");

        public class ProjectRequest { public Guid ProjectId { get; set; } }
        public class DeleteRequest { public Guid ProjectId { get; set; } public Guid ConditionId { get; set; } }
    }
}