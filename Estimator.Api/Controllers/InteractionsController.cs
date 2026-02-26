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

        /// <summary>
        /// Accept a list of changed Conditions from Takeoff. Each Condition object may contain only
        /// changed documents/pages/zones (as a diff) or full data for new conditions.
        /// </summary>
        [HttpPost("conditions-changed")]
        public ActionResult<List<ProjectConditionQuantities>> ConditionsChanged([FromBody] List<ProjectConditionQuantities> conditions)
        {
            if (conditions == null || !conditions.Any()) return BadRequest();
            var res = _store.UpsertByCallback(conditions);
            return Ok(res);
        }

        [HttpPost("conditions-deleted")]
        public async Task<IActionResult> ConditionsDeleted([FromBody] ConditionsDeleteRequest req, CancellationToken ct = default)
        {
            if (req.ProjectId == Guid.Empty || req.ConditionIds == null || !req.ConditionIds.Any()) return BadRequest();
            
            var allDeleted = true;
            foreach (var conditionId in req.ConditionIds)
            {
                var deleted = _store.Delete(req.ProjectId, conditionId);
                if (!deleted) allDeleted = false;
            }
            
            if (!allDeleted) return NotFound();
            
            // Pull snapshot for this project to ensure consistency after deletion
            try
            {
                var conditions = await _client.PullProjectSnapshotAsync(req.ProjectId, ct);
                _store.ReplaceAll(req.ProjectId, conditions);
            }
            catch (Exception ex)
            {
                // Log but don't fail - deletion was successful
                System.Diagnostics.Debug.WriteLine($"Failed to pull snapshot after deletion: {ex.Message}");
            }
            
            return NoContent();
        }

        [HttpPost("documents-deleted")]
        public async Task<IActionResult> DocumentsDeleted([FromBody] DocumentsDeleteRequest req, CancellationToken ct = default)
        {
            if (req.ProjectId == Guid.Empty || req.DocumentIds == null || !req.DocumentIds.Any()) return BadRequest();
            
            var allDeleted = true;
            foreach (var documentId in req.DocumentIds)
            {
                var deleted = _store.DeleteDocument(req.ProjectId, documentId);
                if (!deleted) allDeleted = false;
            }
            
            if (!allDeleted) return NotFound();
            
            // Pull snapshot for this project to ensure consistency after deletion
            try
            {
                var conditions = await _client.PullProjectSnapshotAsync(req.ProjectId, ct);
                _store.ReplaceAll(req.ProjectId, conditions);
            }
            catch (Exception ex)
            {
                // Log but don't fail - deletion was successful
                System.Diagnostics.Debug.WriteLine($"Failed to pull snapshot after deletion: {ex.Message}");
            }
            
            return NoContent();
        }

        [HttpPost("pages-deleted")]
        public async Task<IActionResult> PagesDeleted([FromBody] PagesDeleteRequest req, CancellationToken ct = default)
        {
            if (req.ProjectId == Guid.Empty || req.PageIds == null || !req.PageIds.Any()) return BadRequest();
            
            var allDeleted = true;
            foreach (var pageId in req.PageIds)
            {
                var deleted = _store.DeletePage(req.ProjectId, pageId);
                if (!deleted) allDeleted = false;
            }
            
            if (!allDeleted) return NotFound();
            
            // Pull snapshot for this project to ensure consistency after deletion
            try
            {
                var conditions = await _client.PullProjectSnapshotAsync(req.ProjectId, ct);
                _store.ReplaceAll(req.ProjectId, conditions);
            }
            catch (Exception ex)
            {
                // Log but don't fail - deletion was successful
                System.Diagnostics.Debug.WriteLine($"Failed to pull snapshot after deletion: {ex.Message}");
            }
            
            return NoContent();
        }

        [HttpPost("takeoffzones-deleted")]
        public async Task<IActionResult> TakeoffZonesDeleted([FromBody] TakeoffZonesDeleteRequest req, CancellationToken ct = default)
        {
            if (req.ProjectId == Guid.Empty || req.ZoneIds == null || !req.ZoneIds.Any()) return BadRequest();
            
            var allDeleted = true;
            foreach (var zoneId in req.ZoneIds)
            {
                var deleted = _store.DeleteTakeoffZone(req.ProjectId, zoneId);
                if (!deleted) allDeleted = false;
            }
            
            if (!allDeleted) return NotFound();
            
            // Pull snapshot for this project to ensure consistency after deletion
            try
            {
                var conditions = await _client.PullProjectSnapshotAsync(req.ProjectId, ct);
                _store.ReplaceAll(req.ProjectId, conditions);
            }
            catch (Exception ex)
            {
                // Log but don't fail - deletion was successful
                System.Diagnostics.Debug.WriteLine($"Failed to pull snapshot after deletion: {ex.Message}");
            }
            
            return NoContent();
        }

        [HttpPost("project-deleted")]
        public IActionResult ProjectDeleted([FromBody] ProjectDeleteRequest req)
        {
            if (req.ProjectId == Guid.Empty) return BadRequest();
            var deleted = _store.DeleteProject(req.ProjectId);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok("ok");

        public class ConditionsDeleteRequest { public Guid ProjectId { get; set; } public List<Guid> ConditionIds { get; set; } = new(); }
        public class DocumentsDeleteRequest { public Guid ProjectId { get; set; } public List<Guid> DocumentIds { get; set; } = new(); }
        public class PagesDeleteRequest { public Guid ProjectId { get; set; } public List<Guid> PageIds { get; set; } = new(); }
        public class TakeoffZonesDeleteRequest { public Guid ProjectId { get; set; } public List<Guid> ZoneIds { get; set; } = new(); }
        public class ProjectDeleteRequest { public Guid ProjectId { get; set; } }
    }
}