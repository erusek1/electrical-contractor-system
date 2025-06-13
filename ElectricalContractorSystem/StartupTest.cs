using System;
using System.Configuration;
using System.Text;
using System.Windows;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem
{
    public static class StartupTest
    {
        public static void RunDiagnostics()
        {
            var diagnostics = new StringBuilder();
            diagnostics.AppendLine("=== Electrical Contractor System Startup Diagnostics ===");
            diagnostics.AppendLine($"Date: {DateTime.Now}");
            diagnostics.AppendLine();

            // Check connection string
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"];
                if (connectionString != null)
                {
                    diagnostics.AppendLine("✓ Connection string found in App.config");
                    diagnostics.AppendLine($"  Connection Name: {connectionString.Name}");
                    diagnostics.AppendLine($"  Provider: {connectionString.ProviderName}");
                    
                    // Parse connection string for display (hide password)
                    var builder = new MySqlConnectionStringBuilder(connectionString.ConnectionString);
                    diagnostics.AppendLine($"  Server: {builder.Server}");
                    diagnostics.AppendLine($"  Port: {builder.Port}");
                    diagnostics.AppendLine($"  Database: {builder.Database}");
                    diagnostics.AppendLine($"  User ID: {builder.UserID}");
                    diagnostics.AppendLine($"  SSL Mode: {builder.SslMode}");
                }
                else
                {
                    diagnostics.AppendLine("✗ Connection string 'ElectricalDB' not found in App.config");
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"✗ Error reading connection string: {ex.Message}");
            }

            diagnostics.AppendLine();

            // Test database connection
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"]?.ConnectionString;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    diagnostics.AppendLine("Testing database connection...");
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        diagnostics.AppendLine("✓ Successfully connected to MySQL server");

                        // Check if database exists
                        using (var cmd = new MySqlCommand("SELECT DATABASE()", connection))
                        {
                            var dbName = cmd.ExecuteScalar()?.ToString();
                            diagnostics.AppendLine($"✓ Connected to database: {dbName}");
                        }

                        // Check for required tables
                        var requiredTables = new[] { "Customers", "Jobs", "Employees", "Vendors", "JobStages", 
                                                     "LaborEntries", "MaterialEntries", "PriceList", 
                                                     "RoomSpecifications", "PermitItems" };
                        
                        diagnostics.AppendLine();
                        diagnostics.AppendLine("Checking for required tables:");
                        
                        foreach (var table in requiredTables)
                        {
                            using (var cmd = new MySqlCommand($"SHOW TABLES LIKE '{table}'", connection))
                            {
                                var tableResult = cmd.ExecuteScalar();
                                if (tableResult != null)
                                {
                                    diagnostics.AppendLine($"  ✓ Table '{table}' exists");
                                }
                                else
                                {
                                    diagnostics.AppendLine($"  ✗ Table '{table}' NOT FOUND");
                                }
                            }
                        }
                    }
                }
                else
                {
                    diagnostics.AppendLine("✗ Connection string is empty or null");
                }
            }
            catch (MySqlException ex)
            {
                diagnostics.AppendLine($"✗ MySQL Error: {ex.Message}");
                diagnostics.AppendLine();
                diagnostics.AppendLine("Common causes:");
                diagnostics.AppendLine("  1. MySQL server is not running");
                diagnostics.AppendLine("  2. Database 'electrical_contractor_db' does not exist");
                diagnostics.AppendLine("  3. Invalid username or password");
                diagnostics.AppendLine("  4. MySQL is not installed");
                diagnostics.AppendLine();
                diagnostics.AppendLine("To fix:");
                diagnostics.AppendLine("  1. Ensure MySQL is installed and running");
                diagnostics.AppendLine("  2. Create the database by running the SQL script in /database/electrical_contractor_db.sql");
                diagnostics.AppendLine("  3. Update the connection string in App.config with your MySQL credentials");
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"✗ General Error: {ex.GetType().Name} - {ex.Message}");
            }

            // Show results
            var dialogResult = MessageBox.Show(
                diagnostics.ToString() + "\n\nWould you like to continue starting the application?",
                "Startup Diagnostics",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (dialogResult == MessageBoxResult.No)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
