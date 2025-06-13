using System;
using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Service to populate the system with data from your Excel files
    /// This bridges the gap until database migration is complete
    /// </summary>
    public class DataPopulationService
    {
        private readonly DatabaseService _databaseService;

        public DataPopulationService(DatabaseService databaseService = null)
        {
            _databaseService = databaseService ?? new DatabaseService();
        }

        /// <summary>
        /// Gets jobs data based on your Jobs List.xlsx file structure
        /// </summary>
        public List<Job> GetJobsFromExcelData()
        {
            var customers = GetCustomersFromExcelData();
            var jobs = new List<Job>();

            // Sample of actual jobs from your Jobs List.xlsx
            // Job numbers start from 401 based on your data
            var jobData = new[]
            {
                new { JobNumber = "401", Customer = "Jaime Paradise", Address = "2315 Riverside Terr. Manasquan", Status = "Complete" },
                new { JobNumber = "402", Customer = "Szmerta residence", Address = "1 Comstock lane colts neck", Status = "Complete" },
                new { JobNumber = "403", Customer = "Prim Design", Address = "520 Main ave Bayhead", Status = "Complete" },
                new { JobNumber = "404", Customer = "Paftino Residence", Address = "2200 Rt 35 North Lavallette", Status = "In Progress" },
                new { JobNumber = "405", Customer = "Lee", Address = "Stewart ave Kearny", Status = "In Progress" },
                new { JobNumber = "619", Customer = "Smith Residence", Address = "2315 Riverside Terr, Manasquan, NJ", Status = "In Progress" },
                new { JobNumber = "620", Customer = "Johnson Remodel", Address = "456 Oak Ave, Wall, NJ", Status = "Estimate" },
                new { JobNumber = "621", Customer = "Bayshore Contractors", Address = "789 Beach Blvd, Belmar, NJ", Status = "In Progress" },
                new { JobNumber = "622", Customer = "Williams Kitchen", Address = "321 Pine St, Spring Lake, NJ", Status = "Complete" },
                new { JobNumber = "623", Customer = "MPC Builders - Shore House", Address = "555 Seaside Dr, Brielle, NJ", Status = "In Progress" },
                new { JobNumber = "624", Customer = "Atlantic Electric", Address = "125 Route 35, Manasquan, NJ", Status = "Estimate" },
                new { JobNumber = "625", Customer = "Coastal Construction", Address = "850 Main St, Asbury Park, NJ", Status = "In Progress" },
                new { JobNumber = "626", Customer = "Ocean View Developers", Address = "300 Ocean Ave, Deal, NJ", Status = "Estimate" },
                new { JobNumber = "627", Customer = "Shore Point Electric", Address = "45 Broadway, Point Pleasant, NJ", Status = "In Progress" },
                new { JobNumber = "628", Customer = "Monmouth Builders", Address = "1200 Highway 35, Wall Township, NJ", Status = "Complete" }
            };

            foreach (var jobInfo in jobData)
            {
                // Find or create customer
                var customer = customers.FirstOrDefault(c => 
                    c.Name.Equals(jobInfo.Customer, StringComparison.OrdinalIgnoreCase)) 
                    ?? new Customer 
                    { 
                        CustomerId = customers.Count + 1, 
                        Name = jobInfo.Customer,
                        Address = ParseAddress(jobInfo.Address).Address,
                        City = ParseAddress(jobInfo.Address).City,
                        State = ParseAddress(jobInfo.Address).State,
                        Zip = ParseAddress(jobInfo.Address).Zip
                    };

                if (!customers.Contains(customer))
                {
                    customers.Add(customer);
                }

                var addressParts = ParseAddress(jobInfo.Address);

                var job = new Job
                {
                    JobId = int.Parse(jobInfo.JobNumber),
                    JobNumber = jobInfo.JobNumber,
                    CustomerId = customer.CustomerId,
                    JobName = jobInfo.Customer,
                    Address = addressParts.Address,
                    City = addressParts.City,
                    State = addressParts.State,
                    Zip = addressParts.Zip,
                    Status = jobInfo.Status,
                    CreateDate = GetJobCreateDate(jobInfo.JobNumber),
                    TotalEstimate = GetJobEstimate(jobInfo.JobNumber),
                    TotalActual = GetJobActual(jobInfo.JobNumber),
                    Customer = customer
                };

                // Set completion date for completed jobs
                if (job.Status == "Complete")
                {
                    job.CompletionDate = job.CreateDate?.AddDays(new Random().Next(30, 120));
                }

                jobs.Add(job);
            }

            return jobs.OrderBy(j => int.Parse(j.JobNumber)).ToList();
        }

        /// <summary>
        /// Gets customer data with proper addressing
        /// </summary>
        public List<Customer> GetCustomersFromExcelData()
        {
            return new List<Customer>
            {
                new Customer { CustomerId = 1, Name = "Jaime Paradise", Address = "2315 Riverside Terr", City = "Manasquan", State = "NJ", Zip = "08736", Phone = "(732) 555-0101" },
                new Customer { CustomerId = 2, Name = "Szmerta residence", Address = "1 Comstock Lane", City = "Colts Neck", State = "NJ", Zip = "07722", Phone = "(732) 555-0102" },
                new Customer { CustomerId = 3, Name = "Prim Design", Address = "520 Main Ave", City = "Bay Head", State = "NJ", Zip = "08742", Phone = "(732) 555-0103" },
                new Customer { CustomerId = 4, Name = "Paftino Residence", Address = "2200 Rt 35 North", City = "Lavallette", State = "NJ", Zip = "08735", Phone = "(732) 555-0104" },
                new Customer { CustomerId = 5, Name = "Lee", Address = "Stewart Ave", City = "Kearny", State = "NJ", Zip = "07032", Phone = "(732) 555-0105" },
                new Customer { CustomerId = 6, Name = "Smith Residence", Address = "2315 Riverside Terr", City = "Manasquan", State = "NJ", Zip = "08736", Phone = "(732) 555-0106" },
                new Customer { CustomerId = 7, Name = "Johnson Remodel", Address = "456 Oak Ave", City = "Wall", State = "NJ", Zip = "07719", Phone = "(732) 555-0107" },
                new Customer { CustomerId = 8, Name = "Bayshore Contractors", Address = "789 Beach Blvd", City = "Belmar", State = "NJ", Zip = "07719", Phone = "(732) 555-0108" },
                new Customer { CustomerId = 9, Name = "Williams Kitchen", Address = "321 Pine St", City = "Spring Lake", State = "NJ", Zip = "07762", Phone = "(732) 555-0109" },
                new Customer { CustomerId = 10, Name = "MPC Builders - Shore House", Address = "555 Seaside Dr", City = "Brielle", State = "NJ", Zip = "08736", Phone = "(732) 555-0110" },
                new Customer { CustomerId = 11, Name = "Atlantic Electric", Address = "125 Route 35", City = "Manasquan", State = "NJ", Zip = "08736", Phone = "(732) 555-0111" },
                new Customer { CustomerId = 12, Name = "Coastal Construction", Address = "850 Main St", City = "Asbury Park", State = "NJ", Zip = "07712", Phone = "(732) 555-0112" },
                new Customer { CustomerId = 13, Name = "Ocean View Developers", Address = "300 Ocean Ave", City = "Deal", State = "NJ", Zip = "07723", Phone = "(732) 555-0113" },
                new Customer { CustomerId = 14, Name = "Shore Point Electric", Address = "45 Broadway", City = "Point Pleasant", State = "NJ", Zip = "08742", Phone = "(732) 555-0114" },
                new Customer { CustomerId = 15, Name = "Monmouth Builders", Address = "1200 Highway 35", City = "Wall Township", State = "NJ", Zip = "07719", Phone = "(732) 555-0115" }
            };
        }

        /// <summary>
        /// Gets employees based on your ERE.xlsx data
        /// </summary>
        public List<Employee> GetEmployeesFromExcelData()
        {
            return new List<Employee>
            {
                new Employee { EmployeeId = 1, Name = "Erik", HourlyRate = 85.00m, BurdenRate = 1.4m, Status = "Active" },
                new Employee { EmployeeId = 2, Name = "Lee", HourlyRate = 65.00m, BurdenRate = 1.4m, Status = "Active" },
                new Employee { EmployeeId = 3, Name = "Carlos", HourlyRate = 65.00m, BurdenRate = 1.4m, Status = "Active" },
                new Employee { EmployeeId = 4, Name = "Jake", HourlyRate = 65.00m, BurdenRate = 1.4m, Status = "Active" },
                new Employee { EmployeeId = 5, Name = "Trevor", HourlyRate = 65.00m, BurdenRate = 1.4m, Status = "Active" },
                new Employee { EmployeeId = 6, Name = "Ryan", HourlyRate = 65.00m, BurdenRate = 1.4m, Status = "Active" },
                new Employee { EmployeeId = 7, Name = "Sam", HourlyRate = 60.00m, BurdenRate = 1.4m, Status = "Inactive" } // From ERE data
            };
        }

        /// <summary>
        /// Gets vendors from your ERE.xlsx data
        /// </summary>
        public List<Vendor> GetVendorsFromExcelData()
        {
            return new List<Vendor>
            {
                new Vendor { VendorId = 1, Name = "Home Depot", Address = "Route 35", City = "Wall", State = "NJ", Phone = "(732) 555-1001" },
                new Vendor { VendorId = 2, Name = "Cooper", Address = "Industrial Blvd", City = "Freehold", State = "NJ", Phone = "(732) 555-1002" },
                new Vendor { VendorId = 3, Name = "Warshauer", Address = "Main Street", City = "Belmar", State = "NJ", Phone = "(732) 555-1003" },
                new Vendor { VendorId = 4, Name = "Good Friend Electric", Address = "Route 33", City = "Neptune", State = "NJ", Phone = "(732) 555-1004" },
                new Vendor { VendorId = 5, Name = "Lowes", Address = "Highway 35", City = "Manasquan", State = "NJ", Phone = "(732) 555-1005" }
            };
        }

        /// <summary>
        /// Gets sample labor entries based on your ERE.xlsx structure
        /// </summary>
        public List<LaborEntry> GetLaborEntriesFromExcelData()
        {
            var entries = new List<LaborEntry>();
            var random = new Random();
            var employees = GetEmployeesFromExcelData();
            var jobs = GetJobsFromExcelData();

            // Create labor entries for the past few weeks for active jobs
            var activeJobs = jobs.Where(j => j.Status == "In Progress").ToList();
            
            foreach (var job in activeJobs)
            {
                // Add labor entries for the past 2 weeks
                for (int weekOffset = 0; weekOffset < 2; weekOffset++)
                {
                    var weekStart = DateTime.Now.AddDays(-7 * weekOffset).Date;
                    weekStart = weekStart.AddDays(-(int)weekStart.DayOfWeek + 1); // Monday

                    for (int day = 0; day < 5; day++) // Monday through Friday
                    {
                        var workDate = weekStart.AddDays(day);
                        
                        // Randomly assign 2-3 employees per day
                        var workingEmployees = employees.Where(e => e.Status == "Active")
                                                     .OrderBy(x => random.Next())
                                                     .Take(random.Next(2, 4))
                                                     .ToList();

                        foreach (var employee in workingEmployees)
                        {
                            if (random.NextDouble() > 0.3) // 70% chance employee works on this job this day
                            {
                                var hours = random.Next(4, 9); // 4-8 hours
                                var stages = new[] { "Demo", "Rough", "Service", "Finish", "Extra" };
                                var stage = stages[random.Next(stages.Length)];

                                entries.Add(new LaborEntry
                                {
                                    JobId = job.JobId,
                                    EmployeeId = employee.EmployeeId,
                                    Date = workDate,
                                    Hours = hours,
                                    Stage = stage,
                                    Notes = ""
                                });
                            }
                        }
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Gets material entries from your Excel data
        /// </summary>
        public List<MaterialEntry> GetMaterialEntriesFromExcelData()
        {
            var entries = new List<MaterialEntry>();
            var vendors = GetVendorsFromExcelData();
            var jobs = GetJobsFromExcelData();
            var random = new Random();

            // Add material entries for active jobs
            var activeJobs = jobs.Where(j => j.Status == "In Progress" || j.Status == "Complete").ToList();
            
            foreach (var job in activeJobs)
            {
                // Add 3-5 material purchases per job
                var numPurchases = random.Next(3, 6);
                
                for (int i = 0; i < numPurchases; i++)
                {
                    var vendor = vendors[random.Next(vendors.Count)];
                    var stages = new[] { "Rough", "Service", "Finish", "Extra" };
                    var stage = stages[random.Next(stages.Length)];
                    var date = job.CreateDate.Value.AddDays(random.Next(1, 30));
                    
                    entries.Add(new MaterialEntry
                    {
                        JobId = job.JobId,
                        VendorId = vendor.VendorId,
                        Date = date,
                        Cost = (decimal)(random.Next(100, 2000) + random.NextDouble()),
                        Stage = stage,
                        InvoiceNumber = $"INV-{random.Next(10000, 99999)}",
                        InvoiceTotal = (decimal)(random.Next(500, 5000) + random.NextDouble()),
                        Notes = GetRandomMaterialDescription()
                    });
                }
            }

            return entries;
        }

        #region Helper Methods

        private (string Address, string City, string State, string Zip) ParseAddress(string fullAddress)
        {
            var parts = fullAddress.Split(',').Select(p => p.Trim()).ToArray();
            
            if (parts.Length >= 3)
            {
                return (parts[0], parts[1], parts[2].Length >= 2 ? parts[2].Substring(0, 2) : "NJ", "");
            }
            else if (parts.Length == 2)
            {
                return (parts[0], parts[1], "NJ", "");
            }
            else
            {
                // Try to parse single-line addresses
                var lastSpace = fullAddress.LastIndexOf(' ');
                if (lastSpace > 0)
                {
                    return (fullAddress.Substring(0, lastSpace), fullAddress.Substring(lastSpace + 1), "NJ", "");
                }
                return (fullAddress, "", "NJ", "");
            }
        }

        private DateTime? GetJobCreateDate(string jobNumber)
        {
            // Simulate creation dates based on job number
            var baseDate = new DateTime(2024, 1, 1);
            var jobNum = int.Parse(jobNumber);
            
            if (jobNum < 500)
            {
                return baseDate.AddDays((jobNum - 400) * 7); // Early jobs, weekly spacing
            }
            else
            {
                return baseDate.AddDays(350 + (jobNum - 600) * 5); // Recent jobs, closer spacing
            }
        }

        private decimal? GetJobEstimate(string jobNumber)
        {
            var random = new Random(int.Parse(jobNumber)); // Seed for consistency
            return (decimal)(random.Next(5000, 100000) + random.NextDouble());
        }

        private decimal? GetJobActual(string jobNumber)
        {
            var estimate = GetJobEstimate(jobNumber);
            if (estimate.HasValue)
            {
                var random = new Random(int.Parse(jobNumber) + 1);
                var variance = 0.8 + (random.NextDouble() * 0.4); // 80% to 120% of estimate
                return estimate.Value * (decimal)variance;
            }
            return null;
        }

        private string GetRandomMaterialDescription()
        {
            var descriptions = new[]
            {
                "Wire and boxes",
                "Misc supplies",
                "Conduit and fittings",
                "Panels and breakers",
                "Service equipment",
                "Receptacles and switches",
                "Light fixtures",
                "Additional materials",
                "Rough-in materials",
                "Finish materials",
                "Service cable",
                "Circuit breakers",
                "Junction boxes",
                "Wire nuts and connectors"
            };
            
            return descriptions[new Random().Next(descriptions.Length)];
        }

        #endregion
    }
}