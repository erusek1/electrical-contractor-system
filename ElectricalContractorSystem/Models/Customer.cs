using System;
using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        
        public string Name { get; set; }
        
        public string Address { get; set; }
        
        public string City { get; set; }
        
        public string State { get; set; }
        
        public string Zip { get; set; }
        
        public string Email { get; set; }
        
        public string Phone { get; set; }
        
        public string Notes { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<Estimate> Estimates { get; set; }
        public virtual ICollection<Job> Jobs { get; set; }
        public virtual ICollection<Property> Properties { get; set; }
        
        public Customer()
        {
            Estimates = new HashSet<Estimate>();
            Jobs = new HashSet<Job>();
            Properties = new HashSet<Property>();
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
        
        // Get count of properties
        public int PropertyCount
        {
            get
            {
                return Properties?.Count ?? 0;
            }
        }
        
        // Get count of all jobs
        public int TotalJobCount
        {
            get
            {
                return Jobs?.Count ?? 0;
            }
        }
        
        // Get count of active jobs
        public int ActiveJobCount
        {
            get
            {
                if (Jobs == null) return 0;
                
                int count = 0;
                foreach (var job in Jobs)
                {
                    if (job.Status != "Complete")
                        count++;
                }
                return count;
            }
        }
    }
}
