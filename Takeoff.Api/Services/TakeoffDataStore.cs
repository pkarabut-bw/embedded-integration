using Contracts;

namespace Takeoff.Api.Services
{
    public class TakeoffDataStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, List<ProjectConditionQuantities>> _data = new();

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
            var conditions = new List<ProjectConditionQuantities>();
            
            for (int condIdx = 0; condIdx < 4; condIdx++)
            {
                var quantitySet = quantitySetsByCondition[condIdx];
                var documents = new List<DocumentConditionQuantities>();
                
                // Base values for this condition (to make values different per condition)
                double baseMultiplier = 50.0 + (condIdx * 25.0);
                
                // Document 1: 3 pages with zones
                documents.Add(new DocumentConditionQuantities
                {
                    DocumentId = docIds[0],
                    Quantities = new(),
                    PageConditionQuantities = new List<PageConditionQuantities>
                    {
                        new PageConditionQuantities
                        {
                            PageId = pageIds[0],
                            PageNumber = 1,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[0],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.0)
                                }
                            }
                        },
                        new PageConditionQuantities
                        {
                            PageId = pageIds[1],
                            PageNumber = 2,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[1],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.8)
                                },
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[2],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.6)
                                }
                            }
                        },
                        new PageConditionQuantities
                        {
                            PageId = pageIds[2],
                            PageNumber = 3,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[3],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.9)
                                }
                            }
                        }
                    }
                });

                // Document 2: 3 pages with zones
                documents.Add(new DocumentConditionQuantities
                {
                    DocumentId = docIds[1],
                    Quantities = new(),
                    PageConditionQuantities = new List<PageConditionQuantities>
                    {
                        new PageConditionQuantities
                        {
                            PageId = pageIds[3],
                            PageNumber = 1,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[4],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.2)
                                }
                            }
                        },
                        new PageConditionQuantities
                        {
                            PageId = pageIds[4],
                            PageNumber = 2,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[5],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.1)
                                },
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[6],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.7)
                                }
                            }
                        },
                        new PageConditionQuantities
                        {
                            PageId = pageIds[5],
                            PageNumber = 3,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[7],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.3)
                                }
                            }
                        }
                    }
                });

                // Document 3: 3 pages with zones
                documents.Add(new DocumentConditionQuantities
                {
                    DocumentId = docIds[2],
                    Quantities = new(),
                    PageConditionQuantities = new List<PageConditionQuantities>
                    {
                        new PageConditionQuantities
                        {
                            PageId = pageIds[6],
                            PageNumber = 1,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[8],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.95)
                                }
                            }
                        },
                        new PageConditionQuantities
                        {
                            PageId = pageIds[7],
                            PageNumber = 2,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[9],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 0.85)
                                },
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[10],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.05)
                                }
                            }
                        },
                        new PageConditionQuantities
                        {
                            PageId = pageIds[8],
                            PageNumber = 3,
                            Quantities = new(),
                            TakeoffZoneConditionQuantities = new List<TakeoffZoneConditionQuantities>
                            {
                                new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = zoneIds[11],
                                    Quantities = GenerateZoneQuantities(quantitySet, baseMultiplier, 1.15)
                                }
                            }
                        }
                    }
                });

                conditions.Add(new ProjectConditionQuantities
                {
                    ConditionId = Guid.NewGuid(),
                    ProjectId = projectId,
                    Quantities = new(),
                    DocumentConditionQuantities = documents
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

        private void ComputeSummaries(ProjectConditionQuantities condition)
        {
            // Compute page summaries from zones
            foreach (var doc in condition.DocumentConditionQuantities ?? new List<DocumentConditionQuantities>())
            {
                foreach (var page in doc.PageConditionQuantities ?? new List<PageConditionQuantities>())
                {
                    var quantities = new Dictionary<string, (string unit, double value)>();
                    foreach (var zone in page.TakeoffZoneConditionQuantities ?? new List<TakeoffZoneConditionQuantities>())
                    {
                        foreach (var q in zone.Quantities ?? new List<Quantity>())
                        {
                            var key = q.Name + "|" + (q.Unit ?? "");
                            if (!quantities.ContainsKey(key))
                            {
                                quantities[key] = (q.Unit ?? "", 0);
                            }
                            quantities[key] = (quantities[key].unit, quantities[key].value + q.Value);
                        }
                    }
                    page.Quantities = quantities.Select(kvp => new Quantity 
                    { 
                        Name = kvp.Key.Split('|')[0], 
                        Unit = kvp.Key.Split('|')[1], 
                        Value = kvp.Value.value 
                    }).ToList();
                }

                // Compute document summaries from pages
                var docQuantities = new Dictionary<string, (string unit, double value)>();
                foreach (var page in doc.PageConditionQuantities ?? new List<PageConditionQuantities>())
                {
                    foreach (var q in page.Quantities ?? new List<Quantity>())
                    {
                        var key = q.Name + "|" + (q.Unit ?? "");
                        if (!docQuantities.ContainsKey(key))
                        {
                            docQuantities[key] = (q.Unit ?? "", 0);
                        }
                        docQuantities[key] = (docQuantities[key].unit, docQuantities[key].value + q.Value);
                    }
                }
                doc.Quantities = docQuantities.Select(kvp => new Quantity 
                { 
                    Name = kvp.Key.Split('|')[0], 
                    Unit = kvp.Key.Split('|')[1], 
                    Value = kvp.Value.value 
                }).ToList();
            }

            // Compute project summary from documents
            var projQuantities = new Dictionary<string, (string unit, double value)>();
            foreach (var doc in condition.DocumentConditionQuantities ?? new List<DocumentConditionQuantities>())
            {
                foreach (var q in doc.Quantities ?? new List<Quantity>())
                {
                    var key = q.Name + "|" + (q.Unit ?? "");
                    if (!projQuantities.ContainsKey(key))
                    {
                        projQuantities[key] = (q.Unit ?? "", 0);
                    }
                    projQuantities[key] = (projQuantities[key].unit, projQuantities[key].value + q.Value);
                }
            }
            condition.Quantities = projQuantities.Select(kvp => new Quantity 
            { 
                Name = kvp.Key.Split('|')[0], 
                Unit = kvp.Key.Split('|')[1], 
                Value = kvp.Value.value 
            }).ToList();
        }

        /// <summary>
        /// Public wrapper to compute summaries on a condition.
        /// Used before computing diff to ensure summaries are correct.
        /// </summary>
        public void ComputeSummariesPublic(ProjectConditionQuantities condition)
        {
            ComputeSummaries(condition);
        }

        private List<DocumentConditionQuantities> CloneDocuments(List<DocumentConditionQuantities> documents)
        {
            return documents.Select(d => new DocumentConditionQuantities
            {
                DocumentId = d.DocumentId,
                Quantities = new List<Quantity>(d.Quantities),
                PageConditionQuantities = d.PageConditionQuantities.Select(p => new PageConditionQuantities
                {
                    PageId = p.PageId,
                    PageNumber = p.PageNumber,
                    Quantities = new List<Quantity>(p.Quantities),
                    TakeoffZoneConditionQuantities = p.TakeoffZoneConditionQuantities.Select(z => new TakeoffZoneConditionQuantities
                    {
                        TakeoffZoneId = z.TakeoffZoneId,
                        Quantities = new List<Quantity>(z.Quantities)
                    }).ToList()
                }).ToList()
            }).ToList();
        }

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

        public ProjectConditionQuantities Add(ProjectConditionQuantities condition)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(condition.ProjectId, out var list))
                {
                    list = new List<ProjectConditionQuantities>();
                    _data[condition.ProjectId] = list;
                }

                var copy = Clone(condition);
                list.Add(copy);
                return Clone(copy);
            }
        }

        public ProjectConditionQuantities Update(ProjectConditionQuantities condition)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(condition.ProjectId, out var list))
                {
                    list = new List<ProjectConditionQuantities>();
                    _data[condition.ProjectId] = list;
                }

                var idx = list.FindIndex(x => x.ConditionId == condition.ConditionId);
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
        public void UpdateFromSnapshot(Guid projectId, List<ProjectConditionQuantities> snapshot)
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

        /// <summary>
        /// Computes the differential update by comparing a new condition with the existing one.
        /// Only includes changed documents/pages/zones.
        /// </summary>
        public ProjectConditionQuantities ComputeDiff(ProjectConditionQuantities newCondition, Guid projectId, Guid conditionId)
        {
            lock (_gate)
            {
                if (!_data.TryGetValue(projectId, out var list)) 
                    return Clone(newCondition); // New project, return full condition

                var existingCondition = list.FirstOrDefault(x => x.ConditionId == conditionId);
                
                // If no existing condition, return full new condition
                if (existingCondition == null)
                    return Clone(newCondition);

                // Compute diff by comparing structures
                var diff = new ProjectConditionQuantities
                {
                    ConditionId = newCondition.ConditionId,
                    ProjectId = newCondition.ProjectId,
                    Quantities = Clone(newCondition.Quantities),
                    DocumentConditionQuantities = new List<DocumentConditionQuantities>()
                };

                // Find changed documents
                var existingDocMap = new Dictionary<Guid, DocumentConditionQuantities>();
                foreach (var existingDoc in existingCondition.DocumentConditionQuantities ?? new List<DocumentConditionQuantities>())
                {
                    existingDocMap[existingDoc.DocumentId] = existingDoc;
                }

                foreach (var newDoc in newCondition.DocumentConditionQuantities ?? new List<DocumentConditionQuantities>())
                {
                    var existingDoc = existingDocMap.ContainsKey(newDoc.DocumentId) ? existingDocMap[newDoc.DocumentId] : null;
                    var changedPages = new List<PageConditionQuantities>();

                    // Find changed pages within this document
                    var existingPageMap = new Dictionary<Guid, PageConditionQuantities>();
                    if (existingDoc != null)
                    {
                        foreach (var existingPage in existingDoc.PageConditionQuantities ?? new List<PageConditionQuantities>())
                        {
                            existingPageMap[existingPage.PageId] = existingPage;
                        }
                    }

                    foreach (var newPage in newDoc.PageConditionQuantities ?? new List<PageConditionQuantities>())
                    {
                        var existingPage = existingPageMap.ContainsKey(newPage.PageId) ? existingPageMap[newPage.PageId] : null;
                        var changedZones = new List<TakeoffZoneConditionQuantities>();

                        // Find changed zones within this page
                        var existingZoneMap = new Dictionary<Guid, TakeoffZoneConditionQuantities>();
                        if (existingPage != null)
                        {
                            foreach (var existingZone in existingPage.TakeoffZoneConditionQuantities ?? new List<TakeoffZoneConditionQuantities>())
                            {
                                existingZoneMap[existingZone.TakeoffZoneId] = existingZone;
                            }
                        }

                        foreach (var newZone in newPage.TakeoffZoneConditionQuantities ?? new List<TakeoffZoneConditionQuantities>())
                        {
                            var existingZone = existingZoneMap.ContainsKey(newZone.TakeoffZoneId) ? existingZoneMap[newZone.TakeoffZoneId] : null;
                            
                            // Check if zone summary changed
                            if (existingZone == null || !QuantitiesEqual(newZone.Quantities, existingZone.Quantities))
                            {
                                changedZones.Add(new TakeoffZoneConditionQuantities
                                {
                                    TakeoffZoneId = newZone.TakeoffZoneId,
                                    Quantities = Clone(newZone.Quantities)
                                });
                            }
                        }

                        // Include page only if it has changed zones or is new
                        if (changedZones.Any() || existingPage == null || !QuantitiesEqual(newPage.Quantities, existingPage.Quantities))
                        {
                            changedPages.Add(new PageConditionQuantities
                            {
                                PageId = newPage.PageId,
                                PageNumber = newPage.PageNumber,
                                Quantities = Clone(newPage.Quantities),
                                TakeoffZoneConditionQuantities = changedZones
                            });
                        }
                    }

                    // Include document only if it has changed pages or is new
                    if (changedPages.Any() || existingDoc == null || !QuantitiesEqual(newDoc.Quantities, existingDoc.Quantities))
                    {
                        diff.DocumentConditionQuantities.Add(new DocumentConditionQuantities
                        {
                            DocumentId = newDoc.DocumentId,
                            Quantities = Clone(newDoc.Quantities),
                            PageConditionQuantities = changedPages
                        });
                    }
                }

                return diff;
            }
        }

        private static bool QuantitiesEqual(List<Quantity>? list1, List<Quantity>? list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            var sorted1 = list1.OrderBy(q => q.Name).ThenBy(q => q.Unit).ToList();
            var sorted2 = list2.OrderBy(q => q.Name).ThenBy(q => q.Unit).ToList();

            for (int i = 0; i < sorted1.Count; i++)
            {
                var q1 = sorted1[i];
                var q2 = sorted2[i];
                if (q1.Name != q2.Name || q1.Unit != q2.Unit || !q1.Value.Equals(q2.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<Quantity> Clone(List<Quantity>? quantities)
        {
            if (quantities == null) return new();
            return quantities.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList();
        }

        private static ProjectConditionQuantities Clone(ProjectConditionQuantities src)
        {
            return new ProjectConditionQuantities
            {
                ConditionId = src.ConditionId,
                ProjectId = src.ProjectId,
                Quantities = src.Quantities?.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList() ?? new List<Quantity>(),
                DocumentConditionQuantities = src.DocumentConditionQuantities?.Select(d => new DocumentConditionQuantities { DocumentId = d.DocumentId, Quantities = d.Quantities?.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList() ?? new List<Quantity>(), PageConditionQuantities = d.PageConditionQuantities?.Select(p => new PageConditionQuantities { PageId = p.PageId, PageNumber = p.PageNumber, Quantities = p.Quantities?.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList() ?? new List<Quantity>(), TakeoffZoneConditionQuantities = p.TakeoffZoneConditionQuantities?.Select(tz => new TakeoffZoneConditionQuantities { TakeoffZoneId = tz.TakeoffZoneId, Quantities = tz.Quantities?.Select(q => new Quantity { Name = q.Name, Unit = q.Unit, Value = q.Value }).ToList() ?? new List<Quantity>() }).ToList() ?? new List<TakeoffZoneConditionQuantities>() }).ToList() ?? new List<PageConditionQuantities>() }).ToList() ?? new List<DocumentConditionQuantities>()
            };
        }
    }
}