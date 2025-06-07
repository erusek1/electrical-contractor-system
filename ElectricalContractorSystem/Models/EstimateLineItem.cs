using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricalContractorSystem.Models
{
    public class EstimateLineItem
    {
        public int LineItemId { get; set; }
        
        [Required]
        public int EstimateId { get; set; }
        
        [Required]
        public int RoomId { get; set; }
        
        [Required]
        public int ItemId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;
        
        [Required]
        [StringLength(20)]
        public string ItemCode { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Description { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }
        
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal TotalPrice => Quantity * UnitPrice;
        
        public int LineOrder { get; set; }
        
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual Estimate Estimate { get; set; }
        public virtual EstimateRoom Room { get; set; }
        public virtual PriceListItem PriceListItem { get; set; }
        
        // Create from price list item
        public static EstimateLineItem CreateFromPriceListItem(PriceListItem item, int quantity = 1)
        {
            return new EstimateLineItem
            {
                ItemId = item.ItemId,
                ItemCode = item.ItemCode,
                Description = item.Name,
                UnitPrice = item.TotalPrice,
                Quantity = quantity
            };
        }
    }
}