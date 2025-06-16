-- Integration Updates for existing tables
-- Run this after add_pricing_tables.sql

USE electrical_contractor_db;

-- -----------------------------------------------------
-- Add EstimateId to Jobs table for tracking source estimate
-- -----------------------------------------------------
ALTER TABLE `Jobs` 
ADD COLUMN `estimate_id` INT NULL AFTER `notes`,
ADD INDEX `idx_job_estimate` (`estimate_id` ASC),
ADD CONSTRAINT `fk_Jobs_Estimates`
  FOREIGN KEY (`estimate_id`)
  REFERENCES `Estimates` (`estimate_id`)
  ON DELETE SET NULL
  ON UPDATE NO ACTION;

-- -----------------------------------------------------
-- Add Assembly reference to EstimateLineItems
-- -----------------------------------------------------
ALTER TABLE `EstimateLineItems`
ADD COLUMN `assembly_id` INT NULL AFTER `item_code`,
ADD INDEX `idx_line_assembly` (`assembly_id` ASC),
ADD CONSTRAINT `fk_EstimateLineItems_AssemblyTemplates`
  FOREIGN KEY (`assembly_id`)
  REFERENCES `AssemblyTemplates` (`assembly_id`)
  ON DELETE SET NULL
  ON UPDATE NO ACTION;

-- -----------------------------------------------------
-- Add Service Type to Estimates and Jobs
-- -----------------------------------------------------
ALTER TABLE `Estimates`
ADD COLUMN `service_type_id` INT NULL AFTER `status`,
ADD INDEX `idx_estimate_service_type` (`service_type_id` ASC),
ADD CONSTRAINT `fk_Estimates_ServiceTypes`
  FOREIGN KEY (`service_type_id`)
  REFERENCES `ServiceTypes` (`service_type_id`)
  ON DELETE SET NULL
  ON UPDATE NO ACTION;

ALTER TABLE `Jobs`
ADD COLUMN `service_type_id` INT NULL AFTER `estimate_id`,
ADD INDEX `idx_job_service_type` (`service_type_id` ASC),
ADD CONSTRAINT `fk_Jobs_ServiceTypes`
  FOREIGN KEY (`service_type_id`)
  REFERENCES `ServiceTypes` (`service_type_id`)
  ON DELETE SET NULL
  ON UPDATE NO ACTION;

-- -----------------------------------------------------
-- Add minimum time blocks to ServiceTypes
-- -----------------------------------------------------
ALTER TABLE `ServiceTypes`
ADD COLUMN `minimum_hours` DECIMAL(5,2) NOT NULL DEFAULT 1.00 AFTER `base_rate`,
ADD COLUMN `drive_time_minimum` DECIMAL(5,2) NOT NULL DEFAULT 0.50 AFTER `minimum_hours`;

-- Update default minimums
UPDATE `ServiceTypes` SET 
  `minimum_hours` = CASE 
    WHEN `name` = 'Residential Service' THEN 1.00
    WHEN `name` = 'Emergency/After-hours' THEN 2.00
    ELSE 0.00
  END,
  `drive_time_minimum` = CASE
    WHEN `name` = 'Residential Service' THEN 0.50
    WHEN `name` = 'Emergency/After-hours' THEN 0.50
    ELSE 0.25
  END;

-- -----------------------------------------------------
-- Add distance-based drive time to Jobs
-- -----------------------------------------------------
ALTER TABLE `Jobs`
ADD COLUMN `distance_miles` DECIMAL(5,1) NULL AFTER `zip`,
ADD COLUMN `drive_time_hours` DECIMAL(5,2) NULL AFTER `distance_miles`;

-- -----------------------------------------------------
-- Create table for tracking estimate-to-job conversions
-- -----------------------------------------------------
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

-- -----------------------------------------------------
-- Add Assembly usage tracking
-- -----------------------------------------------------
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

-- -----------------------------------------------------
-- Add quick code support to PriceList for backward compatibility
-- -----------------------------------------------------
ALTER TABLE `PriceList`
ADD COLUMN `quick_code` VARCHAR(20) NULL AFTER `item_code`,
ADD INDEX `idx_quick_code` (`quick_code` ASC);

-- -----------------------------------------------------
-- Create view for estimate to job tracking
-- -----------------------------------------------------
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
