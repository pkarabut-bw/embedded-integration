using Contracts;

namespace Estimator.Api.Services
{
    public class EstimatorDataStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, List<Condition>> _data = new();

        public IReadOnlyList<Condition> GetAll(Guid projectId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var list)) return new List<Condition>();
                return list.Select(Clone).ToList();
            }
        }

        public Condition? Get(Guid projectId, Guid conditionId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var list)) return null;
                var c = list.FirstOrDefault(x => x.Id == conditionId);
                return c is null ? null : Clone(c);
            }
        }

        public void ReplaceAll(Guid projectId, List<Condition> snapshot)
        {
            lock (_gate)
            {
                _data[projectId] = snapshot.Select(Clone).ToList();
            }
        }

        public Condition UpsertByCallback(Condition changed)
        {
            lock (_gate)
            {
                changed.Measurements ??= new List<Measurement>();

                if (!_data.TryGetValue(changed.ProjectId, out var list))
                {
                    list = new List<Condition>();
                    _data[changed.ProjectId] = list;
                }

                var existing = list.FirstOrDefault(x => x.Id == changed.Id);
                if (existing is null)
                {
                    var copy = Clone(changed);
                    list.Add(copy);
                    return Clone(copy);
                }

                // Build a lookup of incoming measurements by name
                var incomingByName = changed.Measurements
                    .Where(m => !string.IsNullOrEmpty(m.MeasurementName))
                    .ToDictionary(m => m.MeasurementName, StringComparer.OrdinalIgnoreCase);

                // Create new list of measurements: for each incoming measurement, update existing if present, else add
                var newMeasurements = new List<Measurement>();
                foreach (var inc in changed.Measurements)
                {
                    if (string.IsNullOrEmpty(inc.MeasurementName)) continue;
                    var ex = existing.Measurements.FirstOrDefault(x => string.Equals(x.MeasurementName, inc.MeasurementName, StringComparison.OrdinalIgnoreCase));
                    if (ex is null)
                    {
                        newMeasurements.Add(new Measurement { MeasurementName = inc.MeasurementName, UnitsOfMeasurements = inc.UnitsOfMeasurements, Value = inc.Value });
                    }
                    else
                    {
                        // update value and units from incoming
                        ex.Value = inc.Value;
                        ex.UnitsOfMeasurements = inc.UnitsOfMeasurements ?? ex.UnitsOfMeasurements;
                        newMeasurements.Add(new Measurement { MeasurementName = ex.MeasurementName, UnitsOfMeasurements = ex.UnitsOfMeasurements, Value = ex.Value });
                    }
                }

                // Replace existing measurements with the new list - this removes any measurements not present in incoming
                existing.Measurements = newMeasurements;

                return Clone(existing);
            }
        }

        public bool Delete(Guid projectId, Guid conditionId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var list)) return false;
                var idx = list.FindIndex(x => x.Id == conditionId);
                if (idx < 0) return false;
                list.RemoveAt(idx);
                return true;
            }
        }

        public List<Guid> GetProjectIds()
        {
            lock (_gate)
            {
                return _data.Keys.ToList();
            }
        }

        private static Condition Clone(Condition src)
        {
            return new Condition
            {
                Id = src.Id,
                ProjectId = src.ProjectId,
                Measurements = src.Measurements?.Select(v => new Measurement
                {
                    MeasurementName = v.MeasurementName,
                    UnitsOfMeasurements = v.UnitsOfMeasurements,
                    Value = v.Value
                }).ToList() ?? new List<Measurement>()
            };
        }
    }
}