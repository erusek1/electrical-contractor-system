using System;
using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Assembly service for managing electrical assemblies, components, and labor adjustments
    /// FIXED: String-to-int conversion errors and DateTime nullable issues
    /// </summary>
    public class AssemblyService
    {
        private readonly DatabaseService _databaseService;
        
        public AssemblyService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        
        #region Assembly Management
        
        /// <summary>
        /// Create a new assembly with the specified parameters
        /// </summary>
        public AssemblyTemplate CreateAssembly(string code, string name, string category, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Assembly code cannot be empty", nameof(code));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Assembly name cannot be empty", nameof(name));
        
            var assembly = new AssemblyTemplate
            {
                AssemblyCode = code,
                Name = name,
                Category = category ?? "General",
                IsDefault = true,
                IsActive = true,
                CreatedBy = createdBy ?? "System",
                CreatedDate = DateTime.Now
            };
            
            _databaseService.SaveAssembly(assembly);
            return assembly;
        }
        
        /// <summary>
        /// Create a variant of an existing assembly
        /// </summary>
        public AssemblyTemplate CreateAssemblyVariant(int parentAssemblyId, string variantName, 
            List<(int priceListItemId, decimal quantity)> componentChanges, string createdBy)
        {
            var parentAssembly = _databaseService.GetAssemblyById(parentAssemblyId);
            if (parentAssembly == null) 
            {
                throw new ArgumentException($"Parent assembly with ID {parentAssemblyId} not found");
            }
            
            // Create the variant
            var variant = parentAssembly.CreateVariant(variantName, createdBy);
            
            // Apply component changes
            if (componentChanges != null)
            {
                foreach (var change in componentChanges)
                {
                    var existingComponent = variant.Components?
                        .FirstOrDefault(c => c.PriceListItemId == change.priceListItemId);
                        
                    if (existingComponent != null)
                    {
                        existingComponent.Quantity = change.quantity;
                    }
                }
            }
            
            _databaseService.SaveAssembly(variant);
            
            // Create variant relationship
            _databaseService.CreateAssemblyVariantRelationship(parentAssemblyId, variant.AssemblyId);
            
            return variant;
        }
        
        /// <summary>
        /// Get assemblies by code - FIXED: Use proper method signature
        /// </summary>
        public List<AssemblyTemplate> GetAssembliesByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new List<AssemblyTemplate>();
                
            try
            {
                // FIXED: Get all assemblies and filter by code since DatabaseService doesn't have GetAssemblyVariants(string)
                var allAssemblies = _databaseService.GetAllAssemblies();
                return allAssemblies.Where(a => a.AssemblyCode.Equals(code, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAssembliesByCode: {ex.Message}");
                return new List<AssemblyTemplate>();
            }
        }
        
        /// <summary>
        /// Search assemblies by term
        /// </summary>
        public List<AssemblyTemplate> SearchAssemblies(string searchTerm)
        {
            try
            {
                var assemblies = _databaseService.GetAllAssemblies();
                
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return assemblies;
                    
                var searchLower = searchTerm.ToLower();
                
                return assemblies.Where(a => 
                    (a.AssemblyCode?.ToLower().Contains(searchLower) ?? false) ||
                    (a.Name?.ToLower().Contains(searchLower) ?? false) ||
                    (a.Description?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SearchAssemblies: {ex.Message}");
                return new List<AssemblyTemplate>();
            }
        }
        
        #endregion
        
        #region Component Management
        
        /// <summary>
        /// Add a component to an assembly
        /// </summary>
        public void AddComponentToAssembly(int assemblyId, int priceListItemId, decimal quantity, string notes = null)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddComponentToAssembly: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Update assembly component quantity
        /// </summary>
        public void UpdateAssemblyComponent(int componentId, decimal newQuantity)
        {
            try
            {
                var component = _databaseService.GetAssemblyComponentById(componentId);
                if (component != null)
                {
                    component.Quantity = newQuantity;
                    _databaseService.UpdateAssemblyComponent(component);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateAssemblyComponent: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Remove component from assembly
        /// </summary>
        public void RemoveComponentFromAssembly(int componentId)
        {
            try
            {
                _databaseService.DeleteAssemblyComponent(componentId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RemoveComponentFromAssembly: {ex.Message}");
                throw;
            }
        }
        
        #endregion
        
        #region Labor Adjustments
        
        /// <summary>
        /// Apply labor adjustment based on difficulty preset
        /// </summary>
        public AssemblyTemplate ApplyLaborAdjustment(int assemblyId, DifficultyPreset preset)
        {
            if (preset == null)
                return null;
                
            try
            {
                var assembly = _databaseService.GetAssemblyById(assemblyId);
                if (assembly == null) return null;
                
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyLaborAdjustment: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get difficulty presets by category
        /// </summary>
        public List<DifficultyPreset> GetDifficultyPresets(string category = null)
        {
            try
            {
                var presets = _databaseService.GetAllDifficultyPresets();
                
                if (!string.IsNullOrEmpty(category))
                {
                    presets = presets.Where(p => p.Category == category).ToList();
                }
                
                return presets.OrderBy(p => p.Category).ThenBy(p => p.SortOrder).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetDifficultyPresets: {ex.Message}");
                return new List<DifficultyPreset>();
            }
        }
        
        /// <summary>
        /// Get suggested presets based on job characteristics - FIXED: DateTime nullable issues
        /// </summary>
        public List<DifficultyPreset> GetSuggestedPresets(Job job)
        {
            if (job == null)
                return new List<DifficultyPreset>();
                
            try
            {
                var suggestions = new List<DifficultyPreset>();
                var allPresets = GetDifficultyPresets();
                
                // Check address for beach/shore
                if (!string.IsNullOrEmpty(job.Address))
                {
                    var addressLower = job.Address.ToLower();
                    if (addressLower.Contains("beach") || 
                        addressLower.Contains("shore") ||
                        addressLower.Contains("ocean"))
                    {
                        var beachPreset = allPresets.FirstOrDefault(p => p.Name.Contains("Beach"));
                        if (beachPreset != null) suggestions.Add(beachPreset);
                    }
                }
                
                // Check date for seasonal adjustments - FIXED: Proper DateTime handling
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
                if (customerJobs?.Any() == true)
                {
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
                }
                
                return suggestions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetSuggestedPresets: {ex.Message}");
                return new List<DifficultyPreset>();
            }
        }
        
        #endregion
        
        #region Analysis and Patterns
        
        /// <summary>
        /// Get assembly usage statistics for the specified date range
        /// </summary>
        public Dictionary<string, AssemblyUsageStats> GetAssemblyUsageStatistics(DateTime? startDate = null, DateTime? endDate = null)
        {
            var stats = new Dictionary<string, AssemblyUsageStats>();
            
            try
            {
                // Get all estimates within date range
                var estimates = _databaseService.GetEstimatesInDateRange(
                    startDate ?? DateTime.Now.AddYears(-1), 
                    endDate ?? DateTime.Now);
                
                if (estimates?.Any() == true)
                {
                    foreach (var estimate in estimates)
                    {
                        if (estimate.LineItems?.Any() == true)
                        {
                            foreach (var lineItem in estimate.LineItems.Where(li => !string.IsNullOrEmpty(li.ItemCode)))
                            {
                                if (!stats.ContainsKey(lineItem.ItemCode))
                                {
                                    stats[lineItem.ItemCode] = new AssemblyUsageStats
                                    {
                                        AssemblyCode = lineItem.ItemCode,
                                        AssemblyName = lineItem.ItemDescription ?? lineItem.ItemCode
                                    };
                                }
                                
                                stats[lineItem.ItemCode].UsageCount++;
                                stats[lineItem.ItemCode].TotalQuantity += lineItem.Quantity;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAssemblyUsageStatistics: {ex.Message}");
            }
            
            return stats;
        }
        
        /// <summary>
        /// Get variant usage by assembly code
        /// </summary>
        public List<AssemblyVariantUsage> GetVariantUsageByCode(string assemblyCode)
        {
            if (string.IsNullOrWhiteSpace(assemblyCode))
                return new List<AssemblyVariantUsage>();
                
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetVariantUsageByCode: {ex.Message}");
                return new List<AssemblyVariantUsage>();
            }
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    /// <summary>
    /// Statistics for assembly usage
    /// </summary>
    public class AssemblyUsageStats
    {
        public string AssemblyCode { get; set; }
        public string AssemblyName { get; set; }
        public int UsageCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal AverageQuantityPerUse => UsageCount > 0 ? TotalQuantity / UsageCount : 0;
    }
    
    /// <summary>
    /// Usage information for assembly variants
    /// </summary>
    public class AssemblyVariantUsage
    {
        public AssemblyTemplate Assembly { get; set; }
        public int UsageCount { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public bool IsDefault { get; set; }
    }
    
    #endregion
}