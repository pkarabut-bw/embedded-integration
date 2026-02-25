using Contracts;

namespace Takeoff.Api.Services
{
    public class TakeoffDataStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, List<Condition>> _data = new();

        public TakeoffDataStore()
        {
            // Initialize with sample data
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            var projectId = Guid.NewGuid();
            
            // Define document/page/zone structure (shared IDs across conditions)
            var docIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var pageIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var zoneIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Define unique quantity sets for each condition
            var quantitySetsByCondition = new[]
            {
                // Condition 1: Linear dimensions (ft) + Area
                new (string Name, string Unit)[] 
                { 
                    ("Length", "ft"), 
                    ("Width", "ft"), 
                    ("Height", "ft"),
                    ("Area", "ft?")
                },
                // Condition 2: Volume + Surface area + Weight
                new (string Name, string Unit)[] 
                { 
                    ("Volume", "ft?"), 
                    ("SurfaceArea", "ft?"), 
                    ("Weight", "lbs"),
                    ("Density", "lbs/ft?")
                },
                // Condition 3: Distance + Time + Cost
                new (string Name, string Unit)[] 
                { 
                    ("Distance", "ft"), 
                    ("Time", "hours"), 
                    ("Rate", "ft/hr"),
                    ("Cost", "$")
                },
                // Condition 4: Inventory (Quantity + Unit Cost + Total Cost)
                new (string Name, string Unit)[] 
                { 
                    ("Quantity", "units"), 
                    ("UnitCost", "$"), 
                    ("TotalCost", "$"),
                    ("Markup", "%")
                }
            };

            // Create 4 conditions with shared structure but unique quantity names per condition
            var conditions = new List<Condition>();
            
            for (int condIdx = 0; condIdx < 4; condIdx++)
            {
                var quantitySet = quantitySetsByCondition[condIdx];
                var documents = new List<Document>();
                
                // Base values for this condition (to make values different per condition)
                double baseMultiplier = 50.0 + (condIdx * 25.0);
                
                // Document 1: 3 pages with zones
                documents.Add(new Document
                {
                    Id = docIds[0],
                    DocumentSummary = new(),
                    Pages = new List<Page>
                    {
                        new Page
                        {
                            Id = pageIds[0],
                            PageNumber = 1,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[0],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.0)
                                }
                            }
                        },
                        new Page
                        {
                            Id = pageIds[1],
                            PageNumber = 2,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[1],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.8)
                                },
                                new TakeoffZone
                                {
                                    Id = zoneIds[2],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.6)
                                }
                            }
                        },
                        new Page
                        {
                            Id = pageIds[2],
                            PageNumber = 3,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[3],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.9)
                                }
                            }
                        }
                    }
                });

                // Document 2: 3 pages with zones
                documents.Add(new Document
                {
                    Id = docIds[1],
                    DocumentSummary = new(),
                    Pages = new List<Page>
                    {
                        new Page
                        {
                            Id = pageIds[3],
                            PageNumber = 1,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[4],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.2)
                                }
                            }
                        },
                        new Page
                        {
                            Id = pageIds[4],
                            PageNumber = 2,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[5],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.1)
                                },
                                new TakeoffZone
                                {
                                    Id = zoneIds[6],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.7)
                                }
                            }
                        },
                        new Page
                        {
                            Id = pageIds[5],
                            PageNumber = 3,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[7],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.3)
                                }
                            }
                        }
                    }
                });

                // Document 3: 3 pages with zones
                documents.Add(new Document
                {
                    Id = docIds[2],
                    DocumentSummary = new(),
                    Pages = new List<Page>
                    {
                        new Page
                        {
                            Id = pageIds[6],
                            PageNumber = 1,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[8],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.95)
                                }
                            }
                        },
                        new Page
                        {
                            Id = pageIds[7],
                            PageNumber = 2,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[9],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.85)
                                },
                                new TakeoffZone
                                {
                                    Id = zoneIds[10],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.05)
                                }
                            }
                        },
                        new Page
                        {
                            Id = pageIds[8],
                            PageNumber = 3,
                            PageSummary = new(),
                            TakeoffZones = new List<TakeoffZone>
                            {
                                new TakeoffZone
                                {
                                    Id = zoneIds[11],
                                    ZoneSummary = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.15)
                                }
                            }
                        }
                    }
                });

                conditions.Add(new Condition
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    ProjectSummary = new(),
                    Documents = documents
                });
            }

            // Compute summaries for all conditions
            foreach (var cond in conditions)
            {
                ComputeSummaries(cond);
            }

            _data[projectId] = conditions;
        }

        private List<Quantity> GenerateZoneQuantities((string Name, string Unit)[] quantitySet, double baseMultiplier, double scaleFactor)
        {
            var quantities = new List<Quantity>();
            for (int i = 0; i < quantitySet.Length; i++)
            {
                var (name, unit) = quantitySet[i];
                double value = (baseMultiplier + (i * 10)) * scaleFactor;
                quantities.Add(new Quantity { Name = name, Unit = unit, Value = Math.Round(value, 2) });
            }
            return quantities;
        }

        private void ComputeSummaries(Condition condition)
        {
            // Compute page summaries from zones
            foreach (var doc in condition.Documents ?? new List<Document>())
            {
                foreach (var page in doc.Pages ?? new List<Page>())
                {
                    var quantities = new Dictionary<string, (string unit, double value)>();
                    foreach (var zone in page.TakeoffZones ?? new List<TakeoffZone>())
                    {
                        foreach (var q in zone.ZoneSummary ?? new List<Quantity>())
                        {
                            var key = q.Name + "|" + (q.Unit ?? "");
                            if (!quantities.ContainsKey(key))
                            {
                                quantities[key] = (q.Unit ?? "", 0);
                            }
                            quantities[key] = (quantities[key].unit, quantities[key].value + q.Value);
                        }
                    }
                    page.PageSummary = quantities.Select(kvp => new Quantity 
                    { 
                        Name = kvp.Key.Split('|')[0], 
                        Unit = kvp.Key.Split('|')[1], 
                        Value = kvp.Value.value 
                    }).ToList();
                }

                // Compute document summaries from pages
                var docQuantities = new Dictionary<string, (string unit, double value)>();
                foreach (var page in doc.Pages ?? new List<Page>())
                {
                    foreach (var q in page.PageSummary ?? new List<Quantity>())
                    {
                        var key = q.Name + "|" + (q.Unit ?? "");
                        if (!docQuantities.ContainsKey(key))
                        {
                            docQuantities[key] = (q.Unit ?? "", 0);
                        }
                        docQuantities[key] = (docQuantities[key].unit, docQuantities[key].value + q.Value);
                    }
                }
                doc.DocumentSummary = docQuantities.Select(kvp => new Quantity 
                { 
                    Name = kvp.Key.Split('|')[0], 
                    Unit = kvp.Key.Split('|')[1], 
                    Value = kvp.Value.value 
                }).ToList();
            }

            // Compute project summary from documents
            var projQuantities = new Dictionary<string, (string unit, double value)>();
            foreach (var doc in condition.Documents ?? new List<Document>())
            {
                foreach (var q in doc.DocumentSummary ?? new List<Quantity>())
                {
                    var key = q.Name + "|" + (q.Unit ?? "");
                    if (!projQuantities.ContainsKey(key))
                    {
                        projQuantities[key] = (q.Unit ?? "", 0);
                    }
                    projQuantities[key] = (projQuantities[key].unit, projQuantities[key].value + q.Value);
                }
            }
            condition.ProjectSummary = projQuantities.Select(kvp => new Quantity 
            { 
                Name = kvp.Key.Split('|')[0], 
                Unit = kvp.Key.Split('|')[1], 
                Value = kvp.Value.value 
            }).ToList();
        }

        private List<Document> CloneDocuments(List<Document> documents)
        {
            return documents.Select(d => new Document
            {
                Id = d.Id,
                DocumentSummary = new List<Quantity>(d.DocumentSummary),
                Pages = d.Pages.Select(p => new Page
                {
                    Id = p.Id,
                    PageNumber = p.PageNumber,
                    PageSummary = new List<Quantity>(p.PageSummary),
                    TakeoffZones = p.TakeoffZones.Select(z => new TakeoffZone
                    {
                        Id = z.Id,
                        ZoneSummary = new List<Quantity>(z.ZoneSummary)
                    }).ToList()
                }).ToList()
            }).ToList();
        }

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
                if (idx >= 0) 
                {
                    list[idx] = copy;
                }
                else 
                {
                    list.Add(copy);
                }
                // Recompute all summaries after update
                ComputeSummaries(copy);
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
                    // Recompute summaries after deletion
                    if (deleted)
                    {
                        ComputeSummaries(cond);
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
                    // Recompute summaries after deletion
                    if (deleted)
                    {
                        ComputeSummaries(cond);
                    }
                }
                return deleted;
            }
        }

        public bool DeleteZone(Guid projectId, Guid zoneId)
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
                    // Recompute summaries after deletion
                    if (deleted)
                    {
                        ComputeSummaries(cond);
                    }
                }
                return deleted;
            }
        }

        // Snapshot replacement (used by demo Pull flow)
        public void UpdateFromSnapshot(Guid projectId, List<Condition> snapshot)
        {
            lock (_gate)
            {
                _data[projectId] = snapshot.Select(Clone).ToList();
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
                ProjectSummary = src.ProjectSummary?.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList() ?? new List<Quantity>(),
                Documents = src.Documents?.Select(d => new Document { Id = d.Id, DocumentSummary = d.DocumentSummary?.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList() ?? new List<Quantity>(), Pages = d.Pages?.Select(p => new Page { Id = p.Id, PageNumber = p.PageNumber, PageSummary = p.PageSummary?.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList() ?? new List<Quantity>(), TakeoffZones = p.TakeoffZones?.Select(tz => new TakeoffZone { Id = tz.Id, ZoneSummary = tz.ZoneSummary?.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList() ?? new List<Quantity>() }).ToList() ?? new List<TakeoffZone>() }).ToList() ?? new List<Page>() }).ToList() ?? new List<Document>()
            };
        }
    }
}