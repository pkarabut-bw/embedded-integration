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

        public async Task SendConditionChangedAsync(Condition condition, CancellationToken ct = default)
        {
            try
            {
                var res = await _client.PostAsJsonAsync("api/interactions/condition-changed", condition, ct);
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
    }
}