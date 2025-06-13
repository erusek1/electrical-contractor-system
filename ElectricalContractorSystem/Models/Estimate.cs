using System;
using System.Collections.Generic;
using System.Linq;

namespace ElectricalContractorSystem.Models
{
    public class Estimate
    {
        public int EstimateId { get; set; }
        public string EstimateNumber { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public string JobName { get; set; }
        
        // Address properties (simplified names to match usage)
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        
        // Keep old properties for backward compatibility
        public string JobAddress { get => Address; set => Address = value; }
        public string JobCity { get => City; set => City = value; }
        public string JobState { get => State; set => State = value; }
        public string JobZip { get => Zip; set => Zip = value; }
        
        public int? SquareFootage { get; set; }
        public int? NumFloors { get; set; }
        public EstimateStatus Status { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime CreatedDate { get => CreateDate; set => CreateDate = value; }
        public DateTime? ExpirationDate { get; set; }
        public string Notes { get; set; }
        public decimal TaxRate { get; set; }
        public decimal MaterialMarkup { get; set; }
        public decimal LaborRate { get; set; }
        public decimal TotalMaterialCost { get; set; }
        public int TotalLaborMinutes { get; set; }
        public decimal TotalPrice { get; set; }
        public int? JobId { get; set; }
        
        // New properties for versioning and conversion
        public int Version { get; set; } = 1;
        public int? ConvertedToJobId { get; set; }
        public DateTime? ConvertedDate { get; set; }
        public string CreatedBy { get; set; }
        
        // Collections
        public List<EstimateRoom> Rooms { get; set; }
        public List<EstimateLineItem> LineItems { get; set; }
        public List<EstimateStageSummary> StageSummaries { get; set; }
        public List<EstimatePermitItem> PermitItems { get; set; }

        // Calculated properties
        public decimal TotalCost => TotalPrice;
        public decimal TotalLaborHours => TotalLaborMinutes / 60m;

        public Estimate()
        {
            Rooms = new List<EstimateRoom>();
            LineItems = new List<EstimateLineItem>();
            StageSummaries = new List<EstimateStageSummary>();
            PermitItems = new List<EstimatePermitItem>();
            Status = EstimateStatus.Draft;
            TaxRate = 0.064m; // Default 6.4%
            MaterialMarkup = 22.00m; // Default 22%
            LaborRate = 75.00m; // Default $75/hour
            CreateDate = DateTime.Now;
            Version = 1;
        }

        public void CalculateTotals()
        {
            TotalMaterialCost = 0;
            TotalLaborMinutes = 0;

            // Calculate from rooms
            foreach (var room in Rooms)
            {
                foreach (var item in room.Items)
                {
                    TotalMaterialCost += item.MaterialCost;
                    TotalLaborMinutes += item.LaborMinutes * item.Quantity;
                }
            }

            // Also calculate from line items if they exist
            foreach (var item in LineItems)
            {
                TotalMaterialCost += item.MaterialCost;
                TotalLaborMinutes += item.LaborMinutes * item.Quantity;
            }

            // Calculate labor cost
            decimal laborHours = TotalLaborMinutes / 60m;
            decimal laborCost = laborHours * LaborRate;

            // Calculate material with markup
            decimal materialWithMarkup = TotalMaterialCost * (1 + MaterialMarkup / 100);

            // Calculate subtotal
            decimal subtotal = materialWithMarkup + laborCost;

            // Calculate total with tax
            TotalPrice = subtotal * (1 + TaxRate);
        }

        public Estimate CreateNewVersion()
        {
            var newEstimate = new Estimate
            {
                EstimateNumber = EstimateNumber,
                CustomerId = CustomerId,
                Customer = Customer,
                JobName = JobName,
                Address = Address,
                City = City,
                State = State,
                Zip = Zip,
                SquareFootage = SquareFootage,
                NumFloors = NumFloors,
                Status = EstimateStatus.Draft,
                Notes = Notes,
                TaxRate = TaxRate,
                MaterialMarkup = MaterialMarkup,
                LaborRate = LaborRate,
                Version = Version + 1,
                CreatedBy = CreatedBy
            };

            // Copy rooms and items
            foreach (var room in Rooms)
            {
                var newRoom = new EstimateRoom
                {
                    RoomName = room.RoomName,
                    RoomOrder = room.RoomOrder,
                    Notes = room.Notes,
                    Items = new List<EstimateLineItem>()
                };

                foreach (var item in room.Items)
                {
                    newRoom.Items.Add(new EstimateLineItem
                    {
                        ItemCode = item.ItemCode,
                        ItemDescription = item.ItemDescription,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        MaterialCost = item.MaterialCost,
                        LaborMinutes = item.LaborMinutes,
                        LineOrder = item.LineOrder,
                        Notes = item.Notes
                    });
                }

                newEstimate.Rooms.Add(newRoom);
            }

            newEstimate.CalculateTotals();
            return newEstimate;
        }
    }

    public enum EstimateStatus
    {
        Draft,
        Sent,
        Approved,
        Rejected,
        Expired,
        Converted
    }
}
