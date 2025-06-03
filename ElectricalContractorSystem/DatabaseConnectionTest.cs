using System;
using System.Windows;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem
{
    /// <summary>
    /// Simple class to test database connectivity
    /// </summary>
    public static class DatabaseConnectionTest
    {
        /// <summary>
        /// Tests the database connection and shows results
        /// </summary>
        public static void TestConnection()
        {
            try
            {
                using (var dbService = new DatabaseService())
                {
                    MessageBox.Show("Testing database connection...", "Database Test", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    bool connectionResult = dbService.TestConnection();
                    
                    if (connectionResult)
                    {
                        // Test a simple query
                        var result = dbService.ExecuteScalar("SELECT COUNT(*) FROM Jobs");
                        int jobCount = Convert.ToInt32(result ?? 0);
                        
                        var customerResult = dbService.ExecuteScalar("SELECT COUNT(*) FROM Customers");
                        int customerCount = Convert.ToInt32(customerResult ?? 0);
                        
                        var employeeResult = dbService.ExecuteScalar("SELECT COUNT(*) FROM Employees");
                        int employeeCount = Convert.ToInt32(employeeResult ?? 0);
                        
                        MessageBox.Show(
                            $"✅ Database connection successful!\n\n" +
                            $"Current Data:\n" +
                            $"• Jobs: {jobCount}\n" +
                            $"• Customers: {customerCount}\n" +
                            $"• Employees: {employeeCount}\n\n" +
                            $"Your application is ready to use!",
                            "Database Connection Test - SUCCESS",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "❌ Database connection failed!\n\n" +
                            "Please check:\n" +
                            "• MySQL server is running\n" +
                            "• Database 'electrical_contractor_db' exists\n" +
                            "• Connection string in App.config is correct\n" +
                            "• Username/password are correct\n\n" +
                            "The application will use test data instead.",
                            "Database Connection Test - FAILED",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Database connection error!\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"Please check:\n" +
                    "• MySQL server is running\n" +
                    "• Database 'electrical_contractor_db' exists\n" +
                    "• Connection string in App.config is correct\n" +
                    "• MySQL connector is installed\n\n" +
                    "The application will use test data instead.",
                    "Database Connection Test - ERROR",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Creates sample data for testing
        /// </summary>
        public static void CreateSampleData()
        {
            try
            {
                using (var dbService = new DatabaseService())
                {
                    if (!dbService.TestConnection())
                    {
                        MessageBox.Show("Cannot create sample data - database connection failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // Check if we already have data
                    var jobCount = Convert.ToInt32(dbService.ExecuteScalar("SELECT COUNT(*) FROM Jobs") ?? 0);
                    
                    if (jobCount > 0)
                    {
                        var result = MessageBox.Show(
                            $"Database already contains {jobCount} jobs.\n\n" +
                            "Do you want to add more sample data?",
                            "Sample Data",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                            
                        if (result == MessageBoxResult.No)
                            return;
                    }
                    
                    // Insert sample customers
                    var customers = new[]
                    {
                        ("Smith Residence", "123 Main St", "Manasquan", "NJ", "08736"),
                        ("Johnson Remodel", "456 Oak Ave", "Wall", "NJ", "07719"),
                        ("Bayshore Contractors", "789 Beach Blvd", "Belmar", "NJ", "07719"),
                        ("Williams Kitchen", "321 Pine St", "Spring Lake", "NJ", "07762"),
                        ("MPC Builders", "555 Seaside Dr", "Brielle", "NJ", "08736")
                    };
                    
                    foreach (var (name, address, city, state, zip) in customers)
                    {
                        // Check if customer already exists
                        var existingCustomer = dbService.ExecuteScalar(
                            "SELECT customer_id FROM Customers WHERE name = @name",
                            new System.Collections.Generic.Dictionary<string, object> { { "@name", name } });
                            
                        if (existingCustomer == null)
                        {
                            dbService.ExecuteNonQuery(
                                "INSERT INTO Customers (name, address, city, state, zip) VALUES (@name, @address, @city, @state, @zip)",
                                new System.Collections.Generic.Dictionary<string, object>
                                {
                                    { "@name", name },
                                    { "@address", address },
                                    { "@city", city },
                                    { "@state", state },
                                    { "@zip", zip }
                                });
                        }
                    }
                    
                    // Get customer IDs
                    var smithId = dbService.ExecuteScalar("SELECT customer_id FROM Customers WHERE name = 'Smith Residence'");
                    var johnsonId = dbService.ExecuteScalar("SELECT customer_id FROM Customers WHERE name = 'Johnson Remodel'");
                    var bayshoreId = dbService.ExecuteScalar("SELECT customer_id FROM Customers WHERE name = 'Bayshore Contractors'");
                    
                    if (smithId != null && johnsonId != null && bayshoreId != null)
                    {
                        // Insert sample jobs
                        var jobs = new[]
                        {
                            ("624", Convert.ToInt32(smithId), "Smith Kitchen Addition", "123 Main St", "Manasquan", "NJ", "08736", "Estimate", DateTime.Now.AddDays(-30), 35000.00m),
                            ("625", Convert.ToInt32(johnsonId), "Johnson Basement Finish", "456 Oak Ave", "Wall", "NJ", "07719", "In Progress", DateTime.Now.AddDays(-15), 18500.00m),
                            ("626", Convert.ToInt32(bayshoreId), "Bayshore New Construction", "789 Beach Blvd", "Belmar", "NJ", "07719", "In Progress", DateTime.Now.AddDays(-45), 75000.00m)
                        };
                        
                        foreach (var (jobNumber, customerId, jobName, address, city, state, zip, status, createDate, estimate) in jobs)
                        {
                            // Check if job already exists
                            var existingJob = dbService.ExecuteScalar(
                                "SELECT job_id FROM Jobs WHERE job_number = @jobNumber",
                                new System.Collections.Generic.Dictionary<string, object> { { "@jobNumber", jobNumber } });
                                
                            if (existingJob == null)
                            {
                                dbService.ExecuteNonQuery(
                                    "INSERT INTO Jobs (job_number, customer_id, job_name, address, city, state, zip, status, create_date, total_estimate) " +
                                    "VALUES (@jobNumber, @customerId, @jobName, @address, @city, @state, @zip, @status, @createDate, @totalEstimate)",
                                    new System.Collections.Generic.Dictionary<string, object>
                                    {
                                        { "@jobNumber", jobNumber },
                                        { "@customerId", customerId },
                                        { "@jobName", jobName },
                                        { "@address", address },
                                        { "@city", city },
                                        { "@state", state },
                                        { "@zip", zip },
                                        { "@status", status },
                                        { "@createDate", createDate },
                                        { "@totalEstimate", estimate }
                                    });
                            }
                        }
                    }
                    
                    // Count final results
                    var finalJobCount = Convert.ToInt32(dbService.ExecuteScalar("SELECT COUNT(*) FROM Jobs") ?? 0);
                    var finalCustomerCount = Convert.ToInt32(dbService.ExecuteScalar("SELECT COUNT(*) FROM Customers") ?? 0);
                    
                    MessageBox.Show(
                        $"✅ Sample data created successfully!\n\n" +
                        $"Database now contains:\n" +
                        $"• Jobs: {finalJobCount}\n" +
                        $"• Customers: {finalCustomerCount}\n\n" +
                        $"You can now test the application with real data!",
                        "Sample Data Created",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error creating sample data!\n\n" +
                    $"Error: {ex.Message}",
                    "Sample Data Creation Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
