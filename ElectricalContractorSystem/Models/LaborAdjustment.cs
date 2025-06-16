using System;

namespace ElectricalContractorSystem.Models
{
    public class LaborAdjustment
    {
        public int AdjustmentId { get; set; }
        
        public int? EstimateId { get; set; }
        
        public int? JobId { get; set; }
        
        public int? AssemblyId { get; set; }
        
        public AdjustmentType AdjustmentType { get; set; }
        
        public int? PresetId { get; set; }
        
        public decimal RoughMultiplier { get; set; } = 1.00m;
        public decimal FinishMultiplier { get; set; } = 1.00m;
        public decimal ServiceMultiplier { get; set; } = 1.00m;
        public decimal ExtraMultiplier { get; set; } = 1.00m;
        
        public string ReasonCode { get; set; }
        
        public string Notes { get; set; }
        
        public string CreatedBy { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Estimate Estimate { get; set; }
        public virtual Job Job { get; set; }
        public virtual DifficultyPreset Preset { get; set; }
        
        // Calculated properties
        public bool HasAdjustment => 
            RoughMultiplier != 1.00m || 
            FinishMultiplier != 1.00m || 
            ServiceMultiplier != 1.00m || 
            ExtraMultiplier != 1.00m;
        
        public string DisplayText
        {
            get
            {
                if (Preset != null)
                    return $"{Preset.Name} - {ReasonCode ?? Notes}";
                    
                return $"Custom Adjustment - {ReasonCode ?? Notes}";
            }
        }
        
        // Methods
        public void ApplyFromPreset(DifficultyPreset preset)
        {
            if (preset != null)
            {
                PresetId = preset.PresetId;
                RoughMultiplier = preset.RoughMultiplier;
                FinishMultiplier = preset.FinishMultiplier;
                ServiceMultiplier = preset.ServiceMultiplier;
                ExtraMultiplier = preset.ExtraMultiplier;
            }
        }
        
        public int AdjustMinutes(int baseMinutes, LaborStage stage)
        {
            decimal multiplier;
            switch (stage)
            {
                case LaborStage.Rough:
                    multiplier = RoughMultiplier;
                    break;
                case LaborStage.Finish:
                    multiplier = FinishMultiplier;
                    break;
                case LaborStage.Service:
                    multiplier = ServiceMultiplier;
                    break;
                case LaborStage.Extra:
                    multiplier = ExtraMultiplier;
                    break;
                default:
                    multiplier = 1.00m;
                    break;
            }
            
            return (int)Math.Round(baseMinutes * multiplier);
        }
    }
    
    public enum AdjustmentType
    {
        PRESET,
        ACCESS,
        CUSTOM,
        SEASON,
        LEARN,
        EQUIP
    }
    
    public enum LaborStage
    {
        Rough,
        Finish,
        Service,
        Extra
    }
}
