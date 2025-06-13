using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        // Extended methods for estimates that aren't in the main file
        
        #region Employee Methods
        
        public List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();
            const string query = "SELECT * FROM Employees ORDER BY Name";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new Employee
                        {
                            EmployeeId = reader.GetInt32("employee_id"),
                            Name = reader.GetString("name"),
                            HourlyRate = reader.GetDecimal("hourly_rate"),
                            BurdenRate = reader.IsDBNull(reader.GetOrdinal("burden_rate")) ? (decimal?)null : reader.GetDecimal("burden_rate"),
                            VehicleCostPerMonth = reader.IsDBNull(reader.GetOrdinal("vehicle_cost_per_month")) ? (decimal?)null : reader.GetDecimal("vehicle_cost_per_month"),
                            OverheadPercentage = reader.IsDBNull(reader.GetOrdinal("overhead_percentage")) ? (decimal?)null : reader.GetDecimal("overhead_percentage"),
                            Status = reader.GetString("status"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                        });
                    }
                }
            }
            
            return employees;
        }
        
        public void SaveEmployee(Employee employee)
        {
            const string query = @"
                INSERT INTO Employees (name, hourly_rate, burden_rate, vehicle_cost_per_month, overhead_percentage, status, notes)
                VALUES (@name, @hourlyRate, @burdenRate, @vehicleCost, @overhead, @status, @notes)";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@name", employee.Name);
                    command.Parameters.AddWithValue("@hourlyRate", employee.HourlyRate);
                    command.Parameters.AddWithValue("@burdenRate", employee.BurdenRate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@vehicleCost", employee.VehicleCostPerMonth ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@overhead", employee.OverheadPercentage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@status", employee.Status ?? "Active");
                    command.Parameters.AddWithValue("@notes", employee.Notes ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                    employee.EmployeeId = (int)command.LastInsertedId;
                }
            }
        }
        
        public void UpdateEmployee(Employee employee)
        {
            const string query = @"
                UPDATE Employees 
                SET name = @name, 
                    hourly_rate = @hourlyRate, 
                    burden_rate = @burdenRate,
                    vehicle_cost_per_month = @vehicleCost,
                    overhead_percentage = @overhead,
                    status = @status, 
                    notes = @notes
                WHERE employee_id = @employeeId";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@employeeId", employee.EmployeeId);
                    command.Parameters.AddWithValue("@name", employee.Name);
                    command.Parameters.AddWithValue("@hourlyRate", employee.HourlyRate);
                    command.Parameters.AddWithValue("@burdenRate", employee.BurdenRate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@vehicleCost", employee.VehicleCostPerMonth ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@overhead", employee.OverheadPercentage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@status", employee.Status ?? "Active");
                    command.Parameters.AddWithValue("@notes", employee.Notes ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public void DeleteEmployee(int employeeId)
        {
            const string query = "DELETE FROM Employees WHERE employee_id = @employeeId";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@employeeId", employeeId);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        #endregion
        
        #region Customer Methods
        
        public void DeleteCustomer(int customerId)
        {
            const string query = "DELETE FROM Customers WHERE customer_id = @customerId";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@customerId", customerId);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        #endregion
        
        #region Price List Methods
        
        public List<PriceList> GetAllPriceListItems()
        {
            var items = new List<PriceList>();
            const string query = "SELECT * FROM PriceList ORDER BY category, name";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new PriceList
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Category = reader.GetString("category"),
                            ItemCode = reader.GetString("item_code"),
                            Name = reader.GetString("name"),
                            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                            BaseCost = reader.GetDecimal("base_cost"),
                            TaxRate = reader.IsDBNull(reader.GetOrdinal("tax_rate")) ? (decimal?)null : reader.GetDecimal("tax_rate"),
                            LaborMinutes = reader.IsDBNull(reader.GetOrdinal("labor_minutes")) ? (int?)null : reader.GetInt32("labor_minutes"),
                            MarkupPercentage = reader.IsDBNull(reader.GetOrdinal("markup_percentage")) ? (decimal?)null : reader.GetDecimal("markup_percentage"),
                            IsActive = reader.GetBoolean("is_active"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                        });
                    }
                }
            }
            
            return items;
        }
        
        public void SavePriceListItem(PriceList item)
        {
            const string query = @"
                INSERT INTO PriceList (category, item_code, name, description, base_cost, tax_rate, labor_minutes, markup_percentage, is_active, notes)
                VALUES (@category, @itemCode, @name, @description, @baseCost, @taxRate, @laborMinutes, @markupPercentage, @isActive, @notes)";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@category", item.Category);
                    command.Parameters.AddWithValue("@itemCode", item.ItemCode);
                    command.Parameters.AddWithValue("@name", item.Name);
                    command.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@baseCost", item.BaseCost);
                    command.Parameters.AddWithValue("@taxRate", item.TaxRate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@laborMinutes", item.LaborMinutes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@markupPercentage", item.MarkupPercentage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@isActive", item.IsActive);
                    command.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                    item.ItemId = (int)command.LastInsertedId;
                }
            }
        }
        
        public void UpdatePriceListItem(PriceList item)
        {
            const string query = @"
                UPDATE PriceList 
                SET category = @category,
                    item_code = @itemCode,
                    name = @name,
                    description = @description,
                    base_cost = @baseCost,
                    tax_rate = @taxRate,
                    labor_minutes = @laborMinutes,
                    markup_percentage = @markupPercentage,
                    is_active = @isActive,
                    notes = @notes
                WHERE item_id = @itemId";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@itemId", item.ItemId);
                    command.Parameters.AddWithValue("@category", item.Category);
                    command.Parameters.AddWithValue("@itemCode", item.ItemCode);
                    command.Parameters.AddWithValue("@name", item.Name);
                    command.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@baseCost", item.BaseCost);
                    command.Parameters.AddWithValue("@taxRate", item.TaxRate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@laborMinutes", item.LaborMinutes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@markupPercentage", item.MarkupPercentage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@isActive", item.IsActive);
                    command.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public void DeletePriceListItem(int itemId)
        {
            const string query = "DELETE FROM PriceList WHERE item_id = @itemId";
            
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@itemId", itemId);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        #endregion
    }
}
