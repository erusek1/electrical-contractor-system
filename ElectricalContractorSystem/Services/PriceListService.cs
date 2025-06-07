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

        #region Price List Management

        public List<PriceListItem> GetAllPriceListItems(bool activeOnly = true)
        {
            var items = new List<PriceListItem>();
            string query = @"
                SELECT * FROM PriceList 
                WHERE (@activeOnly = 0 OR is_active = 1)
                ORDER BY category, name";

            var parameters = new Dictionary<string, object>
            {
                ["@activeOnly"] = activeOnly ? 1 : 0
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    items.Add(MapPriceListItemFromReader(reader));
                }
            }

            return items;
        }

        public List<PriceListItem> GetPriceListItemsByCategory(string category, bool activeOnly = true)
        {
            var items = new List<PriceListItem>();
            string query = @"
                SELECT * FROM PriceList 
                WHERE category = @category AND (@activeOnly = 0 OR is_active = 1)
                ORDER BY name";

            var parameters = new Dictionary<string, object>
            {
                ["@category"] = category,
                ["@activeOnly"] = activeOnly ? 1 : 0
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    items.Add(MapPriceListItemFromReader(reader));
                }
            }

            return items;
        }

        public PriceListItem GetPriceListItemByCode(string itemCode)
        {
            string query = "SELECT * FROM PriceList WHERE item_code = @itemCode";
            var parameters = new Dictionary<string, object>
            {
                ["@itemCode"] = itemCode
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                if (reader.Read())
                {
                    return MapPriceListItemFromReader(reader);
                }
            }

            return null;
        }

        public List<PriceListItem> SearchPriceListItems(string searchTerm, bool activeOnly = true)
        {
            var items = new List<PriceListItem>();
            string query = @"
                SELECT * FROM PriceList 
                WHERE (item_code LIKE @searchTerm OR name LIKE @searchTerm OR description LIKE @searchTerm)
                      AND (@activeOnly = 0 OR is_active = 1)
                ORDER BY 
                    CASE 
                        WHEN item_code = @exactTerm THEN 1
                        WHEN item_code LIKE @startTerm THEN 2
                        WHEN name LIKE @startTerm THEN 3
                        ELSE 4
                    END,
                    name";

            var parameters = new Dictionary<string, object>
            {
                ["@searchTerm"] = $"%{searchTerm}%",
                ["@exactTerm"] = searchTerm,
                ["@startTerm"] = $"{searchTerm}%",
                ["@activeOnly"] = activeOnly ? 1 : 0
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    items.Add(MapPriceListItemFromReader(reader));
                }
            }

            return items;
        }

        public void SavePriceListItem(PriceListItem item)
        {
            if (item.ItemId == 0)
            {
                InsertPriceListItem(item);
            }
            else
            {
                UpdatePriceListItem(item);
            }
        }

        public void DeletePriceListItem(int itemId)
        {
            // Soft delete - set is_active to false
            string query = "UPDATE PriceList SET is_active = 0 WHERE item_id = @itemId";
            var parameters = new Dictionary<string, object>
            {
                ["@itemId"] = itemId
            };

            _databaseService.ExecuteNonQuery(query, parameters);
        }

        #endregion

        #region Category Management

        public List<string> GetCategories()
        {
            var categories = new List<string>();
            string query = "SELECT DISTINCT category FROM PriceList ORDER BY category";

            using (var reader = _databaseService.ExecuteReader(query))
            {
                while (reader.Read())
                {
                    categories.Add(reader.GetString("category"));
                }
            }

            return categories;
        }

        #endregion

        #region Quick Code Lookup

        public Dictionary<string, PriceListItem> GetQuickCodeMapping()
        {
            var mapping = new Dictionary<string, PriceListItem>(StringComparer.OrdinalIgnoreCase);
            
            // Get all active items with codes
            string query = @"
                SELECT * FROM PriceList 
                WHERE item_code IS NOT NULL AND item_code != '' AND is_active = 1";

            using (var reader = _databaseService.ExecuteReader(query))
            {
                while (reader.Read())
                {
                    var item = MapPriceListItemFromReader(reader);
                    if (!string.IsNullOrEmpty(item.ItemCode))
                    {
                        mapping[item.ItemCode] = item;
                    }
                }
            }

            return mapping;
        }

        public List<string> GetCommonItemCodes()
        {
            // Return the most commonly used item codes for quick entry
            var codes = new List<string>();
            
            // You can customize this query based on usage statistics
            string query = @"
                SELECT DISTINCT item_code 
                FROM PriceList 
                WHERE item_code IS NOT NULL AND item_code != '' AND is_active = 1
                ORDER BY 
                    CASE item_code
                        WHEN 'hh' THEN 1
                        WHEN 'O' THEN 2
                        WHEN 'S' THEN 3
                        WHEN '3W' THEN 4
                        WHEN 'Gfi' THEN 5
                        WHEN 'ARL' THEN 6
                        WHEN 'fridge' THEN 7
                        ELSE 999
                    END,
                    item_code
                LIMIT 20";

            using (var reader = _databaseService.ExecuteReader(query))
            {
                while (reader.Read())
                {
                    codes.Add(reader.GetString("item_code"));
                }
            }

            return codes;
        }

        #endregion

        #region Bulk Operations

        public void BulkUpdatePrices(decimal percentageIncrease, string category = null)
        {
            string query = @"
                UPDATE PriceList 
                SET base_cost = base_cost * (1 + @percentage / 100)
                WHERE is_active = 1";

            var parameters = new Dictionary<string, object>
            {
                ["@percentage"] = percentageIncrease
            };

            if (!string.IsNullOrEmpty(category))
            {
                query += " AND category = @category";
                parameters["@category"] = category;
            }

            _databaseService.ExecuteNonQuery(query, parameters);
        }

        public void ImportPriceListItems(List<PriceListItem> items)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in items)
                        {
                            // Check if item code already exists
                            string checkQuery = "SELECT item_id FROM PriceList WHERE item_code = @itemCode";
                            var checkCmd = new MySqlCommand(checkQuery, connection, transaction);
                            checkCmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            
                            var existingId = checkCmd.ExecuteScalar();
                            
                            if (existingId != null)
                            {
                                // Update existing item
                                item.ItemId = Convert.ToInt32(existingId);
                                UpdatePriceListItem(item, connection, transaction);
                            }
                            else
                            {
                                // Insert new item
                                InsertPriceListItem(item, connection, transaction);
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Price Calculation Helpers

        public decimal CalculateTotalPrice(string itemCode, int quantity, bool includeTax = true, bool includeMarkup = true)
        {
            var item = GetPriceListItemByCode(itemCode);
            if (item == null)
            {
                return 0;
            }

            decimal baseTotal = item.BaseCost * quantity;
            
            if (includeMarkup && item.MarkupPercentage > 0)
            {
                baseTotal *= (1 + item.MarkupPercentage / 100);
            }

            if (includeTax && item.TaxRate > 0)
            {
                baseTotal *= (1 + item.TaxRate);
            }

            return Math.Round(baseTotal, 2);
        }

        public int CalculateLaborMinutes(string itemCode, int quantity)
        {
            var item = GetPriceListItemByCode(itemCode);
            if (item == null)
            {
                return 0;
            }

            return item.LaborMinutes * quantity;
        }

        #endregion

        #region Private Helper Methods

        private PriceListItem MapPriceListItemFromReader(MySqlDataReader reader)
        {
            return new PriceListItem
            {
                ItemId = reader.GetInt32("item_id"),
                Category = reader.GetString("category"),
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
        }

        private void InsertPriceListItem(PriceListItem item)
        {
            string query = @"
                INSERT INTO PriceList (
                    category, item_code, name, description, base_cost,
                    tax_rate, labor_minutes, markup_percentage, is_active, notes
                ) VALUES (
                    @category, @itemCode, @name, @description, @baseCost,
                    @taxRate, @laborMinutes, @markupPercentage, @isActive, @notes
                )";

            var parameters = new Dictionary<string, object>
            {
                ["@category"] = item.Category,
                ["@itemCode"] = item.ItemCode,
                ["@name"] = item.Name,
                ["@description"] = item.Description ?? (object)DBNull.Value,
                ["@baseCost"] = item.BaseCost,
                ["@taxRate"] = item.TaxRate,
                ["@laborMinutes"] = item.LaborMinutes,
                ["@markupPercentage"] = item.MarkupPercentage,
                ["@isActive"] = item.IsActive,
                ["@notes"] = item.Notes ?? (object)DBNull.Value
            };

            _databaseService.ExecuteNonQuery(query, parameters);
        }

        private void InsertPriceListItem(PriceListItem item, MySqlConnection connection, MySqlTransaction transaction)
        {
            string query = @"
                INSERT INTO PriceList (
                    category, item_code, name, description, base_cost,
                    tax_rate, labor_minutes, markup_percentage, is_active, notes
                ) VALUES (
                    @category, @itemCode, @name, @description, @baseCost,
                    @taxRate, @laborMinutes, @markupPercentage, @isActive, @notes
                )";

            var cmd = new MySqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@category", item.Category);
            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
            cmd.Parameters.AddWithValue("@name", item.Name);
            cmd.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@baseCost", item.BaseCost);
            cmd.Parameters.AddWithValue("@taxRate", item.TaxRate);
            cmd.Parameters.AddWithValue("@laborMinutes", item.LaborMinutes);
            cmd.Parameters.AddWithValue("@markupPercentage", item.MarkupPercentage);
            cmd.Parameters.AddWithValue("@isActive", item.IsActive);
            cmd.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
            item.ItemId = (int)cmd.LastInsertedId;
        }

        private void UpdatePriceListItem(PriceListItem item)
        {
            string query = @"
                UPDATE PriceList SET
                    category = @category, item_code = @itemCode, name = @name,
                    description = @description, base_cost = @baseCost,
                    tax_rate = @taxRate, labor_minutes = @laborMinutes,
                    markup_percentage = @markupPercentage, is_active = @isActive, notes = @notes
                WHERE item_id = @itemId";

            var parameters = new Dictionary<string, object>
            {
                ["@itemId"] = item.ItemId,
                ["@category"] = item.Category,
                ["@itemCode"] = item.ItemCode,
                ["@name"] = item.Name,
                ["@description"] = item.Description ?? (object)DBNull.Value,
                ["@baseCost"] = item.BaseCost,
                ["@taxRate"] = item.TaxRate,
                ["@laborMinutes"] = item.LaborMinutes,
                ["@markupPercentage"] = item.MarkupPercentage,
                ["@isActive"] = item.IsActive,
                ["@notes"] = item.Notes ?? (object)DBNull.Value
            };

            _databaseService.ExecuteNonQuery(query, parameters);
        }

        private void UpdatePriceListItem(PriceListItem item, MySqlConnection connection, MySqlTransaction transaction)
        {
            string query = @"
                UPDATE PriceList SET
                    category = @category, item_code = @itemCode, name = @name,
                    description = @description, base_cost = @baseCost,
                    tax_rate = @taxRate, labor_minutes = @laborMinutes,
                    markup_percentage = @markupPercentage, is_active = @isActive, notes = @notes
                WHERE item_id = @itemId";

            var cmd = new MySqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@itemId", item.ItemId);
            cmd.Parameters.AddWithValue("@category", item.Category);
            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
            cmd.Parameters.AddWithValue("@name", item.Name);
            cmd.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@baseCost", item.BaseCost);
            cmd.Parameters.AddWithValue("@taxRate", item.TaxRate);
            cmd.Parameters.AddWithValue("@laborMinutes", item.LaborMinutes);
            cmd.Parameters.AddWithValue("@markupPercentage", item.MarkupPercentage);
            cmd.Parameters.AddWithValue("@isActive", item.IsActive);
            cmd.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Stage Mapping Configuration

        public Dictionary<string, string> GetItemStageMapping()
        {
            // This could be made configurable in the database
            // For now, returning a hardcoded mapping based on your business rules
            
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Service stage items
            mapping["panel"] = "Service";
            mapping["meter"] = "Service";
            mapping["service"] = "Service";
            mapping["main"] = "Service";
            mapping["disconnect"] = "Service";

            // Rough stage items
            mapping["wire"] = "Rough";
            mapping["12/2"] = "Rough";
            mapping["14/2"] = "Rough";
            mapping["10/2"] = "Rough";
            mapping["10/3"] = "Rough";
            mapping["12/3"] = "Rough";
            mapping["14/3"] = "Rough";
            mapping["pipe"] = "Rough";
            mapping["conduit"] = "Rough";
            mapping["box"] = "Rough";
            mapping["jbox"] = "Rough";
            mapping["romex"] = "Rough";
            mapping["thhn"] = "Rough";
            mapping["ground"] = "Rough";

            // Demo stage items
            mapping["demo"] = "Demo";
            mapping["remove"] = "Demo";
            mapping["demolish"] = "Demo";

            // Most other items default to Finish stage
            // (switches, outlets, fixtures, etc.)

            return mapping;
        }

        public string GetStageForItem(string itemCode, string itemName)
        {
            if (string.IsNullOrEmpty(itemCode) && string.IsNullOrEmpty(itemName))
                return "Finish";

            var stageMapping = GetItemStageMapping();

            // Check item code first
            if (!string.IsNullOrEmpty(itemCode))
            {
                var code = itemCode.ToLower();
                
                // Direct match
                if (stageMapping.ContainsKey(code))
                    return stageMapping[code];

                // Partial match
                foreach (var mapping in stageMapping)
                {
                    if (code.Contains(mapping.Key))
                        return mapping.Value;
                }
            }

            // Check item name
            if (!string.IsNullOrEmpty(itemName))
            {
                var name = itemName.ToLower();
                
                foreach (var mapping in stageMapping)
                {
                    if (name.Contains(mapping.Key))
                        return mapping.Value;
                }
            }

            // Default to Finish stage
            return "Finish";
        }

        #endregion
    }
}
