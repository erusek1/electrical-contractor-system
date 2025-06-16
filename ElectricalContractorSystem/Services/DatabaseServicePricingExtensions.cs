using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public static class DatabaseServicePricingExtensions
    {
        #region Material Management

        public static List<Material> GetAllMaterials(this DatabaseService db)
        {
            var materials = new List<Material>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                
                // Check if Materials table exists
                var checkCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Materials'", connection);
                    
                var tableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                
                if (!tableExists)
                {
                    // Return empty list if table doesn't exist
                    return materials;
                }
                
                var cmd = new MySqlCommand(@"
                    SELECT m.*, v.name AS VendorName 
                    FROM Materials m
                    LEFT JOIN Vendors v ON m.preferred_vendor_id = v.vendor_id
                    ORDER BY m.category, m.name", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        materials.Add(MapMaterial(reader));
                    }
                }
            }
            return materials;
        }

        public static Material GetMaterialById(this DatabaseService db, int materialId)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    SELECT m.*, v.name AS VendorName 
                    FROM Materials m
                    LEFT JOIN Vendors v ON m.preferred_vendor_id = v.vendor_id
                    WHERE m.material_id = @materialId", connection);
                
                cmd.Parameters.AddWithValue("@materialId", materialId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapMaterial(reader);
                    }
                }
            }
            return null;
        }

        public static void SaveMaterial(this DatabaseService db, Material material)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                MySqlCommand cmd;
                
                if (material.MaterialId == 0)
                {
                    cmd = new MySqlCommand(@"
                        INSERT INTO Materials (material_code, name, description, category, 
                            unit_of_measure, current_price, tax_rate, min_stock_level, max_stock_level,
                            preferred_vendor_id, is_active, created_date)
                        VALUES (@code, @name, @desc, @category, @unit, @price, @tax, 
                            @minStock, @maxStock, @vendorId, @active, @created);
                        SELECT LAST_INSERT_ID();", connection);
                    
                    cmd.Parameters.AddWithValue("@created", material.CreatedDate);
                }
                else
                {
                    cmd = new MySqlCommand(@"
                        UPDATE Materials SET 
                            material_code = @code, name = @name, description = @desc,
                            category = @category, unit_of_measure = @unit, current_price = @price,
                            tax_rate = @tax, min_stock_level = @minStock, max_stock_level = @maxStock,
                            preferred_vendor_id = @vendorId, is_active = @active, updated_date = @updated
                        WHERE material_id = @id", connection);
                    
                    cmd.Parameters.AddWithValue("@id", material.MaterialId);
                    cmd.Parameters.AddWithValue("@updated", DateTime.Now);
                }

                cmd.Parameters.AddWithValue("@code", material.MaterialCode);
                cmd.Parameters.AddWithValue("@name", material.Name);
                cmd.Parameters.AddWithValue("@desc", material.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@category", material.Category);
                cmd.Parameters.AddWithValue("@unit", material.UnitOfMeasure);
                cmd.Parameters.AddWithValue("@price", material.CurrentPrice);
                cmd.Parameters.AddWithValue("@tax", material.TaxRate);
                cmd.Parameters.AddWithValue("@minStock", material.MinStockLevel);
                cmd.Parameters.AddWithValue("@maxStock", material.MaxStockLevel);
                cmd.Parameters.AddWithValue("@vendorId", material.PreferredVendorId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@active", material.IsActive);

                if (material.MaterialId == 0)
                {
                    material.MaterialId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                else
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Add the UpdateMaterial method
        public static void UpdateMaterial(this DatabaseService db, Material material)
        {
            db.SaveMaterial(material);
        }

        #endregion

        #region Material Price History

        public static void SaveMaterialPriceHistory(this DatabaseService db, MaterialPriceHistory history)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    INSERT INTO MaterialPriceHistory (material_id, price, effective_date, 
                        vendor_id, purchase_order_number, quantity_purchased, notes, created_by)
                    VALUES (@materialId, @newPrice, @changeDate, @vendorId, @invoice, 
                        @quantity, @notes, @changedBy)", connection);

                cmd.Parameters.AddWithValue("@materialId", history.MaterialId);
                cmd.Parameters.AddWithValue("@oldPrice", history.OldPrice);
                cmd.Parameters.AddWithValue("@newPrice", history.NewPrice);
                cmd.Parameters.AddWithValue("@percentChange", history.PercentageChange);
                cmd.Parameters.AddWithValue("@changedBy", history.ChangedBy);
                cmd.Parameters.AddWithValue("@changeDate", history.ChangeDate);
                cmd.Parameters.AddWithValue("@vendorId", history.VendorId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@invoice", history.InvoiceNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@quantity", history.QuantityPurchased ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", history.Notes ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        public static List<MaterialPriceHistory> GetMaterialPriceHistory(this DatabaseService db, 
            int materialId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var history = new List<MaterialPriceHistory>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var query = @"
                    SELECT mph.*, v.name AS VendorName, m.name AS MaterialName
                    FROM MaterialPriceHistory mph
                    LEFT JOIN Vendors v ON mph.vendor_id = v.vendor_id
                    LEFT JOIN Materials m ON mph.material_id = m.material_id
                    WHERE mph.material_id = @materialId";

                if (startDate.HasValue)
                    query += " AND mph.effective_date >= @startDate";
                if (endDate.HasValue)
                    query += " AND mph.effective_date <= @endDate";

                query += " ORDER BY mph.effective_date DESC";

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@materialId", materialId);
                if (startDate.HasValue)
                    cmd.Parameters.AddWithValue("@startDate", startDate.Value);
                if (endDate.HasValue)
                    cmd.Parameters.AddWithValue("@endDate", endDate.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        history.Add(MapMaterialPriceHistory(reader));
                    }
                }
            }
            return history;
        }

        #endregion

        #region Assembly Management

        public static List<AssemblyTemplate> GetAllAssemblies(this DatabaseService db)
        {
            var assemblies = new List<AssemblyTemplate>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                
                // Check if AssemblyTemplates table exists
                var checkCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'AssemblyTemplates'", connection);
                    
                var tableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                
                if (!tableExists)
                {
                    // Return empty list if table doesn't exist
                    return assemblies;
                }
                
                var cmd = new MySqlCommand(@"
                    SELECT * FROM AssemblyTemplates 
                    WHERE is_active = 1
                    ORDER BY category, assembly_code, name", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        assemblies.Add(MapAssemblyTemplate(reader));
                    }
                }
            }

            // Load components for each assembly
            foreach (var assembly in assemblies)
            {
                assembly.Components = db.GetAssemblyComponents(assembly.AssemblyId);
            }

            return assemblies;
        }

        public static AssemblyTemplate GetAssemblyById(this DatabaseService db, int assemblyId)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    SELECT * FROM AssemblyTemplates 
                    WHERE assembly_id = @assemblyId", connection);
                
                cmd.Parameters.AddWithValue("@assemblyId", assemblyId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var assembly = MapAssemblyTemplate(reader);
                        assembly.Components = db.GetAssemblyComponents(assemblyId);
                        return assembly;
                    }
                }
            }
            return null;
        }

        public static List<AssemblyTemplate> GetAssemblyVariants(this DatabaseService db, string assemblyCode)
        {
            var variants = new List<AssemblyTemplate>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                // First get the main assembly
                var cmd = new MySqlCommand(@"
                    SELECT * FROM AssemblyTemplates 
                    WHERE assembly_code = @code AND is_active = 1
                    ORDER BY is_default DESC, name", connection);
                
                cmd.Parameters.AddWithValue("@code", assemblyCode);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        variants.Add(MapAssemblyTemplate(reader));
                    }
                }

                // Then get variants
                cmd = new MySqlCommand(@"
                    SELECT at.* FROM AssemblyTemplates at
                    INNER JOIN AssemblyVariants av ON at.assembly_id = av.variant_assembly_id
                    INNER JOIN AssemblyTemplates parent ON av.parent_assembly_id = parent.assembly_id
                    WHERE parent.assembly_code = @code AND at.is_active = 1
                    ORDER BY av.sort_order, at.name", connection);
                
                cmd.Parameters.AddWithValue("@code", assemblyCode);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        variants.Add(MapAssemblyTemplate(reader));
                    }
                }
            }

            // Load components
            foreach (var variant in variants)
            {
                variant.Components = db.GetAssemblyComponents(variant.AssemblyId);
            }

            return variants;
        }

        public static void SaveAssembly(this DatabaseService db, AssemblyTemplate assembly)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                MySqlCommand cmd;
                
                if (assembly.AssemblyId == 0)
                {
                    cmd = new MySqlCommand(@"
                        INSERT INTO AssemblyTemplates (assembly_code, name, description, category,
                            rough_minutes, finish_minutes, service_minutes, extra_minutes,
                            is_default, is_active, created_by, created_date)
                        VALUES (@code, @name, @desc, @category, @rough, @finish, @service, @extra,
                            @isDefault, @active, @createdBy, @created);
                        SELECT LAST_INSERT_ID();", connection);
                    
                    cmd.Parameters.AddWithValue("@created", assembly.CreatedDate);
                }
                else
                {
                    cmd = new MySqlCommand(@"
                        UPDATE AssemblyTemplates SET 
                            assembly_code = @code, name = @name, description = @desc,
                            category = @category, rough_minutes = @rough, finish_minutes = @finish,
                            service_minutes = @service, extra_minutes = @extra,
                            is_default = @isDefault, is_active = @active,
                            updated_by = @updatedBy, updated_date = @updated
                        WHERE assembly_id = @id", connection);
                    
                    cmd.Parameters.AddWithValue("@id", assembly.AssemblyId);
                    cmd.Parameters.AddWithValue("@updatedBy", assembly.UpdatedBy);
                    cmd.Parameters.AddWithValue("@updated", DateTime.Now);
                }

                cmd.Parameters.AddWithValue("@code", assembly.AssemblyCode);
                cmd.Parameters.AddWithValue("@name", assembly.Name);
                cmd.Parameters.AddWithValue("@desc", assembly.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@category", assembly.Category);
                cmd.Parameters.AddWithValue("@rough", assembly.RoughMinutes);
                cmd.Parameters.AddWithValue("@finish", assembly.FinishMinutes);
                cmd.Parameters.AddWithValue("@service", assembly.ServiceMinutes);
                cmd.Parameters.AddWithValue("@extra", assembly.ExtraMinutes);
                cmd.Parameters.AddWithValue("@isDefault", assembly.IsDefault);
                cmd.Parameters.AddWithValue("@active", assembly.IsActive);
                cmd.Parameters.AddWithValue("@createdBy", assembly.CreatedBy);

                if (assembly.AssemblyId == 0)
                {
                    assembly.AssemblyId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                else
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void CreateAssemblyVariantRelationship(this DatabaseService db, 
            int parentAssemblyId, int variantAssemblyId, int sortOrder = 0)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    INSERT INTO AssemblyVariants (parent_assembly_id, variant_assembly_id, sort_order)
                    VALUES (@parentId, @variantId, @sortOrder)", connection);

                cmd.Parameters.AddWithValue("@parentId", parentAssemblyId);
                cmd.Parameters.AddWithValue("@variantId", variantAssemblyId);
                cmd.Parameters.AddWithValue("@sortOrder", sortOrder);

                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Assembly Components

        public static List<AssemblyComponent> GetAssemblyComponents(this DatabaseService db, int assemblyId)
        {
            var components = new List<AssemblyComponent>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    SELECT ac.*, m.name AS ItemName, m.material_code AS ItemCode,
                           m.current_price AS UnitPrice
                    FROM AssemblyComponents ac
                    INNER JOIN Materials m ON ac.material_id = m.material_id
                    WHERE ac.assembly_id = @assemblyId
                    ORDER BY ac.component_id", connection);
                
                cmd.Parameters.AddWithValue("@assemblyId", assemblyId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        components.Add(MapAssemblyComponent(reader));
                    }
                }
            }
            return components;
        }

        public static AssemblyComponent GetAssemblyComponentById(this DatabaseService db, int componentId)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    SELECT ac.*, m.name AS ItemName, m.material_code AS ItemCode,
                           m.current_price AS UnitPrice
                    FROM AssemblyComponents ac
                    INNER JOIN Materials m ON ac.material_id = m.material_id
                    WHERE ac.component_id = @componentId", connection);
                
                cmd.Parameters.AddWithValue("@componentId", componentId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapAssemblyComponent(reader);
                    }
                }
            }
            return null;
        }

        public static void SaveAssemblyComponent(this DatabaseService db, AssemblyComponent component)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    INSERT INTO AssemblyComponents (assembly_id, material_id, quantity, notes)
                    VALUES (@assemblyId, @materialId, @quantity, @notes);
                    SELECT LAST_INSERT_ID();", connection);

                cmd.Parameters.AddWithValue("@assemblyId", component.AssemblyId);
                cmd.Parameters.AddWithValue("@materialId", component.MaterialId ?? component.PriceListItemId);  // Support both old and new
                cmd.Parameters.AddWithValue("@quantity", component.Quantity);
                cmd.Parameters.AddWithValue("@notes", component.Notes ?? (object)DBNull.Value);

                component.ComponentId = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void UpdateAssemblyComponent(this DatabaseService db, AssemblyComponent component)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    UPDATE AssemblyComponents 
                    SET quantity = @quantity, notes = @notes
                    WHERE component_id = @componentId", connection);

                cmd.Parameters.AddWithValue("@componentId", component.ComponentId);
                cmd.Parameters.AddWithValue("@quantity", component.Quantity);
                cmd.Parameters.AddWithValue("@notes", component.Notes ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteAssemblyComponent(this DatabaseService db, int componentId)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(
                    "DELETE FROM AssemblyComponents WHERE component_id = @componentId", connection);
                
                cmd.Parameters.AddWithValue("@componentId", componentId);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Difficulty Presets

        public static List<DifficultyPreset> GetAllDifficultyPresets(this DatabaseService db)
        {
            var presets = new List<DifficultyPreset>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                
                // Check if DifficultyPresets table exists
                var checkCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'DifficultyPresets'", connection);
                    
                var tableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                
                if (!tableExists)
                {
                    // Return empty list if table doesn't exist
                    return presets;
                }
                
                var cmd = new MySqlCommand(@"
                    SELECT * FROM DifficultyPresets 
                    WHERE is_active = 1
                    ORDER BY category, sort_order, name", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        presets.Add(MapDifficultyPreset(reader));
                    }
                }
            }
            return presets;
        }

        public static void SaveDifficultyPreset(this DatabaseService db, DifficultyPreset preset)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                MySqlCommand cmd;
                
                if (preset.PresetId == 0)
                {
                    cmd = new MySqlCommand(@"
                        INSERT INTO DifficultyPresets (name, category, description,
                            rough_multiplier, finish_multiplier, service_multiplier, extra_multiplier,
                            is_active, sort_order)
                        VALUES (@name, @category, @desc, @rough, @finish, @service, @extra,
                            @active, @sortOrder);
                        SELECT LAST_INSERT_ID();", connection);
                }
                else
                {
                    cmd = new MySqlCommand(@"
                        UPDATE DifficultyPresets SET 
                            name = @name, category = @category, description = @desc,
                            rough_multiplier = @rough, finish_multiplier = @finish,
                            service_multiplier = @service, extra_multiplier = @extra,
                            is_active = @active, sort_order = @sortOrder
                        WHERE preset_id = @id", connection);
                    
                    cmd.Parameters.AddWithValue("@id", preset.PresetId);
                }

                cmd.Parameters.AddWithValue("@name", preset.Name);
                cmd.Parameters.AddWithValue("@category", preset.Category);
                cmd.Parameters.AddWithValue("@desc", preset.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@rough", preset.RoughMultiplier);
                cmd.Parameters.AddWithValue("@finish", preset.FinishMultiplier);
                cmd.Parameters.AddWithValue("@service", preset.ServiceMultiplier);
                cmd.Parameters.AddWithValue("@extra", preset.ExtraMultiplier);
                cmd.Parameters.AddWithValue("@active", preset.IsActive);
                cmd.Parameters.AddWithValue("@sortOrder", preset.SortOrder);

                if (preset.PresetId == 0)
                {
                    preset.PresetId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                else
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Labor Adjustments

        public static List<LaborAdjustment> GetLaborAdjustmentsByJob(this DatabaseService db, int jobId)
        {
            var adjustments = new List<LaborAdjustment>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    SELECT la.*, dp.name AS PresetName
                    FROM LaborAdjustments la
                    LEFT JOIN DifficultyPresets dp ON la.preset_id = dp.preset_id
                    WHERE la.job_id = @jobId
                    ORDER BY la.created_date DESC", connection);
                
                cmd.Parameters.AddWithValue("@jobId", jobId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        adjustments.Add(MapLaborAdjustment(reader));
                    }
                }
            }
            return adjustments;
        }

        public static void SaveLaborAdjustment(this DatabaseService db, LaborAdjustment adjustment)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    INSERT INTO LaborAdjustments (job_id, estimate_id, adjustment_type, preset_id,
                        rough_multiplier, finish_multiplier, service_multiplier, extra_multiplier,
                        reason, created_by, created_date)
                    VALUES (@jobId, @estimateId, @reason, @presetId,
                        @rough, @finish, @service, @extra,
                        @notes, @createdBy, @created)", connection);

                cmd.Parameters.AddWithValue("@jobId", adjustment.JobId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@estimateId", adjustment.EstimateId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@assemblyId", adjustment.AssemblyId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@presetId", adjustment.PresetId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@rough", adjustment.RoughMultiplier);
                cmd.Parameters.AddWithValue("@finish", adjustment.FinishMultiplier);
                cmd.Parameters.AddWithValue("@service", adjustment.ServiceMultiplier);
                cmd.Parameters.AddWithValue("@extra", adjustment.ExtraMultiplier);
                cmd.Parameters.AddWithValue("@reason", adjustment.ReasonCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", adjustment.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@createdBy", adjustment.CreatedBy);
                cmd.Parameters.AddWithValue("@created", adjustment.CreatedDate);

                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Service Types

        public static List<ServiceType> GetAllServiceTypes(this DatabaseService db)
        {
            var serviceTypes = new List<ServiceType>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                
                // Check if ServiceTypes table exists
                var checkCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'ServiceTypes'", connection);
                    
                var tableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                
                if (!tableExists)
                {
                    // Return empty list if table doesn't exist
                    return serviceTypes;
                }
                
                var cmd = new MySqlCommand(@"
                    SELECT * FROM ServiceTypes 
                    WHERE is_active = 1
                    ORDER BY service_type_id, name", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        serviceTypes.Add(MapServiceType(reader));
                    }
                }
            }
            return serviceTypes;
        }

        #endregion

        #region Analysis Methods

        public static decimal GetAverageMaterialPrice(this DatabaseService db, int materialId, int days = 30)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    SELECT AVG(price) 
                    FROM MaterialPriceHistory 
                    WHERE material_id = @materialId 
                    AND effective_date >= DATE_SUB(NOW(), INTERVAL @days DAY)", connection);
                
                cmd.Parameters.AddWithValue("@materialId", materialId);
                cmd.Parameters.AddWithValue("@days", days);

                var result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
        }

        public static List<Estimate> GetEstimatesInDateRange(this DatabaseService db, 
            DateTime? startDate, DateTime? endDate)
        {
            var estimates = new List<Estimate>();
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var query = "SELECT * FROM Estimates WHERE 1=1";
                
                if (startDate.HasValue)
                    query += " AND CreatedDate >= @startDate";
                if (endDate.HasValue)
                    query += " AND CreatedDate <= @endDate";

                var cmd = new MySqlCommand(query, connection);
                
                if (startDate.HasValue)
                    cmd.Parameters.AddWithValue("@startDate", startDate.Value);
                if (endDate.HasValue)
                    cmd.Parameters.AddWithValue("@endDate", endDate.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        estimates.Add(MapEstimate(reader));
                    }
                }
            }

            // Load line items for each estimate  
            foreach (var estimate in estimates)
            {
                // Use the existing method from DatabaseService.Estimating.cs
                estimate.Rooms = db.GetEstimateRooms(estimate.EstimateId);
                estimate.StageSummaries = db.GetEstimateStageSummaries(estimate.EstimateId);
            }

            return estimates;
        }

        public static int GetAssemblyUsageCount(this DatabaseService db, int assemblyId)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM EstimateLineItems 
                    WHERE assembly_id = @assemblyId", connection);
                
                cmd.Parameters.AddWithValue("@assemblyId", assemblyId);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static DateTime? GetAssemblyLastUsedDate(this DatabaseService db, int assemblyId)
        {
            using (var connection = db.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
                    SELECT MAX(e.CreatedDate)
                    FROM EstimateLineItems eli
                    INNER JOIN Estimates e ON eli.estimate_id = e.EstimateId
                    WHERE eli.assembly_id = @assemblyId", connection);
                
                cmd.Parameters.AddWithValue("@assemblyId", assemblyId);

                var result = cmd.ExecuteScalar();
                return result != DBNull.Value ? (DateTime?)result : null;
            }
        }

        #endregion

        #region Mapping Methods

        private static Material MapMaterial(MySqlDataReader reader)
        {
            var material = new Material
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

            // Map vendor if joined
            if (!reader.IsDBNull(reader.GetOrdinal("VendorName")))
            {
                material.PreferredVendor = new Vendor
                {
                    VendorId = material.PreferredVendorId.Value,
                    Name = reader.GetString("VendorName")
                };
            }

            return material;
        }

        private static MaterialPriceHistory MapMaterialPriceHistory(MySqlDataReader reader)
        {
            return new MaterialPriceHistory
            {
                HistoryId = reader.GetInt32("price_history_id"),
                MaterialId = reader.GetInt32("material_id"),
                OldPrice = 0, // Not in new schema, handle separately
                NewPrice = reader.GetDecimal("price"),
                PercentageChange = 0, // Calculate separately
                ChangedBy = reader.GetString("created_by"),
                ChangeDate = reader.GetDateTime("effective_date"),
                VendorId = reader.IsDBNull(reader.GetOrdinal("vendor_id")) ? null : (int?)reader.GetInt32("vendor_id"),
                InvoiceNumber = reader.IsDBNull(reader.GetOrdinal("purchase_order_number")) ? null : reader.GetString("purchase_order_number"),
                QuantityPurchased = reader.IsDBNull(reader.GetOrdinal("quantity_purchased")) ? null : (decimal?)reader.GetDecimal("quantity_purchased"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        private static AssemblyTemplate MapAssemblyTemplate(MySqlDataReader reader)
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
                CreatedBy = reader.GetString("created_by"),
                CreatedDate = reader.GetDateTime("created_date"),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetString("updated_by"),
                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("updated_date")) ? null : (DateTime?)reader.GetDateTime("updated_date")
            };
        }

        private static AssemblyComponent MapAssemblyComponent(MySqlDataReader reader)
        {
            return new AssemblyComponent
            {
                ComponentId = reader.GetInt32("component_id"),
                AssemblyId = reader.GetInt32("assembly_id"),
                MaterialId = reader.GetInt32("material_id"),
                PriceListItemId = reader.GetInt32("material_id"), // Map to same for compatibility
                Quantity = reader.GetDecimal("quantity"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                ItemName = reader.GetString("ItemName"),
                ItemCode = reader.GetString("ItemCode"),
                UnitPrice = reader.GetDecimal("UnitPrice")
            };
        }

        private static DifficultyPreset MapDifficultyPreset(MySqlDataReader reader)
        {
            return new DifficultyPreset
            {
                PresetId = reader.GetInt32("preset_id"),
                Name = reader.GetString("name"),
                Category = reader.GetString("category"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                RoughMultiplier = reader.GetDecimal("rough_multiplier"),
                FinishMultiplier = reader.GetDecimal("finish_multiplier"),
                ServiceMultiplier = reader.GetDecimal("service_multiplier"),
                ExtraMultiplier = reader.GetDecimal("extra_multiplier"),
                IsActive = reader.GetBoolean("is_active"),
                SortOrder = reader.GetInt32("sort_order")
            };
        }

        private static LaborAdjustment MapLaborAdjustment(MySqlDataReader reader)
        {
            return new LaborAdjustment
            {
                AdjustmentId = reader.GetInt32("adjustment_id"),
                JobId = reader.IsDBNull(reader.GetOrdinal("job_id")) ? null : (int?)reader.GetInt32("job_id"),
                EstimateId = reader.IsDBNull(reader.GetOrdinal("estimate_id")) ? null : (int?)reader.GetInt32("estimate_id"),
                AssemblyId = null, // Not in new schema
                PresetId = reader.IsDBNull(reader.GetOrdinal("preset_id")) ? null : (int?)reader.GetInt32("preset_id"),
                RoughMultiplier = reader.GetDecimal("rough_multiplier"),
                FinishMultiplier = reader.GetDecimal("finish_multiplier"),
                ServiceMultiplier = reader.GetDecimal("service_multiplier"),
                ExtraMultiplier = reader.GetDecimal("extra_multiplier"),
                ReasonCode = reader.GetString("adjustment_type"),
                Notes = reader.IsDBNull(reader.GetOrdinal("reason")) ? null : reader.GetString("reason"),
                CreatedBy = reader.GetString("created_by"),
                CreatedDate = reader.GetDateTime("created_date")
            };
        }

        private static ServiceType MapServiceType(MySqlDataReader reader)
        {
            return new ServiceType
            {
                ServiceTypeId = reader.GetInt32("service_type_id"),
                Name = reader.GetString("name"),
                Code = "", // Not in schema, set default
                LaborMultiplier = reader.GetDecimal("labor_multiplier"),
                BaseHourlyRate = 85.00m, // Default base rate
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                IsActive = reader.GetBoolean("is_active"),
                SortOrder = 0 // Not in schema
            };
        }

        private static Estimate MapEstimate(MySqlDataReader reader)
        {
            return new Estimate
            {
                EstimateId = reader.GetInt32("EstimateId"),
                EstimateNumber = reader.GetString("EstimateNumber"),
                CustomerId = reader.GetInt32("CustomerId"),
                PropertyId = reader.IsDBNull(reader.GetOrdinal("PropertyId")) ? null : (int?)reader.GetInt32("PropertyId"),
                ProjectName = reader.GetString("ProjectName"),
                Status = (EstimateStatus)Enum.Parse(typeof(EstimateStatus), reader.GetString("Status")),
                CreateDate = reader.GetDateTime("CreatedDate"),
                ValidUntilDate = reader.GetDateTime("ValidUntilDate"),
                ServiceTypeId = reader.IsDBNull(reader.GetOrdinal("ServiceTypeId")) ? null : (int?)reader.GetInt32("ServiceTypeId"),
                TotalMaterialCost = reader.GetDecimal("TotalMaterialCost"),
                TotalLaborMinutes = reader.GetInt32("TotalLaborMinutes"),
                TotalLaborCost = reader.GetDecimal("TotalLaborCost"),
                TotalPrice = reader.GetDecimal("TotalPrice"),
                ApprovedDate = reader.IsDBNull(reader.GetOrdinal("ApprovedDate")) ? null : (DateTime?)reader.GetDateTime("ApprovedDate"),
                ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ? null : reader.GetString("ApprovedBy"),
                ConvertedToJobId = reader.IsDBNull(reader.GetOrdinal("ConvertedToJobId")) ? null : (int?)reader.GetInt32("ConvertedToJobId"),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes"),
                CreatedBy = reader.GetString("CreatedBy"),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetString("UpdatedBy"),
                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedDate")) ? null : (DateTime?)reader.GetDateTime("UpdatedDate")
            };
        }

        #endregion
    }
}
