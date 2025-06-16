-- Migration script to add missing features to existing pricing tables
-- This adds only what's missing from the current GitHub implementation

USE electrical_contractor_db;

-- =====================================================
-- 1. Add missing difficulty presets
-- =====================================================

-- First check if these presets already exist before inserting
INSERT INTO `DifficultyPresets` (`name`, `description`, `category`, `rough_multiplier`, `finish_multiplier`, `service_multiplier`, `extra_multiplier`, `sort_order`) 
SELECT * FROM (
    SELECT 'Occupied Home - Minimal' as name, 'Some furniture, minor obstacles' as description, 'Conditions' as category, 1.050 as rough_multiplier, 1.050 as finish_multiplier, 1.050 as service_multiplier, 1.050 as extra_multiplier, 19 as sort_order
    UNION ALL
    SELECT 'Full Gut Renovation', 'Some surprises expected', 'Construction Type', 1.050, 1.050, 1.050, 1.050, 4
    UNION ALL
    SELECT 'Multi-Story 3rd Floor+', 'Extra time carrying materials', 'Location/Access', 1.050, 1.050, 1.050, 1.050, 15
    UNION ALL
    SELECT 'Finished Basement', 'Working around finished surfaces', 'Location/Access', 1.150, 1.150, 1.150, 1.150, 16
    UNION ALL
    SELECT 'Construction Site - Multiple Trades', 'Coordination with other trades', 'Conditions', 1.100, 1.100, 1.100, 1.100, 22
    UNION ALL
    SELECT 'Designer/Architect Involved', 'More meetings, precision required', 'Conditions', 1.150, 1.150, 1.150, 1.150, 23
    UNION ALL
    SELECT 'Permit/Inspection Required', 'Extra documentation time', 'Special Circumstances', 1.050, 1.050, 1.050, 1.050, 32
    UNION ALL
    SELECT 'Insurance Job', 'Documentation, adjuster meetings', 'Special Circumstances', 1.200, 1.200, 1.200, 1.200, 33
) AS new_presets
WHERE NOT EXISTS (
    SELECT 1 FROM DifficultyPresets dp 
    WHERE dp.name = new_presets.name
);

-- =====================================================
-- 2. Add integration columns and tables
-- =====================================================

-- Add EstimateId to Jobs table if it doesn't exist
SET @db_name = DATABASE();
SET @table_name = 'Jobs';
SET @column_name = 'estimate_id';
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = @db_name 
    AND TABLE_NAME = @table_name 
    AND COLUMN_NAME = @column_name
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE Jobs ADD COLUMN estimate_id INT NULL AFTER notes,
     ADD INDEX idx_job_estimate (estimate_id ASC),
     ADD CONSTRAINT fk_Jobs_Estimates
       FOREIGN KEY (estimate_id)
       REFERENCES Estimates (estimate_id)
       ON DELETE SET NULL
       ON UPDATE NO ACTION',
    'SELECT "Column estimate_id already exists in Jobs table"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add Assembly reference to EstimateLineItems if it doesn't exist
SET @column_name = 'assembly_id';
SET @table_name = 'EstimateLineItems';
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = @db_name 
    AND TABLE_NAME = @table_name 
    AND COLUMN_NAME = @column_name
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE EstimateLineItems ADD COLUMN assembly_id INT NULL AFTER item_code,
     ADD INDEX idx_line_assembly (assembly_id ASC),
     ADD CONSTRAINT fk_EstimateLineItems_AssemblyTemplates
       FOREIGN KEY (assembly_id)
       REFERENCES AssemblyTemplates (assembly_id)
       ON DELETE SET NULL
       ON UPDATE NO ACTION',
    'SELECT "Column assembly_id already exists in EstimateLineItems table"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add service_type_id to Estimates if it doesn't exist
SET @column_name = 'service_type_id';
SET @table_name = 'Estimates';
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = @db_name 
    AND TABLE_NAME = @table_name 
    AND COLUMN_NAME = @column_name
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE Estimates ADD COLUMN service_type_id INT NULL AFTER status,
     ADD INDEX idx_estimate_service_type (service_type_id ASC),
     ADD CONSTRAINT fk_Estimates_ServiceTypes
       FOREIGN KEY (service_type_id)
       REFERENCES ServiceTypes (service_type_id)
       ON DELETE SET NULL
       ON UPDATE NO ACTION',
    'SELECT "Column service_type_id already exists in Estimates table"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add service_type_id to Jobs if it doesn't exist
