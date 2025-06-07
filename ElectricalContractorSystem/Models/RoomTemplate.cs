using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class RoomTemplate
    {
        public int TemplateId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TemplateName { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RoomType { get; set; }
        
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<RoomTemplateItem> TemplateItems { get; set; }
        
        public RoomTemplate()
        {
            TemplateItems = new HashSet<RoomTemplateItem>();
            CreatedDate = DateTime.Now;
        }
        
        // Create an estimate room from this template
        public EstimateRoom CreateEstimateRoom()
        {
            var room = new EstimateRoom
            {
                RoomName = this.RoomType,
                Notes = $"Created from template: {this.TemplateName}"
            };
            
            int lineOrder = 0;
            foreach (var templateItem in this.TemplateItems)
            {
                if (!templateItem.IsOptional || templateItem.IsOptional)
                {
                    var lineItem = new EstimateLineItem
                    {
                        ItemId = templateItem.ItemId,
                        ItemCode = templateItem.PriceListItem?.ItemCode ?? "",
                        Description = templateItem.PriceListItem?.Name ?? "",
                        UnitPrice = templateItem.PriceListItem?.TotalPrice ?? 0,
                        Quantity = templateItem.Quantity,
                        LineOrder = lineOrder++,
                        Notes = templateItem.Notes
                    };
                    
                    room.LineItems.Add(lineItem);
                }
            }
            
            return room;
        }
        
        // Clone template
        public RoomTemplate Clone()
        {
            var clone = new RoomTemplate
            {
                TemplateName = $"{this.TemplateName} (Copy)",
                RoomType = this.RoomType,
                Description = this.Description,
                IsActive = true
            };
            
            foreach (var item in this.TemplateItems)
            {
                clone.TemplateItems.Add(new RoomTemplateItem
                {
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    IsOptional = item.IsOptional,
                    Notes = item.Notes
                });
            }
            
            return clone;
        }
    }
}
