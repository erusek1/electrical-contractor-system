using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ElectricalContractorSystem.Models
{
    public class Estimate
    {
        public int EstimateId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string EstimateNumber { get; set; }
        
        public int Version { get; set; } = 1;
        
        [Required]
        public int CustomerId { get; set; }
        
        [Required]
        [StringLength(255)]
        public string JobName { get; set; }
        
        [StringLength(255)]
        public string Address { get; set; }
        
        [StringLength(50)]
        public string City { get; set; }
        
        [StringLength(2)]
        public string State { get; set; }
        
        [StringLength(10)]
        public string Zip { get; set; }
        
        public int? SquareFootage { get; set; }
        public int? NumFloors { get; set; }
        
        public EstimateStatus Status { get; set; } = EstimateStatus.Draft;
        
        public decimal LaborRate { get; set; } = 85.00m;
        public decimal MaterialMarkup { get; set; } = 22.00m;
        
        public decimal? TotalLaborHours { get; set; }
        public decimal? TotalMaterialCost { get; set; }
        public decimal? TotalCost { get; set; }
        
        public string Notes { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime? SentDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        [StringLength(50)]
        public string CreatedBy { get; set; }
        
        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual ICollection<EstimateRoom> Rooms { get; set; }
        public virtual ICollection<EstimateLineItem> LineItems { get; set; }
        public virtual ICollection<EstimateStageSummary> StageSummaries { get; set; }
        public virtual ICollection<EstimatePermitItem> PermitItems { get; set; }
        
        public Estimate()
        {
            Rooms = new HashSet<EstimateRoom>();
            LineItems = new HashSet<EstimateLineItem>();
            StageSummaries = new HashSet<EstimateStageSummary>();
            PermitItems = new HashSet<EstimatePermitItem>();
        }
        
        // Calculate totals
        public void CalculateTotals()
        {
            // Calculate total labor hours by stage
            var laborByStage = new Dictionary<string, decimal>();
            
            foreach (var item in LineItems)
            {
                var priceListItem = item.PriceListItem;
                if (priceListItem != null)
                {
                    foreach (var labor in priceListItem.LaborMinutes)
                    {
                        if (!laborByStage.ContainsKey(labor.Stage))
                            laborByStage[labor.Stage] = 0;
                        
                        laborByStage[labor.Stage] += (item.Quantity * labor.Minutes) / 60m;
                    }
                }
            }
            
            TotalLaborHours = laborByStage.Values.Sum();
            
            // Calculate material cost
            TotalMaterialCost = LineItems.Sum(li => li.TotalPrice);
            
            // Calculate total cost
            var laborCost = TotalLaborHours.GetValueOrDefault() * LaborRate;
            var materialWithMarkup = TotalMaterialCost.GetValueOrDefault() * (1 + MaterialMarkup / 100);
            TotalCost = laborCost + materialWithMarkup;
            
            // Update stage summaries
            foreach (var stage in laborByStage)
            {
                var summary = StageSummaries.FirstOrDefault(s => s.Stage == stage.Key);
                if (summary == null)
                {
                    summary = new EstimateStageSummary
                    {
                        EstimateId = EstimateId,
                        Stage = stage.Key
                    };
                    StageSummaries.Add(summary);
                }
                summary.EstimatedHours = stage.Value;
            }
        }
        
        // Clone estimate for new version
        public Estimate CreateNewVersion()
        {
            var newEstimate = new Estimate
            {
                EstimateNumber = this.EstimateNumber,
                Version = this.Version + 1,
                CustomerId = this.CustomerId,
                JobName = this.JobName,
                Address = this.Address,
                City = this.City,
                State = this.State,
                Zip = this.Zip,
                SquareFootage = this.SquareFootage,
                NumFloors = this.NumFloors,
                Status = EstimateStatus.Draft,
                LaborRate = this.LaborRate,
                MaterialMarkup = this.MaterialMarkup,
                Notes = this.Notes,
                CreatedDate = DateTime.Now,
                CreatedBy = this.CreatedBy
            };
            
            // Clone rooms and items
            foreach (var room in this.Rooms.OrderBy(r => r.RoomOrder))
            {
                var newRoom = new EstimateRoom
                {
                    RoomName = room.RoomName,
                    RoomOrder = room.RoomOrder,
                    Notes = room.Notes
                };
                
                foreach (var item in room.LineItems.OrderBy(i => i.LineOrder))
                {
                    newRoom.LineItems.Add(new EstimateLineItem
                    {
                        ItemId = item.ItemId,
                        Quantity = item.Quantity,
                        ItemCode = item.ItemCode,
                        Description = item.Description,
                        UnitPrice = item.UnitPrice,
                        LineOrder = item.LineOrder,
                        Notes = item.Notes
                    });
                }
                
                newEstimate.Rooms.Add(newRoom);
            }
            
            return newEstimate;
        }
    }
    
    public enum EstimateStatus
    {
        Draft,
        Sent,
        Approved,
        Rejected,
        Expired
    }
}