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

        public async Task<List<ProjectConditionQuantities>> GetAllConditionsAsync(Guid projectId, CancellationToken ct = default)
        {
            try
            {
                var res = await _client.GetAsync($"api/interactions/projects/{projectId}/conditions-all", ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Takeoff responded with {StatusCode} when requesting conditions", res.StatusCode);
                    throw new HttpRequestException("Remote returned " + res.StatusCode);
                }
                var list = await res.Content.ReadFromJsonAsync<List<ProjectConditionQuantities>>(cancellationToken: ct);
                return list ?? new List<ProjectConditionQuantities>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get conditions from Takeoff");
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

        public async Task<List<ProjectConditionQuantities>> PullSnapshotAsync(CancellationToken ct = default)
        {
            try
            {
                // Step 1: Get all project IDs from DemoController
                var projectIds = await GetAllProjectIdsAsync(ct);
                _logger.LogInformation("Pulled {ProjectCount} project IDs from Takeoff", projectIds.Count);

                // Step 2: Fetch conditions for each project from InteractionsController
                var allConditions = new List<ProjectConditionQuantities>();
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

        public async Task<List<ProjectConditionQuantities>> PullProjectSnapshotAsync(Guid projectId, CancellationToken ct = default)
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

        public class ProjectDataDto
        {
            public Guid ProjectId { get; set; }
            public List<ProjectConditionQuantities> Conditions { get; set; } = new();
        }
    }
}