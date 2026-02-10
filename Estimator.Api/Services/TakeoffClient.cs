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
    }
}