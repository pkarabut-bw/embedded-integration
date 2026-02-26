using Contracts;

namespace Estimator.Api.Services
{
    public class EstimatorDataStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, List<ProjectConditionQuantities>> _data = new();

        public IReadOnlyList<ProjectConditionQuantities> GetAll(Guid projectId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var list)) return new List<ProjectConditionQuantities>();
                return list.Select(Clone).ToList();
            }
        }

        public ProjectConditionQuantities? Get(Guid projectId, Guid conditionId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var list)) return null;
                var c = list.FirstOrDefault(x => x.ConditionId == conditionId);
                return c is null ? null : Clone(c);
            }
        }

        public void ReplaceAll(Guid projectId, List<ProjectConditionQuantities> snapshot)
        {
            lock (_gate)
            {
                _data[projectId] = snapshot.Select(Clone).ToList();
            }
        }

        public void ReplaceAllProjects(List<ProjectConditionQuantities> allConditions)
        {
            lock (_gate)
            {
                _data.Clear();
                foreach (var cond in allConditions)
                {
                    if (!_data.TryGetValue(cond.ProjectId, out var list))
                    {
                        list = new List<ProjectConditionQuantities>();
                        _data[cond.ProjectId] = list;
                    }
                    list.Add(Clone(cond));
                }
            }
        }

        public List<ProjectConditionQuantities> UpsertByCallback(List<ProjectConditionQuantities> changedList)
        {
            lock (_gate)
            {
                var result = new List<ProjectConditionQuantities>();
                foreach (var changed in changedList)
                {
                    if (changed == null) continue;
                    if (changed.ProjectId == Guid.Empty) continue;
                    changed.DocumentConditionQuantities ??= new List<DocumentConditionQuantities>();

                    if (!_data.TryGetValue(changed.ProjectId, out var list))
                    {
                        list = new List<ProjectConditionQuantities>();
                        _data[changed.ProjectId] = list;
                    }

                    var existing = list.FirstOrDefault(x => x.ConditionId == changed.ConditionId);
                    if (existing is null)
                    {
                        var copy = Clone(changed);
                        list.Add(copy);
                        result.Add(Clone(copy));
                        continue;
                    }

                    // Merge documents: update or add by Id
                    foreach (var doc in changed.DocumentConditionQuantities)
                    {
                        var exDoc = existing.DocumentConditionQuantities.FirstOrDefault(d => d.DocumentId == doc.DocumentId);
                        if (exDoc is null)
                        {
                            existing.DocumentConditionQuantities.Add(Clone(doc));
                        }
                        else
                        {
                            // simple replace of document summary and pages for changed doc
                            exDoc.Quantities = doc.Quantities?.Select(q => Clone(q)).ToList() ?? new List<Quantity>();

                            // merge pages by id
                            doc.PageConditionQuantities ??= new List<PageConditionQuantities>();
                            foreach (var p in doc.PageConditionQuantities)
                            {
                                var exPage = exDoc.PageConditionQuantities.FirstOrDefault(pp => pp.PageId == p.PageId);
                                if (exPage is null) exDoc.PageConditionQuantities.Add(Clone(p));
                                else
                                {
                                    exPage.Quantities = p.Quantities?.Select(q => Clone(q)).ToList() ?? new List<Quantity>();
                                    // merge takeoff zones on page level
                                    p.TakeoffZoneConditionQuantities ??= new List<TakeoffZoneConditionQuantities>();
                                    foreach (var tz in p.TakeoffZoneConditionQuantities)
                                    {
                                        var exTz = exPage.TakeoffZoneConditionQuantities.FirstOrDefault(t => t.TakeoffZoneId == tz.TakeoffZoneId);
                                        if (exTz is null) exPage.TakeoffZoneConditionQuantities.Add(Clone(tz));
                                        else exTz.Quantities = tz.Quantities?.Select(q => Clone(q)).ToList() ?? new List<Quantity>();
                                    }
                                }
                            }
                        }
                    }

                    // Update the condition's project summary from callback (Takeoff has already computed it)
                    existing.Quantities = changed.Quantities?.Select(q => Clone(q)).ToList() ?? new List<Quantity>();

                    result.Add(Clone(existing));
                }

                return result;
            }
        }

        public bool Delete(Guid projectId, Guid conditionId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var list)) return false;
                var idx = list.FindIndex(x => x.ConditionId == conditionId);
                if (idx < 0) return false;
                list.RemoveAt(idx);
                return true;
            }
        }

        public bool DeleteDocument(Guid projectId, Guid documentId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var conditions)) return false;
                bool deleted = false;
                foreach (var cond in conditions)
                {
                    if (cond.DocumentConditionQuantities != null)
                    {
                        var idx = cond.DocumentConditionQuantities.FindIndex(d => d.DocumentId == documentId);
                        if (idx >= 0)
                        {
                            cond.DocumentConditionQuantities.RemoveAt(idx);
                            deleted = true;
                        }
                    }
                }
                return deleted;
            }
        }

        public bool DeletePage(Guid projectId, Guid pageId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var conditions)) return false;
                bool deleted = false;
                foreach (var cond in conditions)
                {
                    foreach (var doc in cond.DocumentConditionQuantities ?? new List<DocumentConditionQuantities>())
                    {
                        if (doc.PageConditionQuantities != null)
                        {
                            var idx = doc.PageConditionQuantities.FindIndex(p => p.PageId == pageId);
                            if (idx >= 0)
                            {
                                doc.PageConditionQuantities.RemoveAt(idx);
                                deleted = true;
                            }
                        }
                    }
                }
                return deleted;
            }
        }

        public bool DeleteTakeoffZone(Guid projectId, Guid zoneId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var conditions)) return false;
                bool deleted = false;
                foreach (var cond in conditions)
                {
                    foreach (var doc in cond.DocumentConditionQuantities ?? new List<DocumentConditionQuantities>())
                    {
                        foreach (var page in doc.PageConditionQuantities ?? new List<PageConditionQuantities>())
                        {
                            if (page.TakeoffZoneConditionQuantities != null)
                            {
                                var idx = page.TakeoffZoneConditionQuantities.FindIndex(z => z.TakeoffZoneId == zoneId);
                                if (idx >= 0)
                                {
                                    page.TakeoffZoneConditionQuantities.RemoveAt(idx);
                                    deleted = true;
                                }
                            }
                        }
                    }
                }
                return deleted;
            }
        }

        // Obsolete methods kept for backward compatibility - use simplified versions above
        public bool DeleteDocument(Guid projectId, Guid conditionId, Guid documentId)
        {
            return DeleteDocument(projectId, documentId);
        }

        public bool DeletePage(Guid projectId, Guid conditionId, Guid documentId, Guid pageId)
        {
            return DeletePage(projectId, pageId);
        }

        public bool DeleteTakeoffZone(Guid projectId, Guid conditionId, Guid documentId, Guid pageId, Guid zoneId)
        {
            return DeleteTakeoffZone(projectId, zoneId);
        }

        public bool DeleteProject(Guid projectId)
        {
            lock (_gate)
            {
                return _data.Remove(projectId);
            }
        }

        public List<Guid> GetProjectIds()
        {
            lock (_gate)
            {
                return _data.Keys.ToList();
            }
        }

        private ProjectConditionQuantities? GetInternal(Guid projectId, Guid conditionId)
        {
            if (!_data.TryGetValue(projectId, out var list)) return null;
            return list.FirstOrDefault(x => x.ConditionId == conditionId);
        }

        private static ProjectConditionQuantities Clone(ProjectConditionQuantities src)
        {
            return new ProjectConditionQuantities
            {
                ConditionId = src.ConditionId,
                ProjectId = src.ProjectId,
                Quantities = src.Quantities?.Select(q => Clone(q)).ToList() ?? new List<Quantity>(),
                DocumentConditionQuantities = src.DocumentConditionQuantities?.Select(d => Clone(d)).ToList() ?? new List<DocumentConditionQuantities>()
            };
        }

        private static DocumentConditionQuantities Clone(DocumentConditionQuantities src)
        {
            return new DocumentConditionQuantities
            {
                DocumentId = src.DocumentId,
                Quantities = src.Quantities?.Select(q => Clone(q)).ToList() ?? new List<Quantity>(),
                PageConditionQuantities = src.PageConditionQuantities?.Select(p => Clone(p)).ToList() ?? new List<PageConditionQuantities>()
            };
        }

        private static PageConditionQuantities Clone(PageConditionQuantities src)
        {
            return new PageConditionQuantities
            {
                PageId = src.PageId,
                PageNumber = src.PageNumber,
                Quantities = src.Quantities?.Select(q => Clone(q)).ToList() ?? new List<Quantity>(),
                TakeoffZoneConditionQuantities = src.TakeoffZoneConditionQuantities?.Select(tz => Clone(tz)).ToList() ?? new List<TakeoffZoneConditionQuantities>()
            };
        }

        private static TakeoffZoneConditionQuantities Clone(TakeoffZoneConditionQuantities src)
        {
            return new TakeoffZoneConditionQuantities
            {
                TakeoffZoneId = src.TakeoffZoneId,
                Quantities = src.Quantities?.Select(q => Clone(q)).ToList() ?? new List<Quantity>()
            };
        }

        private static Quantity Clone(Quantity src)
        {
            return new Quantity
            {
                Name = src.Name,
                Unit = src.Unit,
                Value = src.Value
            };
        }
    }
}