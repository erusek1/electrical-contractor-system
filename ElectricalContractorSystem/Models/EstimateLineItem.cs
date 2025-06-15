namespace ElectricalContractorSystem.Models
{
    public class EstimateLineItem
    {
        public int LineId { get; set; }
        public int RoomId { get; set; }
        public int? ItemId { get; set; }
        public int? AssemblyId { get; set; }  // New field for assembly reference
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        
        // Add EstimateId property for navigation
        public int EstimateId { get; set; }
        
        // Entry mode tracking
        public enum EntryMode
        {
            Assembly,      // Using assembly with auto-calculated labor by stage
            PriceList      // Using price list item with manual labor entry
        }
        
        public EntryMode Mode { get; set; } = EntryMode.PriceList;
        
        // Aliases for compatibility
        public string Description 
        { 
            get => ItemDescription; 
            set => ItemDescription = value; 
        }
        
        public string ItemName
        {
            get => ItemDescription;
            set => ItemDescription = value;
        }
        
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MaterialCost { get; set; }
        
        // Labor tracking - different based on mode
        public int LaborMinutes { get; set; }  // Total for PriceList mode
        
        // Labor by stage for Assembly mode
        public int? RoughLaborMinutes { get; set; }
        public int? FinishLaborMinutes { get; set; }
        public int? ServiceLaborMinutes { get; set; }
        public int? ExtraLaborMinutes { get; set; }
        
        public int LineOrder { get; set; }
        public string Notes { get; set; }
        
        // Calculated properties
        public decimal TotalPrice => Quantity * UnitPrice;
        
        public int TotalLaborMinutes
        {
            get
            {
                if (Mode == EntryMode.Assembly)
                {
                    return (RoughLaborMinutes ?? 0) + 
                           (FinishLaborMinutes ?? 0) + 
                           (ServiceLaborMinutes ?? 0) + 
                           (ExtraLaborMinutes ?? 0);
                }
                return LaborMinutes;
            }
        }
        
        // Factory method to create from PriceListItem
        public static EstimateLineItem CreateFromPriceListItem(PriceListItem item)
        {
            return new EstimateLineItem
            {
                ItemCode = item.ItemCode,
                ItemDescription = item.Name,
                UnitPrice = item.SellPrice,
                MaterialCost = item.BaseCost,
                LaborMinutes = item.LaborMinutes,
                Quantity = 1,
                LineOrder = 0,
                Mode = EntryMode.PriceList
            };
        }
        
        // Factory method to create from AssemblyTemplate
        public static EstimateLineItem CreateFromAssembly(AssemblyTemplate assembly, decimal laborRate)
        {
            // Calculate total price including labor and materials
            var totalPrice = assembly.CalculateTotalCost(laborRate, null, 22.0m);
            
            return new EstimateLineItem
            {
                AssemblyId = assembly.AssemblyId,
                ItemCode = assembly.AssemblyCode,
                ItemDescription = assembly.Name,
                UnitPrice = totalPrice,
                MaterialCost = assembly.TotalMaterialCost,
                RoughLaborMinutes = assembly.RoughMinutes,
                FinishLaborMinutes = assembly.FinishMinutes,
                ServiceLaborMinutes = assembly.ServiceMinutes,
                ExtraLaborMinutes = assembly.ExtraMinutes,
                Quantity = 1,
                LineOrder = 0,
                Mode = EntryMode.Assembly
            };
        }
    }
}
