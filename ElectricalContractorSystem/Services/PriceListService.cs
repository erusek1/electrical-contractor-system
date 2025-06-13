using System;
using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    public class PriceListService
    {
        private readonly DatabaseService _databaseService;

        public PriceListService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public List<PriceListItem> GetAllPriceListItems(bool activeOnly = true)
        {
            var items = new List<PriceListItem>();
            
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                var query = @"
                    SELECT item_id, category, item_code, name, description, 
                           base_cost, tax_rate, labor_minutes, markup_percentage, 
                           is_active, notes, rough_minutes, finish_minutes, 
                           service_minutes, extra_minutes
                    FROM PriceList";

                if (activeOnly)
                {
                    query += " WHERE is_active = 1";
                }

                query += " ORDER BY category, name";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PriceListItem
                            {
                                ItemId = reader.GetInt32("item_id"),
                                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                                ItemCode = reader.GetString("item_code"),
                                Name = reader.GetString("name"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                BaseCost = reader.GetDecimal("base_cost"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                LaborMinutes = reader.GetInt32("labor_minutes"),
                                MarkupPercentage = reader.GetDecimal("markup_percentage"),
                                IsActive = reader.GetBoolean("is_active"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                            };

                            // Load stage-specific labor minutes if they exist
                            if (!reader.IsDBNull(reader.GetOrdinal("rough_minutes")))
                            {
                                item.RoughMinutes = reader.GetInt32("rough_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("finish_minutes")))
                            {
                                item.FinishMinutes = reader.GetInt32("finish_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("service_minutes")))
                            {
                                item.ServiceMinutes = reader.GetInt32("service_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("extra_minutes")))
                            {
                                item.ExtraMinutes = reader.GetInt32("extra_minutes");
                            }

                            items.Add(item);
                        }
                    }
                }
            }
            
            return items;
        }

        public PriceListItem GetPriceListItemByCode(string itemCode)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                var query = @"
                    SELECT item_id, category, item_code, name, description, 
                           base_cost, tax_rate, labor_minutes, markup_percentage, 
                           is_active, notes, rough_minutes, finish_minutes, 
                           service_minutes, extra_minutes
                    FROM PriceList
                    WHERE item_code = @itemCode AND is_active = 1";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@itemCode", itemCode);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var item = new PriceListItem
                            {
                                ItemId = reader.GetInt32("item_id"),
                                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                                ItemCode = reader.GetString("item_code"),
                                Name = reader.GetString("name"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                BaseCost = reader.GetDecimal("base_cost"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                LaborMinutes = reader.GetInt32("labor_minutes"),
                                MarkupPercentage = reader.GetDecimal("markup_percentage"),
                                IsActive = reader.GetBoolean("is_active"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                            };

                            // Load stage-specific labor minutes if they exist
                            if (!reader.IsDBNull(reader.GetOrdinal("rough_minutes")))
                            {
                                item.RoughMinutes = reader.GetInt32("rough_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("finish_minutes")))
                            {
                                item.FinishMinutes = reader.GetInt32("finish_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("service_minutes")))
                            {
                                item.ServiceMinutes = reader.GetInt32("service_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("extra_minutes")))
                            {
                                item.ExtraMinutes = reader.GetInt32("extra_minutes");
                            }

                            return item;
                        }
                    }
                }
            }
            
            return null;
        }

        public List<PriceListItem> SearchPriceListItems(string searchTerm)
        {
            var items = new List<PriceListItem>();
            
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                var query = @"
                    SELECT item_id, category, item_code, name, description, 
                           base_cost, tax_rate, labor_minutes, markup_percentage, 
                           is_active, notes, rough_minutes, finish_minutes, 
                           service_minutes, extra_minutes
                    FROM PriceList
                    WHERE is_active = 1 
                    AND (item_code LIKE @searchTerm 
                         OR name LIKE @searchTerm 
                         OR description LIKE @searchTerm)
                    ORDER BY 
                        CASE 
                            WHEN item_code = @exactTerm THEN 1
                            WHEN item_code LIKE @startTerm THEN 2
                            WHEN name LIKE @startTerm THEN 3
                            ELSE 4
                        END,
                        item_code, name";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
                    command.Parameters.AddWithValue("@exactTerm", searchTerm);
                    command.Parameters.AddWithValue("@startTerm", $"{searchTerm}%");
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PriceListItem
                            {
                                ItemId = reader.GetInt32("item_id"),
                                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                                ItemCode = reader.GetString("item_code"),
                                Name = reader.GetString("name"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                BaseCost = reader.GetDecimal("base_cost"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                LaborMinutes = reader.GetInt32("labor_minutes"),
                                MarkupPercentage = reader.GetDecimal("markup_percentage"),
                                IsActive = reader.GetBoolean("is_active"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                            };

                            // Load stage-specific labor minutes if they exist
                            if (!reader.IsDBNull(reader.GetOrdinal("rough_minutes")))
                            {
                                item.RoughMinutes = reader.GetInt32("rough_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("finish_minutes")))
                            {
                                item.FinishMinutes = reader.GetInt32("finish_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("service_minutes")))
                            {
                                item.ServiceMinutes = reader.GetInt32("service_minutes");
                            }
                            if (!reader.IsDBNull(reader.GetOrdinal("extra_minutes")))
                            {
                                item.ExtraMinutes = reader.GetInt32("extra_minutes");
                            }

                            items.Add(item);
                        }
                    }
                }
            }
            
            return items;
        }

        public void SavePriceListItem(PriceListItem item)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                
                if (item.ItemId == 0)
                {
                    InsertPriceListItem(connection, item);
                }
                else
                {
                    UpdatePriceListItem(connection, item);
                }
            }
        }

        private void InsertPriceListItem(MySqlConnection connection, PriceListItem item)
        {
            var query = @"
                INSERT INTO PriceList (category, item_code, name, description, 
                                      base_cost, tax_rate, labor_minutes, markup_percentage, 
                                      is_active, notes, rough_minutes, finish_minutes, 
                                      service_minutes, extra_minutes)
                VALUES (@category, @item_code, @name, @description,
                        @base_cost, @tax_rate, @labor_minutes, @markup_percentage,
                        @is_active, @notes, @rough_minutes, @finish_minutes,
                        @service_minutes, @extra_minutes)";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@category", item.Category ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@item_code", item.ItemCode);
                command.Parameters.AddWithValue("@name", item.Name);
                command.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@base_cost", item.BaseCost);
                command.Parameters.AddWithValue("@tax_rate", item.TaxRate);
                command.Parameters.AddWithValue("@labor_minutes", item.LaborMinutes);
                command.Parameters.AddWithValue("@markup_percentage", item.MarkupPercentage);
                command.Parameters.AddWithValue("@is_active", item.IsActive);
                command.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@rough_minutes", item.RoughMinutes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@finish_minutes", item.FinishMinutes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@service_minutes", item.ServiceMinutes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@extra_minutes", item.ExtraMinutes ?? (object)DBNull.Value);

                command.ExecuteNonQuery();
                item.ItemId = (int)command.LastInsertedId;
            }
        }

        private void UpdatePriceListItem(MySqlConnection connection, PriceListItem item)
        {
            var query = @"
                UPDATE PriceList SET
                    category = @category,
                    item_code = @item_code,
                    name = @name,
                    description = @description,
                    base_cost = @base_cost,
                    tax_rate = @tax_rate,
                    labor_minutes = @labor_minutes,
                    markup_percentage = @markup_percentage,
                    is_active = @is_active,
                    notes = @notes,
                    rough_minutes = @rough_minutes,
                    finish_minutes = @finish_minutes,
                    service_minutes = @service_minutes,
                    extra_minutes = @extra_minutes
                WHERE item_id = @item_id";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@item_id", item.ItemId);
                command.Parameters.AddWithValue("@category", item.Category ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@item_code", item.ItemCode);
                command.Parameters.AddWithValue("@name", item.Name);
                command.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@base_cost", item.BaseCost);
                command.Parameters.AddWithValue("@tax_rate", item.TaxRate);
                command.Parameters.AddWithValue("@labor_minutes", item.LaborMinutes);
                command.Parameters.AddWithValue("@markup_percentage", item.MarkupPercentage);
                command.Parameters.AddWithValue("@is_active", item.IsActive);
                command.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@rough_minutes", item.RoughMinutes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@finish_minutes", item.FinishMinutes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@service_minutes", item.ServiceMinutes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@extra_minutes", item.ExtraMinutes ?? (object)DBNull.Value);

                command.ExecuteNonQuery();
            }
        }

        public void DeletePriceListItem(int itemId)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                
                // Soft delete - just mark as inactive
                var query = "UPDATE PriceList SET is_active = 0 WHERE item_id = @item_id";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@item_id", itemId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool ItemCodeExists(string itemCode, int excludeItemId = 0)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                var query = "SELECT COUNT(*) FROM PriceList WHERE item_code = @item_code AND item_id != @exclude_id";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@item_code", itemCode);
                    command.Parameters.AddWithValue("@exclude_id", excludeItemId);
                    
                    var count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public List<string> GetCategories()
        {
            var categories = new List<string>();
            
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                var query = "SELECT DISTINCT category FROM PriceList WHERE category IS NOT NULL ORDER BY category";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(reader.GetString("category"));
                        }
                    }
                }
            }
            
            return categories;
        }

        public Dictionary<string, PriceListItem> GetItemsByCodeDictionary()
        {
            var items = GetAllPriceListItems(true);
            return items.ToDictionary(i => i.ItemCode, i => i);
        }
    }
}
