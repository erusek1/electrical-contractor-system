using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    // Extension methods for DatabaseService to support pricing and assembly features
    public static class DatabaseServicePricingExtensions
    {
        #region Material Methods
        
        public static List<Material> GetAllMaterials(this DatabaseService service)
        {
            var materials = new List<Material>();
            
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT * FROM Materials WHERE is_active = TRUE ORDER BY category, name";
                
                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        materials.Add(ReadMaterial(reader));
                    }
                }
            }
            
            return materials;
        }
        
        public static Material GetMaterialById(this DatabaseService service, int materialId)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT * FROM Materials WHERE material_id = @materialId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@materialId", materialId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadMaterial(reader);
                        }
                    }
                }
            }
            
            return null;
        }
        
        public static void UpdateMaterial(this DatabaseService service, Material material)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"UPDATE Materials SET 
                    current_price = @price,
                    updated_date = @updated
                    WHERE material_id = @materialId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@materialId", material.MaterialId);
                    command.Parameters.AddWithValue("@price", material.CurrentPrice);
                    command.Parameters.AddWithValue("@updated", DateTime.Now);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
        
        #endregion
        
        #region Material Price History Methods
        
        public static void SaveMaterialPriceHistory(this DatabaseService service, MaterialPriceHistory history)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"INSERT INTO MaterialPriceHistory 
                    (material_id, price, effective_date, vendor_id, purchase_order_number, 
                     quantity_purchased, notes, created_by)
                    VALUES (@materialId, @price, @effectiveDate, @vendorId, @poNumber, 
                            @quantity, @notes, @createdBy)";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@materialId", history.MaterialId);
                    command.Parameters.AddWithValue("@price", history.Price);
                    command.Parameters.AddWithValue("@effectiveDate", history.EffectiveDate);
                    command.Parameters.AddWithValue("@vendorId", history.VendorId.HasValue ? (object)history.VendorId : DBNull.Value);
                    command.Parameters.AddWithValue("@poNumber", history.PurchaseOrderNumber ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@quantity", history.QuantityPurchased.HasValue ? (object)history.QuantityPurchased : DBNull.Value);
                    command.Parameters.AddWithValue("@notes", history.Notes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@createdBy", history.CreatedBy);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public static List<MaterialPriceHistory> GetMaterialPriceHistory(this DatabaseService service, 
            int materialId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var history = new List<MaterialPriceHistory>();
            
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT mph.*, m.name as material_name, v.name as vendor_name,
                    LAG(mph.price) OVER (PARTITION BY mph.material_id ORDER BY mph.effective_date) as prev_price
                    FROM MaterialPriceHistory mph
                    LEFT JOIN Materials m ON mph.material_id = m.material_id
                    LEFT JOIN Vendors v ON mph.vendor_id = v.vendor_id
                    WHERE mph.material_id = @materialId";
                
                if (startDate.HasValue)
                    query += " AND mph.effective_date >= @startDate";
                if (endDate.HasValue)
                    query += " AND mph.effective_date <= @endDate";
                    
                query += " ORDER BY mph.effective_date DESC";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@materialId", materialId);
                    if (startDate.HasValue)
                        command.Parameters.AddWithValue("@startDate", startDate.Value);
                    if (endDate.HasValue)
                        command.Parameters.AddWithValue("@endDate", endDate.Value);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = ReadMaterialPriceHistory(reader);
                            
                            // Calculate percentage change
                            if (!reader.IsDBNull(reader.GetOrdinal("prev_price")))
                            {
                                decimal prevPrice = reader.GetDecimal("prev_price");
                                if (prevPrice != 0)
                                {
                                    item.PercentageChangeFromPrevious = ((item.Price - prevPrice) / prevPrice) * 100;
                                }
                            }
                            
                            history.Add(item);
                        }
                    }
                }
            }
            
            return history;
        }
        
        #endregion
        
        #region Assembly Methods
        
        public static List<AssemblyTemplate> GetAllAssemblies(this DatabaseService service)
        {
            var assemblies = new List<AssemblyTemplate>();
            
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT * FROM AssemblyTemplates ORDER BY category, assembly_code";
                
                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        assemblies.Add(ReadAssemblyTemplate(reader));
                    }
                }
                
                // Load components for each assembly
                foreach (var assembly in assemblies)
                {
                    assembly.Components = service.GetAssemblyComponents(assembly.AssemblyId);
                }
            }
            
            return assemblies;
        }
        
        public static AssemblyTemplate GetAssemblyById(this DatabaseService service, int assemblyId)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT * FROM AssemblyTemplates WHERE assembly_id = @assemblyId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@assemblyId", assemblyId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var assembly = ReadAssemblyTemplate(reader);
                            reader.Close();
                            
                            // Load components
                            assembly.Components = service.GetAssemblyComponents(assemblyId);
                            
                            return assembly;
                        }
                    }
                }
            }
            
            return null;
        }
        
        public static void SaveAssembly(this DatabaseService service, AssemblyTemplate assembly)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                
                if (assembly.AssemblyId == 0)
                {
                    // Insert new assembly
                    var query = @"INSERT INTO AssemblyTemplates 
                        (assembly_code, name, description, category, rough_minutes, finish_minutes, 
                         service_minutes, extra_minutes, is_default, is_active, sort_order, created_by)
                        VALUES (@code, @name, @desc, @category, @rough, @finish, @service, @extra, 
                                @isDefault, @isActive, @sortOrder, @createdBy);
                        SELECT LAST_INSERT_ID();";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@code", assembly.AssemblyCode);
                        command.Parameters.AddWithValue("@name", assembly.Name);
                        command.Parameters.AddWithValue("@desc", assembly.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@category", assembly.Category);
                        command.Parameters.AddWithValue("@rough", assembly.RoughMinutes);
                        command.Parameters.AddWithValue("@finish", assembly.FinishMinutes);
                        command.Parameters.AddWithValue("@service", assembly.ServiceMinutes);
                        command.Parameters.AddWithValue("@extra", assembly.ExtraMinutes);
                        command.Parameters.AddWithValue("@isDefault", assembly.IsDefault);
                        command.Parameters.AddWithValue("@isActive", assembly.IsActive);
                        command.Parameters.AddWithValue("@sortOrder", assembly.SortOrder);
                        command.Parameters.AddWithValue("@createdBy", assembly.CreatedBy);
                        
                        assembly.AssemblyId = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
                else
                {
                    // Update existing assembly
                    var query = @"UPDATE AssemblyTemplates SET 
                        assembly_code = @code,
                        name = @name,
                        description = @desc,
                        category = @category,
                        rough_minutes = @rough,
                        finish_minutes = @finish,
                        service_minutes = @service,
                        extra_minutes = @extra,
                        is_default = @isDefault,
                        is_active = @isActive,
                        sort_order = @sortOrder,
                        updated_date = NOW()
                        WHERE assembly_id = @assemblyId";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@assemblyId", assembly.AssemblyId);
                        command.Parameters.AddWithValue("@code", assembly.AssemblyCode);
                        command.Parameters.AddWithValue("@name", assembly.Name);
                        command.Parameters.AddWithValue("@desc", assembly.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@category", assembly.Category);
                        command.Parameters.AddWithValue("@rough", assembly.RoughMinutes);
                        command.Parameters.AddWithValue("@finish", assembly.FinishMinutes);
                        command.Parameters.AddWithValue("@service", assembly.ServiceMinutes);
                        command.Parameters.AddWithValue("@extra", assembly.ExtraMinutes);
                        command.Parameters.AddWithValue("@isDefault", assembly.IsDefault);
                        command.Parameters.AddWithValue("@isActive", assembly.IsActive);
                        command.Parameters.AddWithValue("@sortOrder", assembly.SortOrder);
                        
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        
        public static void UpdateAssembly(this DatabaseService service, AssemblyTemplate assembly)
        {
            service.SaveAssembly(assembly);
        }
        
        public static List<AssemblyTemplate> GetAssemblyVariants(this DatabaseService service, string assemblyCode)
        {
            var assemblies = new List<AssemblyTemplate>();
            
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT * FROM AssemblyTemplates 
                    WHERE assembly_code = @code AND is_active = TRUE 
                    ORDER BY is_default DESC, sort_order";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@code", assemblyCode);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            assemblies.Add(ReadAssemblyTemplate(reader));
                        }
                    }
                }
                
                // Load components for each assembly
                foreach (var assembly in assemblies)
                {
                    assembly.Components = service.GetAssemblyComponents(assembly.AssemblyId);
                }
            }
            
            return assemblies;
        }
        
        public static void CreateAssemblyVariantRelationship(this DatabaseService service, 
            int parentAssemblyId, int variantAssemblyId)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"INSERT INTO AssemblyVariants (parent_assembly_id, variant_assembly_id, sort_order)
                    SELECT @parentId, @variantId, COALESCE(MAX(sort_order), 0) + 1
                    FROM AssemblyVariants WHERE parent_assembly_id = @parentId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@parentId", parentAssemblyId);
                    command.Parameters.AddWithValue("@variantId", variantAssemblyId);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
        
        #endregion
        
        #region Assembly Component Methods
        
        public static List<AssemblyComponent> GetAssemblyComponents(this DatabaseService service, int assemblyId)
        {
            var components = new List<AssemblyComponent>();
            
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT ac.*, m.* FROM AssemblyComponents ac
                    JOIN Materials m ON ac.material_id = m.material_id
                    WHERE ac.assembly_id = @assemblyId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@assemblyId", assemblyId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            components.Add(ReadAssemblyComponent(reader));
                        }
                    }
                }
            }
            
            return components;
        }
        
        public static void SaveAssemblyComponent(this DatabaseService service, AssemblyComponent component)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"INSERT INTO AssemblyComponents (assembly_id, material_id, quantity, is_optional, notes)
                    VALUES (@assemblyId, @materialId, @quantity, @isOptional, @notes)";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@assemblyId", component.AssemblyId);
                    command.Parameters.AddWithValue("@materialId", component.MaterialId);
                    command.Parameters.AddWithValue("@quantity", component.Quantity);
                    command.Parameters.AddWithValue("@isOptional", component.IsOptional);
                    command.Parameters.AddWithValue("@notes", component.Notes ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public static AssemblyComponent GetAssemblyComponentById(this DatabaseService service, int componentId)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT ac.*, m.* FROM AssemblyComponents ac
                    JOIN Materials m ON ac.material_id = m.material_id
                    WHERE ac.component_id = @componentId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@componentId", componentId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadAssemblyComponent(reader);
                        }
                    }
                }
            }
            
            return null;
        }
        
        public static void UpdateAssemblyComponent(this DatabaseService service, AssemblyComponent component)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"UPDATE AssemblyComponents SET 
                    quantity = @quantity,
                    is_optional = @isOptional,
                    notes = @notes
                    WHERE component_id = @componentId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@componentId", component.ComponentId);
                    command.Parameters.AddWithValue("@quantity", component.Quantity);
                    command.Parameters.AddWithValue("@isOptional", component.IsOptional);
                    command.Parameters.AddWithValue("@notes", component.Notes ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public static void DeleteAssemblyComponent(this DatabaseService service, int componentId)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"DELETE FROM AssemblyComponents WHERE component_id = @componentId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@componentId", componentId);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        #endregion
        
        #region Difficulty Preset Methods
        
        public static List<DifficultyPreset> GetAllDifficultyPresets(this DatabaseService service)
        {
            var presets = new List<DifficultyPreset>();
            
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT * FROM DifficultyPresets WHERE is_active = TRUE ORDER BY category, sort_order";
                
                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        presets.Add(ReadDifficultyPreset(reader));
                    }
                }
            }
            
            return presets;
        }
        
        #endregion
        
        #region Labor Adjustment Methods
        
        public static List<LaborAdjustment> GetLaborAdjustmentsByJob(this DatabaseService service, int jobId)
        {
            var adjustments = new List<LaborAdjustment>();
            
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT la.*, dp.* FROM LaborAdjustments la
                    LEFT JOIN DifficultyPresets dp ON la.preset_id = dp.preset_id
                    WHERE la.job_id = @jobId
                    ORDER BY la.created_date DESC";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@jobId", jobId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            adjustments.Add(ReadLaborAdjustment(reader));
                        }
                    }
                }
            }
            
            return adjustments;
        }
        
        #endregion
        
        #region Assembly Usage Methods
        
        public static int GetAssemblyUsageCount(this DatabaseService service, int assemblyId)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT COUNT(*) FROM EstimateLineItems 
                    WHERE assembly_id = @assemblyId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@assemblyId", assemblyId);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }
        
        public static DateTime? GetAssemblyLastUsedDate(this DatabaseService service, int assemblyId)
        {
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT MAX(e.created_date) 
                    FROM EstimateLineItems eli
                    JOIN Estimates e ON eli.estimate_id = e.estimate_id
                    WHERE eli.assembly_id = @assemblyId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@assemblyId", assemblyId);
                    var result = command.ExecuteScalar();
                    
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToDateTime(result);
                    }
                }
            }
            
            return null;
        }
        
        public static List<Estimate> GetEstimatesInDateRange(this DatabaseService service, 
            DateTime? startDate, DateTime? endDate)
        {
            var estimates = new List<Estimate>();
            
            using (var connection = service.GetConnection())
            {
                connection.Open();
                var query = @"SELECT * FROM Estimates WHERE 1=1";
                
                if (startDate.HasValue)
                    query += " AND created_date >= @startDate";
                if (endDate.HasValue)
                    query += " AND created_date <= @endDate";
                    
                query += " ORDER BY created_date DESC";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    if (startDate.HasValue)
                        command.Parameters.AddWithValue("@startDate", startDate.Value);
                    if (endDate.HasValue)
                        command.Parameters.AddWithValue("@endDate", endDate.Value);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Fix: Call ReadEstimate correctly - it's not an extension method on DatabaseService
                            estimates.Add(ReadEstimate(reader));
                        }
                    }
                }
                
                // Load line items for each estimate
                foreach (var estimate in estimates)
                {
                    service.LoadEstimateDetails(estimate);
                }
            }
            
            return estimates;
        }
        
        // Add the ReadEstimate method that was missing
        private static Estimate ReadEstimate(MySqlDataReader reader)
        {
            return new Estimate
            {
                EstimateId = reader.GetInt32("estimate_id"),
                EstimateNumber = reader.GetString("estimate_number"),
                CustomerId = reader.GetInt32("customer_id"),
                JobName = reader.GetString("job_name"),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                Status = (EstimateStatus)Enum.Parse(typeof(EstimateStatus), reader.GetString("status")),
                LaborRate = reader.GetDecimal("labor_rate"),
                MaterialMarkup = reader.GetDecimal("material_markup"),
                TaxRate = reader.GetDecimal("tax_rate"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                CreateDate = reader.GetDateTime("created_date"),
                CreatedBy = reader.GetString("created_by"),
                ConvertedToJobId = reader.IsDBNull(reader.GetOrdinal("converted_to_job_id")) ? null : (int?)reader.GetInt32("converted_to_job_id")
            };
        }
        
        #endregion
        
        #region Helper Methods - Reading from DataReader
        
        private static Material ReadMaterial(MySqlDataReader reader)
        {
            return new Material
            {
                MaterialId = reader.GetInt32("material_id"),
                MaterialCode = reader.GetString("material_code"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Category = reader.GetString("category"),
                UnitOfMeasure = reader.GetString("unit_of_measure"),
                CurrentPrice = reader.GetDecimal("current_price"),
                TaxRate = reader.GetDecimal("tax_rate"),
                MinStockLevel = reader.GetInt32("min_stock_level"),
                MaxStockLevel = reader.GetInt32("max_stock_level"),
                PreferredVendorId = reader.IsDBNull(reader.GetOrdinal("preferred_vendor_id")) ? null : (int?)reader.GetInt32("preferred_vendor_id"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedDate = reader.GetDateTime("created_date"),
                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("updated_date")) ? null : (DateTime?)reader.GetDateTime("updated_date")
            };
        }
        
        private static MaterialPriceHistory ReadMaterialPriceHistory(MySqlDataReader reader)
        {
            return new MaterialPriceHistory
            {
                PriceHistoryId = reader.GetInt32("price_history_id"),
                MaterialId = reader.GetInt32("material_id"),
                Price = reader.GetDecimal("price"),
                EffectiveDate = reader.GetDateTime("effective_date"),
                VendorId = reader.IsDBNull(reader.GetOrdinal("vendor_id")) ? null : (int?)reader.GetInt32("vendor_id"),
                PurchaseOrderNumber = reader.IsDBNull(reader.GetOrdinal("purchase_order_number")) ? null : reader.GetString("purchase_order_number"),
                QuantityPurchased = reader.IsDBNull(reader.GetOrdinal("quantity_purchased")) ? null : (decimal?)reader.GetDecimal("quantity_purchased"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                CreatedBy = reader.GetString("created_by"),
                CreatedDate = reader.GetDateTime("created_date")
            };
        }
        
        private static AssemblyTemplate ReadAssemblyTemplate(MySqlDataReader reader)
        {
            return new AssemblyTemplate
            {
                AssemblyId = reader.GetInt32("assembly_id"),
                AssemblyCode = reader.GetString("assembly_code"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Category = reader.GetString("category"),
                RoughMinutes = reader.GetInt32("rough_minutes"),
                FinishMinutes = reader.GetInt32("finish_minutes"),
                ServiceMinutes = reader.GetInt32("service_minutes"),
                ExtraMinutes = reader.GetInt32("extra_minutes"),
                IsDefault = reader.GetBoolean("is_default"),
                IsActive = reader.GetBoolean("is_active"),
                SortOrder = reader.GetInt32("sort_order"),
                CreatedDate = reader.GetDateTime("created_date"),
                CreatedBy = reader.GetString("created_by"),
                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("updated_date")) ? null : (DateTime?)reader.GetDateTime("updated_date")
            };
        }
        
        private static AssemblyComponent ReadAssemblyComponent(MySqlDataReader reader)
        {
            var component = new AssemblyComponent
            {
                ComponentId = reader.GetInt32("component_id"),
                AssemblyId = reader.GetInt32("assembly_id"),
                MaterialId = reader.GetInt32("material_id"),
                Quantity = reader.GetDecimal("quantity"),
                IsOptional = reader.GetBoolean("is_optional"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
            
            // Read material data if present
            if (reader.HasColumn("material_code"))
            {
                component.Material = ReadMaterial(reader);
            }
            
            return component;
        }
        
        private static DifficultyPreset ReadDifficultyPreset(MySqlDataReader reader)
        {
            return new DifficultyPreset
            {
                PresetId = reader.GetInt32("preset_id"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Category = reader.GetString("category"),
                RoughMultiplier = reader.GetDecimal("rough_multiplier"),
                FinishMultiplier = reader.GetDecimal("finish_multiplier"),
                ServiceMultiplier = reader.GetDecimal("service_multiplier"),
                ExtraMultiplier = reader.GetDecimal("extra_multiplier"),
                IsActive = reader.GetBoolean("is_active"),
                SortOrder = reader.GetInt32("sort_order")
            };
        }
        
        private static LaborAdjustment ReadLaborAdjustment(MySqlDataReader reader)
        {
            var adjustment = new LaborAdjustment
            {
                AdjustmentId = reader.GetInt32("adjustment_id"),
                EstimateId = reader.IsDBNull(reader.GetOrdinal("estimate_id")) ? null : (int?)reader.GetInt32("estimate_id"),
                JobId = reader.IsDBNull(reader.GetOrdinal("job_id")) ? null : (int?)reader.GetInt32("job_id"),
                AdjustmentType = (AdjustmentType)Enum.Parse(typeof(AdjustmentType), reader.GetString("adjustment_type")),
                PresetId = reader.IsDBNull(reader.GetOrdinal("preset_id")) ? null : (int?)reader.GetInt32("preset_id"),
                RoughMultiplier = reader.GetDecimal("rough_multiplier"),
                FinishMultiplier = reader.GetDecimal("finish_multiplier"),
                ServiceMultiplier = reader.GetDecimal("service_multiplier"),
                ExtraMultiplier = reader.GetDecimal("extra_multiplier"),
                Reason = reader.IsDBNull(reader.GetOrdinal("reason")) ? null : reader.GetString("reason"),
                CreatedBy = reader.GetString("created_by"),
                CreatedDate = reader.GetDateTime("created_date")
            };
            
            // Read preset data if present
            if (adjustment.PresetId.HasValue && reader.HasColumn("preset_id") && !reader.IsDBNull(reader.GetOrdinal("preset_id")))
            {
                adjustment.Preset = ReadDifficultyPreset(reader);
            }
            
            return adjustment;
        }
        
        // Extension method to check if a column exists
        private static bool HasColumn(this IDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
        
        #endregion
    }
}