SET @table_name = 'Jobs';
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = @db_name 
    AND TABLE_NAME = @table_name 
    AND COLUMN_NAME = @column_name
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE Jobs ADD COLUMN service_type_id INT NULL AFTER estimate_id,
     ADD INDEX idx_job_service_type (service_type_id ASC),
     ADD CONSTRAINT fk_Jobs_ServiceTypes
       FOREIGN KEY (service_type_id)
       REFERENCES ServiceTypes (service_type_id)
       ON DELETE SET NULL
       ON UPDATE NO ACTION',
    'SELECT "Column service_type_id already exists in Jobs table"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add minimum time blocks to ServiceTypes if columns don't exist
SET @table_name = 'ServiceTypes';
SET @column_name = 'minimum_hours';
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = @db_name 
    AND TABLE_NAME = @table_name 
    AND COLUMN_NAME = @column_name
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE ServiceTypes
     ADD COLUMN minimum_hours DECIMAL(5,2) NOT NULL DEFAULT 1.00 AFTER labor_multiplier,
     ADD COLUMN drive_time_minimum DECIMAL(5,2) NOT NULL DEFAULT 0.50 AFTER minimum_hours',
    'SELECT "Columns minimum_hours already exist in ServiceTypes table"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Update default minimums for ServiceTypes
UPDATE ServiceTypes SET 
  minimum_hours = CASE 
    WHEN name = 'Residential Service' THEN 1.00
    WHEN name = 'Emergency/After-hours' THEN 2.00
    ELSE 0.00
  END,
  drive_time_minimum = CASE
    WHEN name = 'Residential Service' THEN 0.50
    WHEN name = 'Emergency/After-hours' THEN 0.50
    ELSE 0.25
  END
WHERE minimum_hours = 1.00; -- Only update if still at default

-- Add distance and drive time to Jobs
SET @table_name = 'Jobs';
SET @column_name = 'distance_miles';
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = @db_name 
    AND TABLE_NAME = @table_name 
    AND COLUMN_NAME = @column_name
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE Jobs
     ADD COLUMN distance_miles DECIMAL(5,1) NULL AFTER zip,
     ADD COLUMN drive_time_hours DECIMAL(5,2) NULL AFTER distance_miles',
    'SELECT "Columns distance_miles already exist in Jobs table"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Create EstimateConversions table if it doesn't exist
