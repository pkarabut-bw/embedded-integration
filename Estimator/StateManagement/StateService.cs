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

        // Flag indicating that state was updated and viewers should refresh
        private bool _needUpdate = false;

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

        // Apply a list of actions (already sorted by OrderNumber)
        public void ApplyActions(System.Collections.Generic.IEnumerable<TakeoffAction> actions)
        {
            if (actions == null) return;
            lock (_lock)
            {
                if (_state == null)
                {
                    _state = new TakeoffState { Quantities = new System.Collections.Generic.List<Quantity>(), Measurements = new System.Collections.Generic.List<Measurement>() };
                }

                bool anyChange = false;
                foreach (var action in actions)
                {
                    try
                    {
                        var changed = ApplyActionInternal(action);
                        if (changed) anyChange = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying action {ActionName} on entity {EntityType}", action?.ActionName, action?.EntityType);
                    }
                }

                if (anyChange)
                {
                    _needUpdate = true;
                }
            }
        }

        private bool ApplyActionInternal(TakeoffAction action)
        {
            if (action == null) return false;
            var entity = action.EntityType ?? string.Empty;
            if (string.Equals(entity, "Quantity", StringComparison.OrdinalIgnoreCase))
            {
                return ApplyQuantityAction(action);
            }
            else if (string.Equals(entity, "Measurement", StringComparison.OrdinalIgnoreCase))
            {
                return ApplyMeasurementAction(action);
            }
            else
            {
                _logger.LogWarning("Unknown entity type in action: {EntityType}", action.EntityType);
                return false;
            }
        }

        private bool ApplyQuantityAction(TakeoffAction action)
        {
            var qty = action.Quantity;
            if (qty == null) return false;
            if (_state.Quantities == null) _state.Quantities = new System.Collections.Generic.List<Quantity>();

            switch ((action.ActionName ?? string.Empty).ToLowerInvariant())
            {
                case "create":
                    if (!_state.Quantities.Exists(q => q != null && q.Id == qty.Id))
                    {
                        _state.Quantities.Add(qty);
                        _logger.LogInformation("Applied create Quantity {Id}", qty.Id);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Create Quantity skipped, Id already exists: {Id}", qty.Id);
                        return false;
                    }
                case "update":
                    var idx = _state.Quantities.FindIndex(q => q != null && q.Id == qty.Id);
                    if (idx >= 0)
                    {
                        _state.Quantities[idx] = qty;
                        _logger.LogInformation("Applied update Quantity {Id}", qty.Id);
                        return true;
                    }
                    else
                    {
                        // If not found, add
                        _state.Quantities.Add(qty);
                        _logger.LogInformation("Update Quantity added as new {Id}", qty.Id);
                        return true;
                    }
                case "delete":
                    var removed = _state.Quantities.RemoveAll(q => q != null && q.Id == qty.Id);
                    _logger.LogInformation("Applied delete Quantity {Id}, removed {Count}", qty.Id, removed);
                    return removed > 0;
                default:
                    _logger.LogWarning("Unknown action name for Quantity: {ActionName}", action.ActionName);
                    return false;
            }
        }

        private bool ApplyMeasurementAction(TakeoffAction action)
        {
            var m = action.Measurement;
            if (m == null) return false;
            if (_state.Measurements == null) _state.Measurements = new System.Collections.Generic.List<Measurement>();

            switch ((action.ActionName ?? string.Empty).ToLowerInvariant())
            {
                case "create":
                    _state.Measurements.Add(m);
                    _logger.LogInformation("Applied create Measurement for Condition {ConditionId}", m.ConditionId);
                    return true;
                case "update":
                    // find by ConditionId and MeasurementType
                    var idx = _state.Measurements.FindIndex(x => x != null && x.ConditionId == m.ConditionId && string.Equals(x.MeasurementType, m.MeasurementType, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        _state.Measurements[idx] = m;
                        _logger.LogInformation("Applied update Measurement for Condition {ConditionId}", m.ConditionId);
                        return true;
                    }
                    else
                    {
                        _state.Measurements.Add(m);
                        _logger.LogInformation("Update Measurement added as new for Condition {ConditionId}", m.ConditionId);
                        return true;
                    }
                case "delete":
                    var removed = _state.Measurements.RemoveAll(x => x != null && x.ConditionId == m.ConditionId && string.Equals(x.MeasurementType, m.MeasurementType, StringComparison.OrdinalIgnoreCase));
                    _logger.LogInformation("Applied delete Measurement for Condition {ConditionId}, removed {Count}", m.ConditionId, removed);
                    return removed > 0;
                default:
                    _logger.LogWarning("Unknown action name for Measurement: {ActionName}", action.ActionName);
                    return false;
            }
        }

        // Return current flag and clear it
        public bool GetAndClearNeedUpdate()
        {
            lock (_lock)
            {
                var val = _needUpdate;
                _needUpdate = false;
                return val;
            }
        }
    }
}
