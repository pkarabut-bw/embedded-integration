using Contracts;
using Estimator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estimator.Api.Controllers
{
    [ApiController]
    [Route("api/demo")]
    public class DemoController : ControllerBase
    {
        private readonly EstimatorDataStore _store;
        private readonly TakeoffClient _client;

        public DemoController(EstimatorDataStore store, TakeoffClient client)
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

        [HttpPost("snapshot/pull")]
        public async Task<ActionResult<List<Condition>>> Pull([FromBody] ProjectRequest req)
        {
            if (req.ProjectId == Guid.Empty) return BadRequest();
            var snapshot = await _client.GetAllConditionsAsync(req.ProjectId);
            _store.ReplaceAll(req.ProjectId, snapshot);
            return Ok(snapshot);
        }

        [HttpGet("projects/{projectId:guid}/conditions/{conditionId:guid}")]
        public ActionResult<Condition> Get(Guid projectId, Guid conditionId)
        {
            var c = _store.Get(projectId, conditionId);
            if (c is null) return NotFound();
            return Ok(c);
        }

        public class ProjectRequest { public Guid ProjectId { get; set; } }
    }
}