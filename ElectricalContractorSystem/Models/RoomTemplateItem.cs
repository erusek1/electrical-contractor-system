using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class RoomTemplateItem
    {
        public int TemplateItemId { get; set; }
        
        [Required]
        public int TemplateId { get; set; }
        
        [Required]
        public int ItemId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;
        
        public bool IsOptional { get; set; } = false;
        
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual RoomTemplate RoomTemplate { get; set; }
        public virtual PriceListItem PriceListItem { get; set; }
        
        // Helper properties
        public decimal TotalPrice => Quantity * (PriceListItem?.TotalPrice ?? 0);
        
        public string DisplayName => PriceListItem != null 
            ? $"{PriceListItem.ItemCode} - {PriceListItem.Name}" 
            : "Unknown Item";
    }
}