CREATE TABLE IF NOT EXISTS `EstimateConversions` (
  `conversion_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
  `job_id` INT NOT NULL,
  `conversion_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `converted_by` VARCHAR(50) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`conversion_id`),
  UNIQUE INDEX `estimate_job_unique` (`estimate_id`, `job_id`),
  CONSTRAINT `fk_EstimateConversions_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_EstimateConversions_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- Create AssemblyUsageStats table if it doesn't exist
CREATE TABLE IF NOT EXISTS `AssemblyUsageStats` (
  `usage_id` INT NOT NULL AUTO_INCREMENT,
  `assembly_id` INT NOT NULL,
  `estimate_id` INT NULL,
  `job_id` INT NULL,
  `quantity_used` INT NOT NULL DEFAULT 1,
  `labor_minutes_adjusted` BOOLEAN NOT NULL DEFAULT FALSE,
  `usage_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`usage_id`),
  INDEX `idx_assembly_usage` (`assembly_id` ASC),
  INDEX `idx_usage_date` (`usage_date` ASC),
  CONSTRAINT `fk_AssemblyUsageStats_Assemblies`
    FOREIGN KEY (`assembly_id`)
    REFERENCES `AssemblyTemplates` (`assembly_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_AssemblyUsageStats_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_AssemblyUsageStats_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- Add quick_code support to PriceList
SET @table_name = 'PriceList';
SET @column_name = 'quick_code';
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = @db_name 
    AND TABLE_NAME = @table_name 
    AND COLUMN_NAME = @column_name
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE PriceList
     ADD COLUMN quick_code VARCHAR(20) NULL AFTER item_code,
     ADD INDEX idx_quick_code (quick_code ASC)',
    'SELECT "Column quick_code already exists in PriceList table"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =====================================================
-- 3. Create missing views
-- =====================================================

-- View for material price changes
CREATE OR REPLACE VIEW vw_material_price_changes AS
SELECT 
    m.material_code,
    m.name AS material_name,
    mph1.price AS current_price,
    mph2.price AS previous_price,
    ROUND(((mph1.price - mph2.price) / mph2.price * 100), 2) AS percent_change,
    mph1.effective_date AS current_date,
    mph2.effective_date AS previous_date,
    DATEDIFF(mph1.effective_date, mph2.effective_date) AS days_between
FROM Materials m
JOIN MaterialPriceHistory mph1 ON m.material_id = mph1.material_id
LEFT JOIN MaterialPriceHistory mph2 ON m.material_id = mph2.material_id 
    AND mph2.effective_date = (
        SELECT MAX(effective_date) 
        FROM MaterialPriceHistory 
        WHERE material_id = m.material_id 
        AND effective_date < mph1.effective_date
    )
WHERE mph1.effective_date = (
    SELECT MAX(effective_date) 
    FROM MaterialPriceHistory 
    WHERE material_id = m.material_id
);

-- View for assembly variants
CREATE OR REPLACE VIEW vw_assembly_with_variants AS
SELECT 
    parent.assembly_code AS code,
    parent.name AS parent_name,
    variant.assembly_id,
    variant.name AS variant_name,
    variant.is_default,
    av.sort_order,
    COALESCE(ac.total_material_cost, 0) AS material_cost,
    variant.rough_minutes,
    variant.finish_minutes,
    variant.service_minutes,
    variant.extra_minutes
FROM AssemblyTemplates parent
JOIN AssemblyVariants av ON parent.assembly_id = av.parent_assembly_id
JOIN AssemblyTemplates variant ON av.variant_assembly_id = variant.assembly_id
LEFT JOIN vw_assembly_costs ac ON variant.assembly_id = ac.assembly_id
WHERE parent.is_active = TRUE AND variant.is_active = TRUE
ORDER BY parent.assembly_code, av.sort_order;

-- View for estimate to job tracking
CREATE OR REPLACE VIEW vw_estimate_job_tracking AS
SELECT 
    e.estimate_id,
    e.estimate_number,
    e.customer_id,
    c.name AS customer_name,
    e.total_price AS estimate_total,
    e.status AS estimate_status,
    e.created_date AS estimate_date,
    j.job_id,
    j.job_number,
    j.total_actual AS job_actual,
    j.status AS job_status,
    ec.conversion_date,
    DATEDIFF(ec.conversion_date, e.created_date) AS days_to_convert,
    (j.total_actual - e.total_price) AS variance,
    ROUND(((j.total_actual - e.total_price) / e.total_price * 100), 2) AS variance_percent
FROM Estimates e
LEFT JOIN EstimateConversions ec ON e.estimate_id = ec.estimate_id
LEFT JOIN Jobs j ON ec.job_id = j.job_id
LEFT JOIN Customers c ON e.customer_id = c.customer_id
ORDER BY e.created_date DESC;

-- =====================================================
-- 4. Update quick codes in PriceList (if quick_code column was just added)
-- =====================================================

-- Add common quick codes based on item names
UPDATE PriceList SET quick_code = 'o' 
WHERE (name LIKE '%outlet%' OR name LIKE '%receptacle%') 
AND name NOT LIKE '%gfci%' AND name NOT LIKE '%gfi%' 
AND quick_code IS NULL;

UPDATE PriceList SET quick_code = 's' 
WHERE name LIKE '%switch%' AND name LIKE '%single%' 
AND quick_code IS NULL;

UPDATE PriceList SET quick_code = '3w' 
WHERE (name LIKE '%3%way%' OR name LIKE '%three%way%') 
AND quick_code IS NULL;

UPDATE PriceList SET quick_code = 'hh' 
WHERE (name LIKE '%recessed%' OR name LIKE '%high%hat%') 
AND quick_code IS NULL;

UPDATE PriceList SET quick_code = 'gfi' 
WHERE (name LIKE '%gfci%' OR name LIKE '%gfi%') 
AND quick_code IS NULL;

UPDATE PriceList SET quick_code = 'ex' 
WHERE (name LIKE '%exhaust%' OR name LIKE '%vent%fan%') 
AND quick_code IS NULL;

-- =====================================================
-- 5. Report what was added
-- =====================================================

SELECT 'Migration Summary:' AS Report;

SELECT 
    'Difficulty Presets Added' AS Item,
    COUNT(*) AS Count
FROM DifficultyPresets
WHERE name IN (
    'Occupied Home - Minimal',
    'Full Gut Renovation',
    'Multi-Story 3rd Floor+',
    'Finished Basement',
    'Construction Site - Multiple Trades',
    'Designer/Architect Involved',
    'Permit/Inspection Required',
    'Insurance Job'
);

SELECT 
    'New Tables Created' AS Item,
    COUNT(*) AS Count
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME IN ('EstimateConversions', 'AssemblyUsageStats');

SELECT 
    'Quick Codes Added to PriceList' AS Item,
    COUNT(*) AS Count
FROM PriceList
WHERE quick_code IS NOT NULL;

SELECT 'Migration complete! Run populate_sample_assemblies.sql next to add sample data.' AS NextStep;