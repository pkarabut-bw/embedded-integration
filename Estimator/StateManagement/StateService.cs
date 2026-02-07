using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Contracts;
using Microsoft.Extensions.Logging;

namespace Estimator.StateManagement
{
    // Minimal StateService that fetches initial state from Takeoff and stores it in-memory
    public class StateService
    {
        private readonly HttpClient _client;
        private readonly ILogger<StateService> _logger;
        private TakeoffState _state;
        private readonly object _lock = new object();

        public StateService(HttpClient client, ILogger<StateService> logger)
        {
            _client = client;
            _logger = logger;
            // synchronous fetch of initial state during startup
            try
            {
                var task = InitializeAsync();
                task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // log the initialization failure
                _logger.LogError(ex, "Failed to initialize Takeoff state from remote service");
                // ignore errors; _state remains null
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                var res = await _client.GetAsync("/Takeoff/GetQuantities");
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Takeoff GetQuantities returned non-success status code: {StatusCode}", res.StatusCode);
                    return;
                }
                var json = await res.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var state = JsonSerializer.Deserialize<TakeoffState>(json, options);
                if (state == null)
                {
                    _logger.LogWarning("Takeoff GetQuantities returned no state (null payload)");
                    return;
                }
                lock (_lock)
                {
                    _state = state;
                }
                _logger.LogInformation("Initialized Takeoff state from remote service: {Quantities} quantities, {Measurements} measurements", _state.Quantities?.Count ?? 0, _state.Measurements?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching Takeoff state");
            }
        }

        public TakeoffState GetState()
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }
}
