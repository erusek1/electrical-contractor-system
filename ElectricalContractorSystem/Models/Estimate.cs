using System;
using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    public class Estimate
    {
        public int EstimateId { get; set; }
        public string EstimateNumber { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public string JobName { get; set; }
        public string JobAddress { get; set; }
        public string JobCity { get; set; }
        public string JobState { get; set; }
        public string JobZip { get; set; }
        public int? SquareFootage { get; set; }
        public int? NumFloors { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string Notes { get; set; }
        public decimal TaxRate { get; set; }
        public decimal MaterialMarkup { get; set; }
        public decimal LaborRate { get; set; }
        public decimal TotalMaterialCost { get; set; }
        public int TotalLaborMinutes { get; set; }
        public decimal TotalPrice { get; set; }
        public int? JobId { get; set; }
        public List<EstimateRoom> Rooms { get; set; }

        public Estimate()
        {
            Rooms = new List<EstimateRoom>();
            Status = "Draft";
            TaxRate = 0.064m; // Default 6.4%
            MaterialMarkup = 22.00m; // Default 22%
            LaborRate = 75.00m; // Default $75/hour
            CreatedDate = DateTime.Now;
        }

        public void CalculateTotals()
        {
            TotalMaterialCost = 0;
            TotalLaborMinutes = 0;

            foreach (var room in Rooms)
            {
                foreach (var item in room.Items)
                {
                    TotalMaterialCost += item.MaterialCost;
                    TotalLaborMinutes += item.LaborMinutes * item.Quantity;
                }
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
    }
}
