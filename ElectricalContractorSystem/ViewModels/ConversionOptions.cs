namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// Options for converting an estimate to a job
    /// </summary>
    public class ConversionOptions
    {
        public string JobNumber { get; set; }
        public bool IncludeAllStages { get; set; }
        public bool IncludeMaterialCosts { get; set; }
        public bool IncludeRoomSpecifications { get; set; }
        public bool IncludePermitItems { get; set; }
        public string Notes { get; set; }
    }
}
