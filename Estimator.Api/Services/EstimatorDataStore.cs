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

        public void ReplaceAllProjects(List<Condition> allConditions)
        {
            lock (_gate)
            {
                _data.Clear();
                foreach (var cond in allConditions)
                {
                    if (!_data.TryGetValue(cond.ProjectId, out var list))
                    {
                        list = new List<Condition>();
                        _data[cond.ProjectId] = list;
                    }
                    list.Add(Clone(cond));
                }
            }
        }

        public List<Condition> UpsertByCallback(List<Condition> changedList)
        {
            lock (_gate)
            {
                var result = new List<Condition>();
                foreach (var changed in changedList)
                {
                    if (changed == null) continue;
                    if (changed.ProjectId == Guid.Empty) continue;
                    changed.Documents ??= new List<Document>();

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
                        result.Add(Clone(copy));
                        continue;
                    }

                    // Merge documents: update or add by Id
                    foreach (var doc in changed.Documents)
                    {
                        var exDoc = existing.Documents.FirstOrDefault(d => d.Id == doc.Id);
                        if (exDoc is null)
                        {
                            existing.Documents.Add(Clone(doc));
                        }
                        else
                        {
                            // simple replace of document summary and pages for changed doc
                            exDoc.DocumentSummary = doc.DocumentSummary?.Select(q => Clone(q)).ToList() ?? new List<Quantity>();

                            // merge pages by id
                            doc.Pages ??= new List<Page>();
                            foreach (var p in doc.Pages)
                            {
                                var exPage = exDoc.Pages.FirstOrDefault(pp => pp.Id == p.Id);
                                if (exPage is null) exDoc.Pages.Add(Clone(p));
                                else
                                {
                                    exPage.PageSummary = p.PageSummary?.Select(q => Clone(q)).ToList() ?? new List<Quantity>();
                                    // merge takeoff zones on page level
                                    p.TakeoffZones ??= new List<TakeoffZone>();
                                    foreach (var tz in p.TakeoffZones)
                                    {
                                        var exTz = exPage.TakeoffZones.FirstOrDefault(t => t.Id == tz.Id);
                                        if (exTz is null) exPage.TakeoffZones.Add(Clone(tz));
                                        else exTz.ZoneSummary = tz.ZoneSummary?.Select(q => Clone(q)).ToList() ?? new List<Quantity>();
                                    }
                                }
                            }
                        }
                    }

                    // Update the condition's project summary from callback (Takeoff has already computed it)
                    existing.ProjectSummary = changed.ProjectSummary?.Select(q => Clone(q)).ToList() ?? new List<Quantity>();

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
                var idx = list.FindIndex(x => x.Id == conditionId);
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
                    if (cond.Documents != null)
                    {
                        var idx = cond.Documents.FindIndex(d => d.Id == documentId);
                        if (idx >= 0)
                        {
                            cond.Documents.RemoveAt(idx);
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
                    foreach (var doc in cond.Documents ?? new List<Document>())
                    {
                        if (doc.Pages != null)
                        {
                            var idx = doc.Pages.FindIndex(p => p.Id == pageId);
                            if (idx >= 0)
                            {
                                doc.Pages.RemoveAt(idx);
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
                    foreach (var doc in cond.Documents ?? new List<Document>())
                    {
                        foreach (var page in doc.Pages ?? new List<Page>())
                        {
                            if (page.TakeoffZones != null)
                            {
                                var idx = page.TakeoffZones.FindIndex(z => z.Id == zoneId);
                                if (idx >= 0)
                                {
                                    page.TakeoffZones.RemoveAt(idx);
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

        public List<Guid> GetProjectIds()
        {
            lock (_gate)
            {
                return _data.Keys.ToList();
            }
        }

        private Condition? GetInternal(Guid projectId, Guid conditionId)
        {
            if (!_data.TryGetValue(projectId, out var list)) return null;
            return list.FirstOrDefault(x => x.Id == conditionId);
        }

        private static Condition Clone(Condition src)
        {
            return new Condition
            {
                Id = src.Id,
                ProjectId = src.ProjectId,
                ProjectSummary = src.ProjectSummary?.Select(q => Clone(q)).ToList() ?? new List<Quantity>(),
                Documents = src.Documents?.Select(d => Clone(d)).ToList() ?? new List<Document>()
            };
        }

        private static Document Clone(Document src)
        {
            return new Document
            {
                Id = src.Id,
                DocumentSummary = src.DocumentSummary?.Select(q => Clone(q)).ToList() ?? new List<Quantity>(),
                Pages = src.Pages?.Select(p => Clone(p)).ToList() ?? new List<Page>()
            };
        }

        private static Page Clone(Page src)
        {
            return new Page
            {
                Id = src.Id,
                PageNumber = src.PageNumber,
                PageSummary = src.PageSummary?.Select(q => Clone(q)).ToList() ?? new List<Quantity>(),
                TakeoffZones = src.TakeoffZones?.Select(tz => Clone(tz)).ToList() ?? new List<TakeoffZone>()
            };
        }

        private static TakeoffZone Clone(TakeoffZone src)
        {
            return new TakeoffZone
            {
                Id = src.Id,
                ZoneSummary = src.ZoneSummary?.Select(q => Clone(q)).ToList() ?? new List<Quantity>()
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