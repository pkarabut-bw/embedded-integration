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
                changed.Metadata ??= new List<MeasurementMetadata>();
                changed.MeasurementValues ??= new List<MeasurementValue>();

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

                // replace metadata
                existing.Metadata = changed.Metadata.Select(m => new MeasurementMetadata
                {
                    MeasurementId = m.MeasurementId,
                    MeasurementName = m.MeasurementName,
                    UnitsOfMeasurements = m.UnitsOfMeasurements
                }).ToList();

                // merge measurement values
                foreach (var mv in changed.MeasurementValues)
                {
                    var exMv = existing.MeasurementValues.FirstOrDefault(x => x.MeasurementId == mv.MeasurementId);
                    if (exMv is null)
                    {
                        existing.MeasurementValues.Add(new MeasurementValue { MeasurementId = mv.MeasurementId, Value = mv.Value });
                    }
                    else
                    {
                        exMv.Value = mv.Value;
                    }
                }

                // ensure measurement values exist for all metadata
                foreach (var meta in existing.Metadata)
                {
                    if (!existing.MeasurementValues.Any(x => x.MeasurementId == meta.MeasurementId))
                    {
                        existing.MeasurementValues.Add(new MeasurementValue { MeasurementId = meta.MeasurementId, Value = 0 });
                    }
                }

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
                Metadata = src.Metadata?.Select(m => new MeasurementMetadata
                {
                    MeasurementId = m.MeasurementId,
                    MeasurementName = m.MeasurementName,
                    UnitsOfMeasurements = m.UnitsOfMeasurements
                }).ToList() ?? new List<MeasurementMetadata>(),
                MeasurementValues = src.MeasurementValues?.Select(v => new MeasurementValue
                {
                    MeasurementId = v.MeasurementId,
                    Value = v.Value
                }).ToList() ?? new List<MeasurementValue>()
            };
        }
    }
}