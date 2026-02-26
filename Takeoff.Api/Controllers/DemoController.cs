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
        public ActionResult<List<ProjectConditionQuantities>> GetAll(Guid projectId)
        {
            var list = _store.GetAll(projectId);
            return Ok(list);
        }

        [HttpGet("projects/{projectId:guid}/conditions/{conditionId:guid}")]
        public ActionResult<ProjectConditionQuantities> Get(Guid projectId, Guid conditionId)
        {
            var c = _store.Get(projectId, conditionId);
            if (c is null) return NotFound();
            return Ok(c);
        }

        [HttpPost("conditions")]
        public async Task<ActionResult<ProjectConditionQuantities>> Create([FromBody] ProjectConditionQuantities condition)
        {
            if (condition.ProjectId == Guid.Empty) return BadRequest("ProjectId required");
            if (condition.ConditionId == Guid.Empty) condition.ConditionId = Guid.NewGuid();

            var created = _store.Add(condition);
            // Fire-and-forget notification to Estimator (new conditions send full data)
            _ = _client.SendConditionChangedAsync(created);
            return Ok(created);
        }

        [HttpPut("conditions/{conditionId:guid}")]
        public async Task<ActionResult<ProjectConditionQuantities>> Update(Guid conditionId, [FromBody] ProjectConditionQuantities condition)
        {
            if (conditionId != condition.ConditionId) condition.ConditionId = conditionId;

            // Compute summaries on the incoming condition BEFORE computing diff
            _store.ComputeSummariesPublic(condition);
            
            // Compute diff BEFORE updating the store (compare new with existing)
            var diff = _store.ComputeDiff(condition, condition.ProjectId, condition.ConditionId);
            
            // Now update the store with the condition that has computed summaries
            var updated = _store.Update(condition);
            
            // Send the diff that was computed before the update
            _ = _client.SendConditionChangedAsync(diff);
            return Ok(updated);
        }

        [HttpDelete("projects/{projectId:guid}/conditions/{conditionId:guid}")]
        public async Task<IActionResult> Delete(Guid projectId, Guid conditionId)
        {
            var deleted = _store.Delete(projectId, conditionId);
            if (!deleted) return NotFound();
            // fire-and-forget notification to Estimator
            _ = _client.SendConditionDeletedAsync(projectId, conditionId);
            return NoContent();
        }

        [HttpDelete("projects/{projectId:guid}/documents/{documentId:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid projectId, Guid documentId)
        {
            var deleted = _store.DeleteDocument(projectId, documentId);
            if (!deleted) return NotFound();
            // fire-and-forget notification to Estimator with specific deletion endpoint
            _ = _client.SendDocumentDeletedAsync(projectId, Guid.Empty, documentId);
            return NoContent();
        }

        [HttpDelete("projects/{projectId:guid}/pages/{pageId:guid}")]
        public async Task<IActionResult> DeletePage(Guid projectId, Guid pageId)
        {
            var deleted = _store.DeletePage(projectId, pageId);
            if (!deleted) return NotFound();
            // fire-and-forget notification to Estimator with specific deletion endpoint
            _ = _client.SendPageDeletedAsync(projectId, Guid.Empty, Guid.Empty, pageId);
            return NoContent();
        }

        [HttpDelete("projects/{projectId:guid}/zones/{zoneId:guid}")]
        public async Task<IActionResult> DeleteZone(Guid projectId, Guid zoneId)
        {
            var deleted = _store.DeleteZone(projectId, zoneId);
            if (!deleted) return NotFound();
            // fire-and-forget notification to Estimator with specific deletion endpoint
            _ = _client.SendTakeoffZoneDeletedAsync(projectId, Guid.Empty, Guid.Empty, Guid.Empty, zoneId);
            return NoContent();
        }

        [HttpPost("guids")]
        public ActionResult<Guid> NewGuid()
        {
            return Ok(Guid.NewGuid());
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok("ok");

        public class ProjectRequest { public Guid ProjectId { get; set; } }

        public class ProjectDataDto 
        { 
            public Guid ProjectId { get; set; }
            public List<ProjectConditionQuantities> Conditions { get; set; } = new();
        }
    }
}