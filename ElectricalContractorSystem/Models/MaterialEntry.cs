using System;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents a material entry (materials purchased for a job stage)
    /// </summary>
    public class MaterialEntry
    {
        /// <summary>
        /// Database ID of the material entry
        /// </summary>
        public int EntryId { get; set; }

        /// <summary>
        /// Associated job ID
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// Associated job stage ID
        /// </summary>
        public int StageId { get; set; }

        /// <summary>
        /// Associated vendor ID
        /// </summary>
        public int VendorId { get; set; }

        /// <summary>
        /// Date of purchase
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Cost of materials allocated to this job stage
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Invoice number from vendor
        /// </summary>
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Total invoice amount (may be split across multiple job stages)
        /// </summary>
        public decimal? InvoiceTotal { get; set; }

        /// <summary>
        /// Additional notes and description of materials
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to Job
        /// </summary>
        public Job Job { get; set; }

        /// <summary>
        /// Navigation property to JobStage
        /// </summary>
        public JobStage Stage { get; set; }

        /// <summary>
        /// Navigation property to Vendor
        /// </summary>
        public Vendor Vendor { get; set; }

        /// <summary>
        /// Indicates whether this entry represents a full invoice or just a portion
        /// </summary>
        public bool IsPartialInvoice
        {
            get
            {
                return InvoiceTotal.HasValue && InvoiceTotal.Value > Cost;
            }
        }

        /// <summary>
        /// Calculates the percentage of the invoice allocated to this job stage
        /// </summary>
        public decimal? InvoicePercentage
        {
            get
            {
                if (InvoiceTotal.HasValue && InvoiceTotal.Value > 0)
                {
                    return (Cost / InvoiceTotal.Value) * 100;
                }
                return null;
            }
        }

        /// <summary>
        /// Returns a formatted description of the material entry (date, vendor, amount)
        /// </summary>
        public string FormattedDescription
        {
            get
            {
                string vendorName = Vendor?.Name ?? "Unknown Vendor";
                return $"{Date:MM/dd/yyyy} - {vendorName} - ${Cost:F2}";
            }
        }
    }
}
