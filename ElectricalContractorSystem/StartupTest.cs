using System;
using System.Configuration;
using System.Windows;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem
{
    /// <summary>
    /// Startup test to check database connectivity and help debug issues
    /// </summary>
    public static class StartupTest
    {
        public static void RunDiagnostics()
        {
            string diagnosticInfo = "=== Electrical Contractor System Startup Diagnostics ===\n\n";
            
            try
            {
                // 1. Check configuration
                diagnosticInfo += "1. Configuration Check:\n";
                var connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"]?.ConnectionString;
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    diagnosticInfo += "   ❌ ERROR: Connection string 'ElectricalDB' not found in App.config\n";
                }
                else
                {
                    diagnosticInfo += "   ✓ Connection string found\n";
                    
                    // Parse connection string for display (hide password)
                    var builder = new MySqlConnectionStringBuilder(connectionString);
                    diagnosticInfo += $"   - Server: {builder.Server}\n";
                    diagnosticInfo += $"   - Database: {builder.Database}\n";
                    diagnosticInfo += $"   - User: {builder.UserID}\n";
                    diagnosticInfo += $"   - SSL Mode: {builder.SslMode}\n";
                }
                
                // 2. Test MySQL connection
                diagnosticInfo += "\n2. Database Connection Test:\n";
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    try
                    {
                        using (var connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            diagnosticInfo += "   ✓ Successfully connected to MySQL server\n";
                            
                            // Check if database exists
                            var command = new MySqlCommand("SELECT DATABASE()", connection);
                            var currentDb = command.ExecuteScalar()?.ToString();
                            diagnosticInfo += $"   ✓ Current database: {currentDb}\n";
                            
                            // Check for required tables
                            diagnosticInfo += "\n3. Database Tables Check:\n";
                            var tablesCommand = new MySqlCommand(@"
                                SELECT TABLE_NAME 
                                FROM INFORMATION_SCHEMA.TABLES 
                                WHERE TABLE_SCHEMA = @dbname
                                ORDER BY TABLE_NAME", connection);
                            tablesCommand.Parameters.AddWithValue("@dbname", currentDb);
                            
                            using (var reader = tablesCommand.ExecuteReader())
                            {
                                bool hasTables = false;
                                while (reader.Read())
                                {
                                    hasTables = true;
                                    diagnosticInfo += $"   ✓ Found table: {reader.GetString(0)}\n";
                                }
                                
                                if (!hasTables)
                                {
                                    diagnosticInfo += "   ⚠️ WARNING: No tables found in database\n";
                                    diagnosticInfo += "   - You need to run the database creation script\n";
                                    diagnosticInfo += "   - Check the /database folder for SQL scripts\n";
                                }
                            }
                        }
                    }
                    catch (MySqlException mysqlEx)
                    {
                        diagnosticInfo += $"   ❌ MySQL Error: {mysqlEx.Message}\n";
                        
                        if (mysqlEx.Message.Contains("Unknown database"))
                        {
                            diagnosticInfo += "\n   📌 TO FIX THIS:\n";
                            diagnosticInfo += "   1. Open MySQL Workbench or command line\n";
                            diagnosticInfo += "   2. Create the database: CREATE DATABASE electrical_contractor_db;\n";
                            diagnosticInfo += "   3. Run the schema script from /database folder\n";
                        }
                        else if (mysqlEx.Message.Contains("Access denied"))
                        {
                            diagnosticInfo += "\n   📌 TO FIX THIS:\n";
                            diagnosticInfo += "   1. Check your MySQL username and password\n";
                            diagnosticInfo += "   2. Update App.config with correct credentials\n";
                            diagnosticInfo += "   3. Make sure the user has access to the database\n";
                        }
                        else if (mysqlEx.Message.Contains("Unable to connect"))
                        {
                            diagnosticInfo += "\n   📌 TO FIX THIS:\n";
                            diagnosticInfo += "   1. Make sure MySQL Server is running\n";
                            diagnosticInfo += "   2. Check if it's running on localhost\n";
                            diagnosticInfo += "   3. Verify the port (default is 3306)\n";
                        }
                    }
                    catch (Exception ex)
                    {
                        diagnosticInfo += $"   ❌ General Error: {ex.Message}\n";
                    }
                }
                
                // 4. Check for required files
                diagnosticInfo += "\n4. Application Files Check:\n";
                var requiredViews = new[] { "JobManagementView", "WeeklyLaborEntryView", "MaterialEntryView", "JobCostTrackingView" };
                
                foreach (var view in requiredViews)
                {
                    var type = Type.GetType($"ElectricalContractorSystem.Views.{view}");
                    if (type != null)
                    {
                        diagnosticInfo += $"   ✓ {view} found\n";
                    }
                    else
                    {
                        diagnosticInfo += $"   ❌ {view} missing\n";
                    }
                }
                
                diagnosticInfo += "\n=== End of Diagnostics ===\n\n";
                diagnosticInfo += "Press OK to continue to the application...\n";
                
            }
            catch (Exception ex)
            {
                diagnosticInfo += $"\n❌ CRITICAL ERROR during diagnostics: {ex.Message}\n";
            }
            
            // Show diagnostic results
            MessageBox.Show(diagnosticInfo, "Startup Diagnostics", MessageBoxButton.OK, 
                diagnosticInfo.Contains("❌") ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
    }
}