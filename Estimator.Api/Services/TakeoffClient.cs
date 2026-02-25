using Contracts;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Estimator.Api.Services
{
    public class TakeoffClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<TakeoffClient> _logger;

        public TakeoffClient(HttpClient client, ILogger<TakeoffClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<List<Condition>> GetAllConditionsAsync(Guid projectId, CancellationToken ct = default)
        {
            try
            {
                var res = await _client.GetAsync($"api/demo/projects/{projectId}/conditions", ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Takeoff responded with {StatusCode} when requesting snapshot", res.StatusCode);
                    throw new HttpRequestException("Remote returned " + res.StatusCode);
                }
                var list = await res.Content.ReadFromJsonAsync<List<Condition>>(cancellationToken: ct);
                return list ?? new List<Condition>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get snapshot from Takeoff");
                throw;
            }
        }

        public async Task<List<Guid>> GetAllProjectIdsAsync(CancellationToken ct = default)
        {
            try
            {
                var res = await _client.GetAsync("api/demo/projects", ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Takeoff responded with {StatusCode} when requesting project IDs", res.StatusCode);
                    throw new HttpRequestException("Remote returned " + res.StatusCode);
                }
                var list = await res.Content.ReadFromJsonAsync<List<Guid>>(cancellationToken: ct);
                return list ?? new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get project IDs from Takeoff");
                throw;
            }
        }

        public async Task<List<Condition>> PullSnapshotAsync(CancellationToken ct = default)
        {
            try
            {
                // Step 1: Get all project IDs
                var projectIds = await GetAllProjectIdsAsync(ct);
                _logger.LogInformation("Pulled {ProjectCount} project IDs from Takeoff", projectIds.Count);

                // Step 2: Fetch conditions for each project
                var allConditions = new List<Condition>();
                foreach (var projectId in projectIds)
                {
                    var conditions = await GetAllConditionsAsync(projectId, ct);
                    allConditions.AddRange(conditions);
                    _logger.LogInformation("Pulled {ConditionCount} conditions for project {ProjectId}", conditions.Count, projectId);
                }

                _logger.LogInformation("Snapshot pull complete: {TotalConditions} total conditions from {ProjectCount} projects", 
                    allConditions.Count, projectIds.Count);
                return allConditions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pull snapshot from Takeoff");
                throw;
            }
        }

        public async Task<List<Condition>> PullProjectSnapshotAsync(Guid projectId, CancellationToken ct = default)
        {
            try
            {
                var conditions = await GetAllConditionsAsync(projectId, ct);
                _logger.LogInformation("Pulled {ConditionCount} conditions for project {ProjectId} after deletion", conditions.Count, projectId);
                return conditions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pull project snapshot from Takeoff");
                throw;
            }
        }

        public class ProjectSnapshot
        {
            public Guid ProjectId { get; set; }
            public List<Condition> Conditions { get; set; } = new();
        }
    }
}