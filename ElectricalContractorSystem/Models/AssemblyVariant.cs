namespace ElectricalContractorSystem.Models
{
    public class AssemblyVariant
    {
        public int VariantId { get; set; }
        
        public int ParentAssemblyId { get; set; }
        
        public int VariantAssemblyId { get; set; }
        
        public int SortOrder { get; set; } = 0;
        
        // Navigation properties
        public virtual AssemblyTemplate ParentAssembly { get; set; }
        public virtual AssemblyTemplate VariantAssembly { get; set; }
    }
}
