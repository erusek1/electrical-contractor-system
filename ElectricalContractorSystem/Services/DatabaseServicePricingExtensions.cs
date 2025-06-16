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
                var cmd = new MySqlCommand(@"
                    SELECT m.*, v.name AS VendorName 
                    FROM Materials m
                    LEFT JOIN Vendors v ON m.PreferredVendorId = v.vendor_id
                    ORDER BY m.Category, m.Name", connection);

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
                    LEFT JOIN Vendors v ON m.PreferredVendorId = v.vendor_id
                    WHERE m.MaterialId = @materialId", connection);
                
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
                        INSERT INTO Materials (MaterialCode, Name, Description, Category, 
                            UnitOfMeasure, CurrentPrice, TaxRate, MinStockLevel, MaxStockLevel,
                            PreferredVendorId, IsActive, CreatedDate)
                        VALUES (@code, @name, @desc, @category, @unit, @price, @tax, 
                            @minStock, @maxStock, @vendorId, @active, @created);
                        SELECT LAST_INSERT_ID();", connection);
                    
                    cmd.Parameters.AddWithValue("@created", material.CreatedDate);
                }
                else
                {
                    cmd = new MySqlCommand(@"
                        UPDATE Materials SET 
                            MaterialCode = @code, Name = @name, Description = @desc,
                            Category = @category, UnitOfMeasure = @unit, CurrentPrice = @price,
                            TaxRate = @tax, MinStockLevel = @minStock, MaxStockLevel = @maxStock,
                            PreferredVendorId = @vendorId, IsActive = @active, UpdatedDate = @updated
                        WHERE MaterialId = @id", connection);
                    
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
                    INSERT INTO MaterialPriceHistory (MaterialId, OldPrice, NewPrice, 
                        PercentageChange, ChangedBy, ChangeDate, VendorId, InvoiceNumber,
                        QuantityPurchased, Notes)
                    VALUES (@materialId, @oldPrice, @newPrice, @percentChange, @changedBy,
                        @changeDate, @vendorId, @invoice, @quantity, @notes)", connection);

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
                    SELECT mph.*, v.name AS VendorName, m.Name AS MaterialName
                    FROM MaterialPriceHistory mph
                    LEFT JOIN Vendors v ON mph.VendorId = v.vendor_id
                    LEFT JOIN Materials m ON mph.MaterialId = m.MaterialId
                    WHERE mph.MaterialId = @materialId";

                if (startDate.HasValue)
                    query += " AND mph.ChangeDate >= @startDate";
                if (endDate.HasValue)
                    query += " AND mph.ChangeDate <= @endDate";

                query += " ORDER BY mph.ChangeDate DESC";

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
                var cmd = new MySqlCommand(@"
                    SELECT * FROM AssemblyTemplates 
                    WHERE IsActive = 1
                    ORDER BY Category, AssemblyCode, Name", connection);

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
                    WHERE AssemblyId = @assemblyId", connection);
                
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
                    WHERE AssemblyCode = @code AND IsActive = 1
                    ORDER BY IsDefault DESC, Name", connection);
                
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
                    INNER JOIN AssemblyVariants av ON at.AssemblyId = av.VariantAssemblyId
                    INNER JOIN AssemblyTemplates parent ON av.ParentAssemblyId = parent.AssemblyId
                    WHERE parent.AssemblyCode = @code AND at.IsActive = 1
                    ORDER BY av.SortOrder, at.Name", connection);
                
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
                        INSERT INTO AssemblyTemplates (AssemblyCode, Name, Description, Category,
                            RoughMinutes, FinishMinutes, ServiceMinutes, ExtraMinutes,
                            IsDefault, IsActive, CreatedBy, CreatedDate)
                        VALUES (@code, @name, @desc, @category, @rough, @finish, @service, @extra,
                            @isDefault, @active, @createdBy, @created);
                        SELECT LAST_INSERT_ID();", connection);
                    
                    cmd.Parameters.AddWithValue("@created", assembly.CreatedDate);
                }
                else
                {
                    cmd = new MySqlCommand(@"
                        UPDATE AssemblyTemplates SET 
                            AssemblyCode = @code, Name = @name, Description = @desc,
                            Category = @category, RoughMinutes = @rough, FinishMinutes = @finish,
                            ServiceMinutes = @service, ExtraMinutes = @extra,
                            IsDefault = @isDefault, IsActive = @active,
                            UpdatedBy = @updatedBy, UpdatedDate = @updated
                        WHERE AssemblyId = @id", connection);
                    
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
                    INSERT INTO AssemblyVariants (ParentAssemblyId, VariantAssemblyId, SortOrder)
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
                    SELECT ac.*, p.name AS ItemName, p.item_code AS ItemCode,
                           p.base_cost AS UnitPrice
                    FROM AssemblyComponents ac
                    INNER JOIN PriceList p ON ac.PriceListItemId = p.item_id
                    WHERE ac.AssemblyId = @assemblyId
                    ORDER BY ac.ComponentId", connection);
                
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
                    SELECT ac.*, p.name AS ItemName, p.item_code AS ItemCode,
                           p.base_cost AS UnitPrice
                    FROM AssemblyComponents ac
                    INNER JOIN PriceList p ON ac.PriceListItemId = p.item_id
                    WHERE ac.ComponentId = @componentId", connection);
                
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
                    INSERT INTO AssemblyComponents (AssemblyId, PriceListItemId, Quantity, Notes)
                    VALUES (@assemblyId, @itemId, @quantity, @notes);
                    SELECT LAST_INSERT_ID();", connection);

                cmd.Parameters.AddWithValue("@assemblyId", component.AssemblyId);
                cmd.Parameters.AddWithValue("@itemId", component.PriceListItemId);
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
                    SET Quantity = @quantity, Notes = @notes
                    WHERE ComponentId = @componentId", connection);

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
                    "DELETE FROM AssemblyComponents WHERE ComponentId = @componentId", connection);
                
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
                var cmd = new MySqlCommand(@"
                    SELECT * FROM DifficultyPresets 
                    WHERE IsActive = 1
                    ORDER BY Category, SortOrder, Name", connection);

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
                        INSERT INTO DifficultyPresets (Name, Category, Description,
                            RoughMultiplier, FinishMultiplier, ServiceMultiplier, ExtraMultiplier,
                            IsActive, SortOrder)
                        VALUES (@name, @category, @desc, @rough, @finish, @service, @extra,
                            @active, @sortOrder);
                        SELECT LAST_INSERT_ID();", connection);
                }
                else
                {
                    cmd = new MySqlCommand(@"
                        UPDATE DifficultyPresets SET 
                            Name = @name, Category = @category, Description = @desc,
                            RoughMultiplier = @rough, FinishMultiplier = @finish,
                            ServiceMultiplier = @service, ExtraMultiplier = @extra,
                            IsActive = @active, SortOrder = @sortOrder
                        WHERE PresetId = @id", connection);
                    
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
                    SELECT la.*, dp.Name AS PresetName
                    FROM LaborAdjustments la
                    LEFT JOIN DifficultyPresets dp ON la.PresetId = dp.PresetId
                    WHERE la.JobId = @jobId
                    ORDER BY la.CreatedDate DESC", connection);
                
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
                    INSERT INTO LaborAdjustments (JobId, EstimateId, AssemblyId, PresetId,
                        RoughMultiplier, FinishMultiplier, ServiceMultiplier, ExtraMultiplier,
                        ReasonCode, Notes, CreatedBy, CreatedDate)
                    VALUES (@jobId, @estimateId, @assemblyId, @presetId,
                        @rough, @finish, @service, @extra,
                        @reason, @notes, @createdBy, @created)", connection);

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
                var cmd = new MySqlCommand(@"
                    SELECT * FROM ServiceTypes 
                    WHERE IsActive = 1
                    ORDER BY SortOrder, Name", connection);

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
                    SELECT AVG(NewPrice) 
                    FROM MaterialPriceHistory 
                    WHERE MaterialId = @materialId 
                    AND ChangeDate >= DATE_SUB(NOW(), INTERVAL @days DAY)", connection);
                
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
                    WHERE AssemblyId = @assemblyId", connection);
                
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
                    INNER JOIN Estimates e ON eli.EstimateId = e.EstimateId
                    WHERE eli.AssemblyId = @assemblyId", connection);
                
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
                MaterialId = reader.GetInt32("MaterialId"),
                MaterialCode = reader.GetString("MaterialCode"),
                Name = reader.GetString("Name"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                Category = reader.GetString("Category"),
                UnitOfMeasure = reader.GetString("UnitOfMeasure"),
                CurrentPrice = reader.GetDecimal("CurrentPrice"),
                TaxRate = reader.GetDecimal("TaxRate"),
                MinStockLevel = reader.GetInt32("MinStockLevel"),
                MaxStockLevel = reader.GetInt32("MaxStockLevel"),
                PreferredVendorId = reader.IsDBNull(reader.GetOrdinal("PreferredVendorId")) ? null : (int?)reader.GetInt32("PreferredVendorId"),
                IsActive = reader.GetBoolean("IsActive"),
                CreatedDate = reader.GetDateTime("CreatedDate"),
                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedDate")) ? null : (DateTime?)reader.GetDateTime("UpdatedDate")
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
                HistoryId = reader.GetInt32("HistoryId"),
                MaterialId = reader.GetInt32("MaterialId"),
                OldPrice = reader.GetDecimal("OldPrice"),
                NewPrice = reader.GetDecimal("NewPrice"),
                PercentageChange = reader.GetDecimal("PercentageChange"),
                ChangedBy = reader.GetString("ChangedBy"),
                ChangeDate = reader.GetDateTime("ChangeDate"),
                VendorId = reader.IsDBNull(reader.GetOrdinal("VendorId")) ? null : (int?)reader.GetInt32("VendorId"),
                InvoiceNumber = reader.IsDBNull(reader.GetOrdinal("InvoiceNumber")) ? null : reader.GetString("InvoiceNumber"),
                QuantityPurchased = reader.IsDBNull(reader.GetOrdinal("QuantityPurchased")) ? null : (decimal?)reader.GetDecimal("QuantityPurchased"),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes")
            };
        }

        private static AssemblyTemplate MapAssemblyTemplate(MySqlDataReader reader)
        {
            return new AssemblyTemplate
            {
                AssemblyId = reader.GetInt32("AssemblyId"),
                AssemblyCode = reader.GetString("AssemblyCode"),
                Name = reader.GetString("Name"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                Category = reader.GetString("Category"),
                RoughMinutes = reader.GetInt32("RoughMinutes"),
                FinishMinutes = reader.GetInt32("FinishMinutes"),
                ServiceMinutes = reader.GetInt32("ServiceMinutes"),
                ExtraMinutes = reader.GetInt32("ExtraMinutes"),
                IsDefault = reader.GetBoolean("IsDefault"),
                IsActive = reader.GetBoolean("IsActive"),
                CreatedBy = reader.GetString("CreatedBy"),
                CreatedDate = reader.GetDateTime("CreatedDate"),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetString("UpdatedBy"),
                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedDate")) ? null : (DateTime?)reader.GetDateTime("UpdatedDate")
            };
        }

        private static AssemblyComponent MapAssemblyComponent(MySqlDataReader reader)
        {
            return new AssemblyComponent
            {
                ComponentId = reader.GetInt32("ComponentId"),
                AssemblyId = reader.GetInt32("AssemblyId"),
                PriceListItemId = reader.GetInt32("PriceListItemId"),
                Quantity = reader.GetDecimal("Quantity"),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes"),
                ItemName = reader.GetString("ItemName"),
                ItemCode = reader.GetString("ItemCode"),
                UnitPrice = reader.GetDecimal("UnitPrice")
            };
        }

        private static DifficultyPreset MapDifficultyPreset(MySqlDataReader reader)
        {
            return new DifficultyPreset
            {
                PresetId = reader.GetInt32("PresetId"),
                Name = reader.GetString("Name"),
                Category = reader.GetString("Category"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                RoughMultiplier = reader.GetDecimal("RoughMultiplier"),
                FinishMultiplier = reader.GetDecimal("FinishMultiplier"),
                ServiceMultiplier = reader.GetDecimal("ServiceMultiplier"),
                ExtraMultiplier = reader.GetDecimal("ExtraMultiplier"),
                IsActive = reader.GetBoolean("IsActive"),
                SortOrder = reader.GetInt32("SortOrder")
            };
        }

        private static LaborAdjustment MapLaborAdjustment(MySqlDataReader reader)
        {
            return new LaborAdjustment
            {
                AdjustmentId = reader.GetInt32("AdjustmentId"),
                JobId = reader.IsDBNull(reader.GetOrdinal("JobId")) ? null : (int?)reader.GetInt32("JobId"),
                EstimateId = reader.IsDBNull(reader.GetOrdinal("EstimateId")) ? null : (int?)reader.GetInt32("EstimateId"),
                AssemblyId = reader.IsDBNull(reader.GetOrdinal("AssemblyId")) ? null : (int?)reader.GetInt32("AssemblyId"),
                PresetId = reader.IsDBNull(reader.GetOrdinal("PresetId")) ? null : (int?)reader.GetInt32("PresetId"),
                RoughMultiplier = reader.GetDecimal("RoughMultiplier"),
                FinishMultiplier = reader.GetDecimal("FinishMultiplier"),
                ServiceMultiplier = reader.GetDecimal("ServiceMultiplier"),
                ExtraMultiplier = reader.GetDecimal("ExtraMultiplier"),
                ReasonCode = reader.IsDBNull(reader.GetOrdinal("ReasonCode")) ? null : reader.GetString("ReasonCode"),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes"),
                CreatedBy = reader.GetString("CreatedBy"),
                CreatedDate = reader.GetDateTime("CreatedDate")
            };
        }

        private static ServiceType MapServiceType(MySqlDataReader reader)
        {
            return new ServiceType
            {
                ServiceTypeId = reader.GetInt32("ServiceTypeId"),
                Name = reader.GetString("Name"),
                Code = reader.GetString("Code"),
                LaborMultiplier = reader.GetDecimal("LaborMultiplier"),
                BaseHourlyRate = reader.GetDecimal("BaseHourlyRate"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                IsActive = reader.GetBoolean("IsActive"),
                SortOrder = reader.GetInt32("SortOrder")
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
