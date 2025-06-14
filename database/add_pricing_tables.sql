-- Additional tables for enhanced pricing and assembly system
-- Run this after the main electrical_contractor_db.sql

USE electrical_contractor_db;

-- Materials table for raw components
CREATE TABLE IF NOT EXISTS `Materials` (
  `material_id` INT NOT NULL AUTO_INCREMENT,
  `material_code` VARCHAR(20) NOT NULL,
  `name` VARCHAR(100) NOT NULL,
  `description` TEXT NULL,
  `category` VARCHAR(50) NOT NULL,
  `unit_of_measure` VARCHAR(20) NOT NULL DEFAULT 'Each',
  `current_price` DECIMAL(10,2) NOT NULL,
  `tax_rate` DECIMAL(5,3) NOT NULL DEFAULT 6.4,
  `min_stock_level` INT NULL DEFAULT 0,
  `max_stock_level` INT NULL DEFAULT 0,
  `preferred_vendor_id` INT NULL,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_date` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`material_id`),
  UNIQUE INDEX `material_code_UNIQUE` (`material_code` ASC),
  INDEX `idx_category` (`category` ASC),
  CONSTRAINT `fk_Materials_Vendors`
    FOREIGN KEY (`preferred_vendor_id`)
    REFERENCES `Vendors` (`vendor_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- Material price history for tracking price changes
CREATE TABLE IF NOT EXISTS `MaterialPriceHistory` (
  `price_history_id` INT NOT NULL AUTO_INCREMENT,
  `material_id` INT NOT NULL,
  `price` DECIMAL(10,2) NOT NULL,
  `effective_date` DATE NOT NULL,
  `vendor_id` INT NULL,
  `purchase_order_number` VARCHAR(50) NULL,
  `quantity_purchased` DECIMAL(10,2) NULL,
  `notes` TEXT NULL,
  `created_by` VARCHAR(50) NOT NULL,
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`price_history_id`),
  INDEX `idx_material_date` (`material_id` ASC, `effective_date` DESC),
  CONSTRAINT `fk_PriceHistory_Materials`
    FOREIGN KEY (`material_id`)
    REFERENCES `Materials` (`material_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_PriceHistory_Vendors`
    FOREIGN KEY (`vendor_id`)
    REFERENCES `Vendors` (`vendor_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- Assembly templates (your quick codes like 'o', 's', 'hh')
CREATE TABLE IF NOT EXISTS `AssemblyTemplates` (
  `assembly_id` INT NOT NULL AUTO_INCREMENT,
  `assembly_code` VARCHAR(20) NOT NULL,
  `name` VARCHAR(100) NOT NULL,
  `description` TEXT NULL,
  `category` VARCHAR(50) NOT NULL,
  `rough_minutes` INT NOT NULL DEFAULT 0,
  `finish_minutes` INT NOT NULL DEFAULT 0,
  `service_minutes` INT NOT NULL DEFAULT 0,
  `extra_minutes` INT NOT NULL DEFAULT 0,
  `is_default` BOOLEAN NOT NULL DEFAULT TRUE,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  `sort_order` INT NOT NULL DEFAULT 0,
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `created_by` VARCHAR(50) NOT NULL,
  `updated_date` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`assembly_id`),
  INDEX `idx_assembly_code` (`assembly_code` ASC),
  INDEX `idx_category` (`category` ASC)
);

-- Assembly components (what materials make up each assembly)
CREATE TABLE IF NOT EXISTS `AssemblyComponents` (
  `component_id` INT NOT NULL AUTO_INCREMENT,
  `assembly_id` INT NOT NULL,
  `material_id` INT NOT NULL,
  `quantity` DECIMAL(10,3) NOT NULL DEFAULT 1,
  `is_optional` BOOLEAN NOT NULL DEFAULT FALSE,
  `notes` TEXT NULL,
  PRIMARY KEY (`component_id`),
  UNIQUE INDEX `unique_assembly_material` (`assembly_id` ASC, `material_id` ASC),
  CONSTRAINT `fk_Components_Assemblies`
    FOREIGN KEY (`assembly_id`)
    REFERENCES `AssemblyTemplates` (`assembly_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Components_Materials`
    FOREIGN KEY (`material_id`)
    REFERENCES `Materials` (`material_id`)
    ON DELETE RESTRICT
    ON UPDATE NO ACTION
);

-- Assembly variants (different versions of the same code)
CREATE TABLE IF NOT EXISTS `AssemblyVariants` (
  `variant_id` INT NOT NULL AUTO_INCREMENT,
  `parent_assembly_id` INT NOT NULL,
  `variant_assembly_id` INT NOT NULL,
  `sort_order` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`variant_id`),
  UNIQUE INDEX `unique_variant` (`parent_assembly_id` ASC, `variant_assembly_id` ASC),
  CONSTRAINT `fk_Variants_Parent`
    FOREIGN KEY (`parent_assembly_id`)
    REFERENCES `AssemblyTemplates` (`assembly_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Variants_Variant`
    FOREIGN KEY (`variant_assembly_id`)
    REFERENCES `AssemblyTemplates` (`assembly_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- Difficulty presets for job complexity
CREATE TABLE IF NOT EXISTS `DifficultyPresets` (
  `preset_id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(50) NOT NULL,
  `description` TEXT NULL,
  `category` VARCHAR(50) NOT NULL,
  `rough_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `finish_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `service_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `extra_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  `sort_order` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`preset_id`),
  INDEX `idx_category` (`category` ASC)
);

-- Labor adjustments tracking
CREATE TABLE IF NOT EXISTS `LaborAdjustments` (
  `adjustment_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NULL,
  `job_id` INT NULL,
  `adjustment_type` ENUM('PRESET', 'ACCESS', 'CUSTOM', 'SEASON', 'LEARN', 'EQUIP') NOT NULL,
  `preset_id` INT NULL,
  `rough_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `finish_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `service_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `extra_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `reason` TEXT NULL,
  `created_by` VARCHAR(50) NOT NULL,
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`adjustment_id`),
  INDEX `idx_estimate` (`estimate_id` ASC),
  INDEX `idx_job` (`job_id` ASC),
  CONSTRAINT `fk_Adjustments_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Adjustments_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Adjustments_Presets`
    FOREIGN KEY (`preset_id`)
    REFERENCES `DifficultyPresets` (`preset_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- Service call types with labor multipliers
CREATE TABLE IF NOT EXISTS `ServiceTypes` (
  `service_type_id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(50) NOT NULL,
  `description` TEXT NULL,
  `labor_multiplier` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `minimum_hours` DECIMAL(5,2) NOT NULL DEFAULT 1.00,
  `drive_time_included` DECIMAL(5,2) NOT NULL DEFAULT 0.00,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  PRIMARY KEY (`service_type_id`)
);

-- Insert default difficulty presets
INSERT INTO `DifficultyPresets` (`name`, `description`, `category`, `rough_multiplier`, `finish_multiplier`, `sort_order`) VALUES
('New Construction - Stud Phase', 'Everything exposed and accessible', 'Construction Type', 0.85, 0.85, 1),
('New Construction - Trim Phase', 'Clean environment', 'Construction Type', 0.95, 0.95, 2),
('Standard Renovation', 'Baseline for typical renovation work', 'Construction Type', 1.00, 1.00, 3),
('Old House (1960-1980)', 'Older construction methods', 'Construction Type', 1.10, 1.10, 4),
('Old House (pre-1960)', 'Knob & tube, plaster walls', 'Construction Type', 1.20, 1.20, 5),
('Historic Property', 'Preservation requirements', 'Construction Type', 1.30, 1.30, 6),
('Beach/Shore House', 'Corrosion and access issues', 'Location', 1.20, 1.10, 10),
('Crawl Space Work', 'Difficult access', 'Location', 1.35, 1.00, 11),
('Attic Work - Summer', 'Heat conditions', 'Location', 1.40, 1.00, 12),
('Attic Work - Winter', 'Cold but manageable', 'Location', 1.25, 1.00, 13),
('Cathedral Ceilings', 'Height and access', 'Location', 1.20, 1.20, 14),
('Occupied Home - Full', 'Working around furniture and residents', 'Conditions', 1.15, 1.15, 20),
('High-End Finishes', 'Extra care required', 'Conditions', 1.00, 1.25, 21),
('December Work', 'Holiday decorations', 'Seasonal', 1.15, 1.15, 30),
('Summer Peak', 'Heat and homeowner presence', 'Seasonal', 1.10, 1.10, 31);

-- Insert default service types
INSERT INTO `ServiceTypes` (`name`, `description`, `labor_multiplier`, `minimum_hours`, `drive_time_included`) VALUES
('Residential New', 'Standard residential new construction', 1.00, 0.00, 0.00),
('Residential Service', 'Service calls to existing homes', 1.50, 1.00, 0.50),
('Commercial', 'Commercial work', 1.10, 4.00, 0.00),
('Emergency/After-hours', 'Emergency or after-hours calls', 2.00, 2.00, 0.50);

-- Add indexes for performance
CREATE INDEX idx_material_history_avg ON MaterialPriceHistory(material_id, effective_date);
CREATE INDEX idx_assembly_active ON AssemblyTemplates(assembly_code, is_active);
CREATE INDEX idx_estimate_adjustments ON LaborAdjustments(estimate_id, adjustment_type);

-- Add views for common queries
CREATE VIEW vw_current_material_prices AS
SELECT 
    m.material_id,
    m.material_code,
    m.name,
    m.category,
    m.current_price,
    m.tax_rate,
    ROUND(m.current_price * (1 + m.tax_rate/100), 2) as price_with_tax,
    (
        SELECT AVG(mph.price) 
        FROM MaterialPriceHistory mph 
        WHERE mph.material_id = m.material_id 
        AND mph.effective_date >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)
    ) as avg_30_day_price,
    (
        SELECT AVG(mph.price) 
        FROM MaterialPriceHistory mph 
        WHERE mph.material_id = m.material_id 
        AND mph.effective_date >= DATE_SUB(CURDATE(), INTERVAL 90 DAY)
    ) as avg_90_day_price
FROM Materials m
WHERE m.is_active = TRUE;

CREATE VIEW vw_assembly_costs AS
SELECT 
    at.assembly_id,
    at.assembly_code,
    at.name,
    at.rough_minutes,
    at.finish_minutes,
    SUM(ac.quantity * m.current_price * (1 + m.tax_rate/100)) as total_material_cost
FROM AssemblyTemplates at
LEFT JOIN AssemblyComponents ac ON at.assembly_id = ac.assembly_id
LEFT JOIN Materials m ON ac.material_id = m.material_id
WHERE at.is_active = TRUE
GROUP BY at.assembly_id;
