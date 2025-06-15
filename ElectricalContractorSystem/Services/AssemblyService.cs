using System;
using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public class AssemblyService
    {
        private readonly DatabaseService _databaseService;
        
        public AssemblyService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }
        
        #region Assembly Management
        
        public AssemblyTemplate CreateAssembly(string code, string name, string category, string createdBy)
        {
            var assembly = new AssemblyTemplate
            {
                AssemblyCode = code,
                Name = name,
                Category = category,
                IsDefault = true,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedDate = DateTime.Now
            };
            
            _databaseService.SaveAssembly(assembly);
            return assembly;
        }
        
        public AssemblyTemplate CreateAssemblyVariant(int parentAssemblyId, string variantName, 
            List<(int priceListItemId, decimal quantity)> componentChanges, string createdBy)
        {
            var parentAssembly = _databaseService.GetAssemblyById(parentAssemblyId);
            if (parentAssembly == null) return null;
            
            // Create the variant
            var variant = parentAssembly.CreateVariant(variantName, createdBy);
            
            // Apply component changes
            foreach (var change in componentChanges)
            {
                var existingComponent = variant.Components
                    .FirstOrDefault(c => c.PriceListItemId == change.priceListItemId);
                    
                if (existingComponent != null)
                {
                    existingComponent.Quantity = change.quantity;
                }
            }
            
            _databaseService.SaveAssembly(variant);
            
            // Create variant relationship
            _databaseService.CreateAssemblyVariantRelationship(parentAssemblyId, variant.AssemblyId);
            
            return variant;
        }
        
        public List<AssemblyTemplate> GetAssembliesByCode(string code)
        {
            return _databaseService.GetAssemblyVariants(code);
        }
        
        public List<AssemblyTemplate> SearchAssemblies(string searchTerm)
        {
            var assemblies = _databaseService.GetAllAssemblies();
            
            if (string.IsNullOrWhiteSpace(searchTerm))
                return assemblies;
                
            var searchLower = searchTerm.ToLower();
            
            return assemblies.Where(a => 
                a.AssemblyCode.ToLower().Contains(searchLower) ||
                a.Name.ToLower().Contains(searchLower) ||
                (a.Description?.ToLower().Contains(searchLower) ?? false)
            ).ToList();
        }
        
        #endregion
        
        #region Component Management
        
        public void AddComponentToAssembly(int assemblyId, int priceListItemId, decimal quantity, string notes = null)
        {
            var component = new AssemblyComponent
            {
                AssemblyId = assemblyId,
                PriceListItemId = priceListItemId,
                Quantity = quantity,
                Notes = notes
            };
            
            _databaseService.SaveAssemblyComponent(component);
        }
        
        public void UpdateAssemblyComponent(int componentId, decimal newQuantity)
        {
            var component = _databaseService.GetAssemblyComponentById(componentId);
            if (component != null)
            {
                component.Quantity = newQuantity;
                _databaseService.UpdateAssemblyComponent(component);
            }
        }
        
        public void RemoveComponentFromAssembly(int componentId)
        {
            _databaseService.DeleteAssemblyComponent(componentId);
        }
        
        #endregion
        
        #region Labor Adjustments
        
        public AssemblyTemplate ApplyLaborAdjustment(int assemblyId, DifficultyPreset preset)
        {
            var assembly = _databaseService.GetAssemblyById(assemblyId);
            if (assembly == null || preset == null) return null;
            
            // Create a temporary copy with adjustments
            var adjustedAssembly = new AssemblyTemplate
            {
                AssemblyId = assembly.AssemblyId,
                AssemblyCode = assembly.AssemblyCode,
                Name = assembly.Name,
                Description = assembly.Description,
                Category = assembly.Category,
                RoughMinutes = (int)(assembly.RoughMinutes * preset.RoughMultiplier),
                FinishMinutes = (int)(assembly.FinishMinutes * preset.FinishMultiplier),
                ServiceMinutes = (int)(assembly.ServiceMinutes * preset.ServiceMultiplier),
                ExtraMinutes = (int)(assembly.ExtraMinutes * preset.ExtraMultiplier),
                Components = assembly.Components
            };
            
            return adjustedAssembly;
        }
        
        public List<DifficultyPreset> GetDifficultyPresets(string category = null)
        {
            var presets = _databaseService.GetAllDifficultyPresets();
            
            if (!string.IsNullOrEmpty(category))
            {
                presets = presets.Where(p => p.Category == category).ToList();
            }
            
            return presets.OrderBy(p => p.Category).ThenBy(p => p.SortOrder).ToList();
        }
        
        public List<DifficultyPreset> GetSuggestedPresets(Job job)
        {
            var suggestions = new List<DifficultyPreset>();
            var allPresets = GetDifficultyPresets();
            
            // Check address for beach/shore
            if (job.Address?.ToLower().Contains("beach") == true || 
                job.Address?.ToLower().Contains("shore") == true ||
                job.Address?.ToLower().Contains("ocean") == true)
            {
                var beachPreset = allPresets.FirstOrDefault(p => p.Name.Contains("Beach"));
                if (beachPreset != null) suggestions.Add(beachPreset);
            }
            
            // Check date for seasonal adjustments
            var jobDate = job.CreateDate;
            if (jobDate.Month == 12)
            {
                var decemberPreset = allPresets.FirstOrDefault(p => p.Name.Contains("December"));
                if (decemberPreset != null) suggestions.Add(decemberPreset);
            }
            else if (jobDate.Month >= 6 && jobDate.Month <= 8)
            {
                var summerPreset = allPresets.FirstOrDefault(p => p.Name.Contains("Summer Peak"));
                if (summerPreset != null) suggestions.Add(summerPreset);
            }
            
            // Check customer history for difficulty patterns
            var customerJobs = _databaseService.GetJobsByCustomer(job.CustomerId);
            var previousAdjustments = customerJobs
                .SelectMany(j => _databaseService.GetLaborAdjustmentsByJob(j.JobId))
                .Where(a => a.PresetId.HasValue)
                .GroupBy(a => a.PresetId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
                
            if (previousAdjustments != null)
            {
                var commonPreset = allPresets.FirstOrDefault(p => p.PresetId == previousAdjustments.Key);
                if (commonPreset != null && !suggestions.Contains(commonPreset))
                    suggestions.Add(commonPreset);
            }
            
            return suggestions;
        }
        
        #endregion
        
        #region Analysis and Patterns
        
        public Dictionary<string, AssemblyUsageStats> GetAssemblyUsageStatistics(DateTime? startDate = null, DateTime? endDate = null)
        {
            var stats = new Dictionary<string, AssemblyUsageStats>();
            
            // Get all estimates and jobs within date range
            var estimates = _databaseService.GetEstimatesInDateRange(startDate, endDate);
            
            foreach (var estimate in estimates)
            {
                foreach (var lineItem in estimate.LineItems.Where(li => !string.IsNullOrEmpty(li.ItemCode)))
                {
                    if (!stats.ContainsKey(lineItem.ItemCode))
                    {
                        stats[lineItem.ItemCode] = new AssemblyUsageStats
                        {
                            AssemblyCode = lineItem.ItemCode,
                            AssemblyName = lineItem.ItemDescription
                        };
                    }
                    
                    stats[lineItem.ItemCode].UsageCount++;
                    stats[lineItem.ItemCode].TotalQuantity += lineItem.Quantity;
                }
            }
            
            return stats;
        }
        
        public List<AssemblyVariantUsage> GetVariantUsageByCode(string assemblyCode)
        {
            var variants = GetAssembliesByCode(assemblyCode);
            var usageList = new List<AssemblyVariantUsage>();
            
            foreach (var variant in variants)
            {
                var usage = new AssemblyVariantUsage
                {
                    Assembly = variant,
                    UsageCount = _databaseService.GetAssemblyUsageCount(variant.AssemblyId),
                    LastUsedDate = _databaseService.GetAssemblyLastUsedDate(variant.AssemblyId),
                    IsDefault = variant.IsDefault
                };
                
                usageList.Add(usage);
            }
            
            return usageList.OrderByDescending(u => u.IsDefault)
                          .ThenByDescending(u => u.UsageCount)
                          .ToList();
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public class AssemblyUsageStats
    {
        public string AssemblyCode { get; set; }
        public string AssemblyName { get; set; }
        public int UsageCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal AverageQuantityPerUse => UsageCount > 0 ? TotalQuantity / UsageCount : 0;
    }
    
    public class AssemblyVariantUsage
    {
        public AssemblyTemplate Assembly { get; set; }
        public int UsageCount { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public bool IsDefault { get; set; }
    }
    
    #endregion
}
