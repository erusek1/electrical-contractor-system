using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ElectricalContractorSystem.Models
{
    public class AssemblyTemplate
    {
        public int AssemblyId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string AssemblyCode { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; }
        
        // Labor minutes by stage
        public int RoughMinutes { get; set; } = 0;
        public int FinishMinutes { get; set; } = 0;
        public int ServiceMinutes { get; set; } = 0;
        public int ExtraMinutes { get; set; } = 0;
        
        public bool IsDefault { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        
        // Navigation properties
        public virtual ICollection<AssemblyComponent> Components { get; set; } = new List<AssemblyComponent>();
        public virtual ICollection<AssemblyVariant> Variants { get; set; } = new List<AssemblyVariant>();
        public virtual ICollection<AssemblyVariant> ParentVariants { get; set; } = new List<AssemblyVariant>();
        
        // Calculated properties
        public int TotalLaborMinutes => RoughMinutes + FinishMinutes + ServiceMinutes + ExtraMinutes;
        
        public decimal TotalLaborHours => TotalLaborMinutes / 60.0m;
        
        public decimal TotalMaterialCost
        {
            get
            {
                return Components?.Sum(c => c.Quantity * (c.Material?.PriceWithTax ?? 0)) ?? 0;
            }
        }
        
        // Methods
        public decimal CalculateLaborCost(decimal hourlyRate, ServiceType serviceType = null)
        {
            var rate = hourlyRate;
            if (serviceType != null)
            {
                rate = hourlyRate * serviceType.LaborMultiplier;
            }
            return TotalLaborHours * rate;
        }
        
        public decimal CalculateTotalCost(decimal hourlyRate, ServiceType serviceType = null, decimal materialMarkup = 22.0m)
        {
            var laborCost = CalculateLaborCost(hourlyRate, serviceType);
            var materialCost = TotalMaterialCost * (1 + materialMarkup / 100);
            return laborCost + materialCost;
        }
        
        public AssemblyTemplate CreateVariant(string newName, string modifiedBy)
        {
            var variant = new AssemblyTemplate
            {
                AssemblyCode = this.AssemblyCode, // Keep same code
                Name = newName,
                Description = this.Description,
                Category = this.Category,
                RoughMinutes = this.RoughMinutes,
                FinishMinutes = this.FinishMinutes,
                ServiceMinutes = this.ServiceMinutes,
                ExtraMinutes = this.ExtraMinutes,
                IsDefault = false, // Variants are not default
                IsActive = true,
                CreatedBy = modifiedBy,
                CreatedDate = DateTime.Now
            };
            
            // Copy components
            foreach (var component in this.Components)
            {
                variant.Components.Add(new AssemblyComponent
                {
                    MaterialId = component.MaterialId,
                    Quantity = component.Quantity,
                    IsOptional = component.IsOptional,
                    Notes = component.Notes
                });
            }
            
            return variant;
        }
        
        public void ApplyLaborAdjustment(DifficultyPreset preset)
        {
            RoughMinutes = (int)(RoughMinutes * preset.RoughMultiplier);
            FinishMinutes = (int)(FinishMinutes * preset.FinishMultiplier);
            ServiceMinutes = (int)(ServiceMinutes * preset.ServiceMultiplier);
            ExtraMinutes = (int)(ExtraMinutes * preset.ExtraMultiplier);
        }
    }
}
