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
        /// Accept a list of changed ConditionData items from Takeoff and apply them to Estimator state.
        /// </summary>
        [HttpPost("condition-changed")]
        public ActionResult<List<Condition>> ConditionChanged([FromBody] List<Condition> conditions)
        {
            if (conditions == null || !conditions.Any()) return BadRequest();
            var res = _store.UpsertByCallback(conditions);
            return Ok(res);
        }

        [HttpPost("condition-deleted")]
        public async Task<IActionResult> ConditionDeleted([FromBody] DeleteRequest req, CancellationToken ct = default)
        {
            if (req.ProjectId == Guid.Empty || req.ConditionId == Guid.Empty) return BadRequest();
            var deleted = _store.Delete(req.ProjectId, req.ConditionId);
            if (!deleted) return NotFound();
            
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

        [HttpPost("document-deleted")]
        public async Task<IActionResult> DocumentDeleted([FromBody] DocumentDeleteRequest req, CancellationToken ct = default)
        {
            if (req.ProjectId == Guid.Empty || req.DocumentId == Guid.Empty) return BadRequest();
            var deleted = _store.DeleteDocument(req.ProjectId, req.DocumentId);
            if (!deleted) return NotFound();
            
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

        [HttpPost("page-deleted")]
        public async Task<IActionResult> PageDeleted([FromBody] PageDeleteRequest req, CancellationToken ct = default)
        {
            if (req.ProjectId == Guid.Empty || req.PageId == Guid.Empty) return BadRequest();
            var deleted = _store.DeletePage(req.ProjectId, req.PageId);
            if (!deleted) return NotFound();
            
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

        [HttpPost("takeoffzone-deleted")]
        public async Task<IActionResult> TakeoffZoneDeleted([FromBody] TakeoffZoneDeleteRequest req, CancellationToken ct = default)
        {
            if (req.ProjectId == Guid.Empty || req.ZoneId == Guid.Empty) return BadRequest();
            var deleted = _store.DeleteTakeoffZone(req.ProjectId, req.ZoneId);
            if (!deleted) return NotFound();
            
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

        [HttpGet("health")]
        public IActionResult Health() => Ok("ok");

        public class DeleteRequest { public Guid ProjectId { get; set; } public Guid ConditionId { get; set; } }
        public class DocumentDeleteRequest { public Guid ProjectId { get; set; } public Guid DocumentId { get; set; } }
        public class PageDeleteRequest { public Guid ProjectId { get; set; } public Guid PageId { get; set; } }
        public class TakeoffZoneDeleteRequest { public Guid ProjectId { get; set; } public Guid ZoneId { get; set; } }
    }
}