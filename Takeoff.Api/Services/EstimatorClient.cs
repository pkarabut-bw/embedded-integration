using Contracts;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Takeoff.Api.Options;

namespace Takeoff.Api.Services
{
    public class EstimatorClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<EstimatorClient> _logger;

        public EstimatorClient(HttpClient client, ILogger<EstimatorClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Sends a condition (or diff) to Estimator.
        /// The Condition object acts as both full update and diff - 
        /// it contains only the changed documents/pages/zones when used as a diff.
        /// </summary>
        public async Task SendConditionChangedAsync(ProjectConditionQuantities condition, CancellationToken ct = default)
        {
            try
            {
                var res = await _client.PostAsJsonAsync("api/interactions/conditions-changed", new List<ProjectConditionQuantities> { condition }, ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Estimator responded with {StatusCode} to conditions-changed", res.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send conditions-changed to Estimator");
            }
        }

        public async Task SendConditionDeletedAsync(Guid projectId, Guid conditionId, CancellationToken ct = default)
        {
            try
            {
                var payload = new { ProjectId = projectId, ConditionIds = new List<Guid> { conditionId } };
                var res = await _client.PostAsJsonAsync("api/interactions/conditions-deleted", payload, ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Estimator responded with {StatusCode} to conditions-deleted", res.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send conditions-deleted to Estimator");
            }
        }

        public async Task SendDocumentDeletedAsync(Guid projectId, Guid conditionId, Guid documentId, CancellationToken ct = default)
        {
            try
            {
                var payload = new { ProjectId = projectId, DocumentIds = new List<Guid> { documentId } };
                var res = await _client.PostAsJsonAsync("api/interactions/documents-deleted", payload, ct);
                if (!res.IsSuccessStatusCode) _logger.LogWarning("Estimator responded with {StatusCode} to documents-deleted", res.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send documents-deleted to Estimator");
            }
        }

        public async Task SendPageDeletedAsync(Guid projectId, Guid conditionId, Guid documentId, Guid pageId, CancellationToken ct = default)
        {
            try
            {
                var payload = new { ProjectId = projectId, PageIds = new List<Guid> { pageId } };
                var res = await _client.PostAsJsonAsync("api/interactions/pages-deleted", payload, ct);
                if (!res.IsSuccessStatusCode) _logger.LogWarning("Estimator responded with {StatusCode} to pages-deleted", res.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send pages-deleted to Estimator");
            }
        }

        public async Task SendTakeoffZoneDeletedAsync(Guid projectId, Guid conditionId, Guid documentId, Guid pageId, Guid zoneId, CancellationToken ct = default)
        {
            try
            {
                var payload = new { ProjectId = projectId, ZoneIds = new List<Guid> { zoneId } };
                var res = await _client.PostAsJsonAsync("api/interactions/takeoffzones-deleted", payload, ct);
                if (!res.IsSuccessStatusCode) _logger.LogWarning("Estimator responded with {StatusCode} to takeoffzones-deleted", res.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send takeoffzones-deleted to Estimator");
            }
        }
    }
}