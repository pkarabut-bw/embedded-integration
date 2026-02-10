using Contracts;

namespace Takeoff.Api.Services
{
    public class TakeoffDataStore
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

        public Condition Add(Condition condition)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(condition.ProjectId, out var list))
                {
                    list = new List<Condition>();
                    _data[condition.ProjectId] = list;
                }

                var copy = Clone(condition);
                list.Add(copy);
                return Clone(copy);
            }
        }

        public Condition Update(Condition condition)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(condition.ProjectId, out var list))
                {
                    list = new List<Condition>();
                    _data[condition.ProjectId] = list;
                }

                var idx = list.FindIndex(x => x.Id == condition.Id);
                var copy = Clone(condition);
                if (idx >= 0) list[idx] = copy; else list.Add(copy);
                return Clone(copy);
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