using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem
{
    /// <summary>
    /// DATABASE VERIFICATION PROGRAM
    /// This program tests the DatabaseService to verify that customer data loads properly
    /// Run this to diagnose any database connectivity issues
    /// </summary>
    public class DatabaseVerificationProgram
    {
        private readonly DatabaseService _databaseService;

        public DatabaseVerificationProgram()
        {
            _databaseService = new DatabaseService();
        }

        /// <summary>
        /// Main verification method - run this to test database connectivity
        /// </summary>
        public void RunVerification()
        {
            Console.WriteLine("=== DATABASE VERIFICATION PROGRAM ===");
            Console.WriteLine("Testing database connectivity and data loading...\n");

            // Test 1: Connection Test
            Console.WriteLine("Test 1: Database Connection");
            bool canConnect = _databaseService.TestConnection();
            Console.WriteLine($"Connection Status: {(canConnect ? "SUCCESS" : "FAILED")}");
            
            if (!canConnect)
            {
                Console.WriteLine("ERROR: Cannot connect to database!");
                Console.WriteLine("Check your connection string in App.config");
                return;
            }

            // Test 2: Database Info
            Console.WriteLine("\nTest 2: Database Information");
            string dbInfo = _databaseService.GetDatabaseInfo();
            Console.WriteLine(dbInfo);

            // Test 3: Customer Data Loading
            Console.WriteLine("\nTest 3: Customer Data Loading");
            TestCustomerLoading();

            // Test 4: Employee Data Loading
            Console.WriteLine("\nTest 4: Employee Data Loading");
            TestEmployeeLoading();

            // Test 5: Vendor Data Loading
            Console.WriteLine("\nTest 5: Vendor Data Loading");
            TestVendorLoading();

            // Test 6: Job Data Loading
            Console.WriteLine("\nTest 6: Job Data Loading");
            TestJobLoading();

            Console.WriteLine("\n=== VERIFICATION COMPLETE ===");
            Console.WriteLine("Check output above for any errors.");
        }

        private void TestCustomerLoading()
        {
            try
            {
                var customers = _databaseService.GetAllCustomers();
                Console.WriteLine($"Loaded {customers.Count} customers");
                
                if (customers.Count > 0)
                {
                    Console.WriteLine("Sample customers:");
                    foreach (var customer in customers.Take(3))
                    {
                        Console.WriteLine($"  - {customer.Name} (ID: {customer.CustomerId})");
                    }
                }
                else
                {
                    Console.WriteLine("WARNING: No customers found in database");
                    Console.WriteLine("This could mean:");
                    Console.WriteLine("  1. Database is empty (needs sample data)");
                    Console.WriteLine("  2. Table doesn't exist (run database schema)");
                    Console.WriteLine("  3. Connection issue (check connection string)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading customers: {ex.Message}");
            }
        }

        private void TestEmployeeLoading()
        {
            try
            {
                var employees = _databaseService.GetAllEmployees();
                Console.WriteLine($"Loaded {employees.Count} employees");
                
                if (employees.Count > 0)
                {
                    Console.WriteLine("Sample employees:");
                    foreach (var employee in employees.Take(3))
                    {
                        Console.WriteLine($"  - {employee.Name} (${employee.HourlyRate}/hr)");
                    }
                }
                else
                {
                    Console.WriteLine("INFO: No employees found - this is expected if database is empty");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading employees: {ex.Message}");
            }
        }

        private void TestVendorLoading()
        {
            try
            {
                var vendors = _databaseService.GetAllVendors();
                Console.WriteLine($"Loaded {vendors.Count} vendors");
                
                if (vendors.Count > 0)
                {
                    Console.WriteLine("Sample vendors:");
                    foreach (var vendor in vendors.Take(3))
                    {
                        Console.WriteLine($"  - {vendor.Name}");
                    }
                }
                else
                {
                    Console.WriteLine("INFO: No vendors found - this is expected if database is empty");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading vendors: {ex.Message}");
            }
        }

        private void TestJobLoading()
        {
            try
            {
                var jobs = _databaseService.GetAllJobs();
                Console.WriteLine($"Loaded {jobs.Count} jobs");
                
                if (jobs.Count > 0)
                {
                    Console.WriteLine("Sample jobs:");
                    foreach (var job in jobs.Take(3))
                    {
                        Console.WriteLine($"  - Job #{job.JobNumber}: {job.JobName}");
                    }
                }
                else
                {
                    Console.WriteLine("INFO: No jobs found - this is expected if database is empty");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading jobs: {ex.Message}");
            }
        }

        /// <summary>
        /// Add sample data to test the system
        /// </summary>
        public void AddSampleData()
        {
            Console.WriteLine("Adding sample data to database...");

            try
            {
                // Add sample customers
                var sampleCustomers = new List<Customer>
                {
                    new Customer
                    {
                        Name = "Smith Residence",
                        Address = "123 Main St",
                        City = "Manasquan",
                        State = "NJ",
                        Zip = "08736",
                        Phone = "(732) 555-0123",
                        Email = "john@smithfamily.com"
                    },
                    new Customer
                    {
                        Name = "Johnson Remodel",
                        Address = "456 Oak Ave",
                        City = "Spring Lake",
                        State = "NJ", 
                        Zip = "07762",
                        Phone = "(732) 555-0456"
                    },
                    new Customer
                    {
                        Name = "Bayshore Contractors",
                        Address = "789 Beach Blvd",
                        City = "Bay Head",
                        State = "NJ",
                        Zip = "08742",
                        Phone = "(732) 555-0789",
                        Email = "info@bayshorecontractors.com"
                    }
                };

                foreach (var customer in sampleCustomers)
                {
                    int customerId = _databaseService.AddCustomer(customer);
                    Console.WriteLine($"Added customer: {customer.Name} (ID: {customerId})");
                }

                Console.WriteLine("Sample data added successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR adding sample data: {ex.Message}");
            }
        }

        /// <summary>
        /// Static method to run verification - can be called from Main or other entry points
        /// </summary>
        public static void RunDatabaseVerification()
        {
            var verifier = new DatabaseVerificationProgram();
            verifier.RunVerification();
        }

        /// <summary>
        /// Static method to add sample data - can be called from Main or other entry points
        /// </summary>
        public static void AddSampleDataToDatabase()
        {
            var verifier = new DatabaseVerificationProgram();
            verifier.AddSampleData();
        }
    }

    /// <summary>
    /// Simple console program that can be used to test database functionality
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Electrical Contractor System - Database Verification");
            Console.WriteLine("====================================================");
            
            if (args.Length > 0 && args[0].ToLower() == "addsample")
            {
                DatabaseVerificationProgram.AddSampleDataToDatabase();
            }
            else
            {
                DatabaseVerificationProgram.RunDatabaseVerification();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}