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
        public async Task SendConditionChangedAsync(Condition condition, CancellationToken ct = default)
        {
            try
            {
                var res = await _client.PostAsJsonAsync("api/interactions/condition-changed", new List<Condition> { condition }, ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Estimator responded with {StatusCode} to condition-changed", res.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send condition-changed to Estimator");
            }
        }

        public async Task SendConditionDeletedAsync(Guid projectId, Guid conditionId, CancellationToken ct = default)
        {
            try
            {
                var payload = new { ProjectId = projectId, ConditionId = conditionId };
                var res = await _client.PostAsJsonAsync("api/interactions/condition-deleted", payload, ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Estimator responded with {StatusCode} to condition-deleted", res.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send condition-deleted to Estimator");
            }
        }

        public async Task SendDocumentDeletedAsync(Guid projectId, Guid conditionId, Guid documentId, CancellationToken ct = default)
        {
            try
            {
                var payload = new { ProjectId = projectId, DocumentId = documentId };
                var res = await _client.PostAsJsonAsync("api/interactions/document-deleted", payload, ct);
                if (!res.IsSuccessStatusCode) _logger.LogWarning("Estimator responded with {StatusCode} to document-deleted", res.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send document-deleted to Estimator");
            }
        }

        public async Task SendPageDeletedAsync(Guid projectId, Guid conditionId, Guid documentId, Guid pageId, CancellationToken ct = default)
        {
            try
            {
                var payload = new { ProjectId = projectId, PageId = pageId };
                var res = await _client.PostAsJsonAsync("api/interactions/page-deleted", payload, ct);
                if (!res.IsSuccessStatusCode) _logger.LogWarning("Estimator responded with {StatusCode} to page-deleted", res.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send page-deleted to Estimator");
            }
        }

        public async Task SendTakeoffZoneDeletedAsync(Guid projectId, Guid conditionId, Guid documentId, Guid pageId, Guid zoneId, CancellationToken ct = default)
        {
            try
            {
                var payload = new { ProjectId = projectId, ZoneId = zoneId };
                var res = await _client.PostAsJsonAsync("api/interactions/takeoffzone-deleted", payload, ct);
                if (!res.IsSuccessStatusCode) _logger.LogWarning("Estimator responded with {StatusCode} to takeoffzone-deleted", res.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send takeoffzone-deleted to Estimator");
            }
        }
    }
}