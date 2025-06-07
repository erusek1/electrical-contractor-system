using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(255)]
        public string Address { get; set; }
        
        [StringLength(50)]
        public string City { get; set; }
        
        [StringLength(2)]
        public string State { get; set; }
        
        [StringLength(10)]
        public string Zip { get; set; }
        
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; }
        
        public string Notes { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<Estimate> Estimates { get; set; }
        
        public Customer()
        {
            Estimates = new HashSet<Estimate>();
            CreatedDate = DateTime.Now;
        }
        
        // Full address helper
        public string FullAddress
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Address)) parts.Add(Address);
                if (!string.IsNullOrWhiteSpace(City)) parts.Add(City);
                if (!string.IsNullOrWhiteSpace(State)) parts.Add(State);
                if (!string.IsNullOrWhiteSpace(Zip)) parts.Add(Zip);
                return string.Join(", ", parts);
            }
        }
    }
}