-- Script to merge the estimating database into the main contractor database
-- Run this against your electrical_contractor_db database

-- First, add the EstimateId field to Jobs table if it doesn't exist
ALTER TABLE Jobs 
ADD COLUMN IF NOT EXISTS `estimate_id` INT NULL AFTER `notes`,
ADD CONSTRAINT `fk_Jobs_Estimates` FOREIGN KEY (`estimate_id`) 
    REFERENCES `Estimates` (`estimate_id`) ON DELETE SET NULL ON UPDATE NO ACTION;

-- Create the Estimates table
CREATE TABLE IF NOT EXISTS `Estimates` (
  `estimate_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_number` VARCHAR(20) NOT NULL,
  `customer_id` INT NOT NULL,
  `job_name` VARCHAR(255) NOT NULL,
  `job_address` VARCHAR(255) NULL,
  `job_city` VARCHAR(50) NULL,
  `job_state` VARCHAR(2) NULL,
  `job_zip` VARCHAR(10) NULL,
  `square_footage` INT NULL,
  `num_floors` INT NULL,
  `status` VARCHAR(20) NOT NULL DEFAULT 'Draft',
  `created_date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `expiration_date` DATE NULL,
  `notes` TEXT NULL,
  `tax_rate` DECIMAL(5,3) NOT NULL DEFAULT 0.064,
  `material_markup` DECIMAL(5,2) NOT NULL DEFAULT 22.00,
  `labor_rate` DECIMAL(10,2) NOT NULL DEFAULT 85.00,
  `total_material_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `total_labor_minutes` INT NOT NULL DEFAULT 0,
  `total_price` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `job_id` INT NULL,
  PRIMARY KEY (`estimate_id`),
  UNIQUE INDEX `estimate_number_UNIQUE` (`estimate_number` ASC),
  CONSTRAINT `fk_Estimates_Customers`
    FOREIGN KEY (`customer_id`)
    REFERENCES `Customers` (`customer_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Estimates_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- Create EstimateRooms table
CREATE TABLE IF NOT EXISTS `EstimateRooms` (
  `room_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
  `room_name` VARCHAR(100) NOT NULL,
  `room_order` INT NOT NULL DEFAULT 0,
  `notes` TEXT NULL,
  PRIMARY KEY (`room_id`),
  CONSTRAINT `fk_EstimateRooms_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- Create EstimateLineItems table (without estimate_id field - it's accessed through room)
CREATE TABLE IF NOT EXISTS `EstimateLineItems` (
  `line_id` INT NOT NULL AUTO_INCREMENT,
  `room_id` INT NOT NULL,
  `item_id` INT NULL,
  `quantity` INT NOT NULL DEFAULT 1,
  `item_code` VARCHAR(20) NULL,
  `description` VARCHAR(255) NOT NULL,
  `unit_price` DECIMAL(10,2) NOT NULL,
  `material_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `labor_minutes` INT NOT NULL DEFAULT 0,
  `line_order` INT NOT NULL DEFAULT 0,
  `notes` TEXT NULL,
  PRIMARY KEY (`line_id`),
  CONSTRAINT `fk_EstimateLineItems_Rooms`
    FOREIGN KEY (`room_id`)
    REFERENCES `EstimateRooms` (`room_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- Create EstimateStageSummary table
CREATE TABLE IF NOT EXISTS `EstimateStageSummary` (
  `summary_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
  `stage` VARCHAR(20) NOT NULL,
  `labor_hours` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `material_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  PRIMARY KEY (`summary_id`),
  UNIQUE INDEX `estimate_stage_unique` (`estimate_id` ASC, `stage` ASC),
  CONSTRAINT `fk_EstimateStageSummary_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- Update PriceList table to include additional fields needed for estimating
ALTER TABLE PriceList 
ADD COLUMN IF NOT EXISTS `labor_rough` INT DEFAULT 0 AFTER `labor_minutes`,
ADD COLUMN IF NOT EXISTS `labor_finish` INT DEFAULT 0 AFTER `labor_rough`,
ADD COLUMN IF NOT EXISTS `labor_service` INT DEFAULT 0 AFTER `labor_finish`,
ADD COLUMN IF NOT EXISTS `labor_extra` INT DEFAULT 0 AFTER `labor_service`,
ADD COLUMN IF NOT EXISTS `sell_price` DECIMAL(10,2) GENERATED ALWAYS AS (base_cost * (1 + tax_rate)) STORED AFTER `markup_percentage`;

-- Add hourly_rate to Employees if not exists
ALTER TABLE Employees 
MODIFY COLUMN `hourly_rate` DECIMAL(10,2) NOT NULL DEFAULT 65.00,
ADD COLUMN IF NOT EXISTS `vehicle_cost_per_hour` DECIMAL(10,2) DEFAULT 0 AFTER `burden_rate`,
ADD COLUMN IF NOT EXISTS `overhead_percentage` DECIMAL(5,2) DEFAULT 0 AFTER `vehicle_cost_per_hour`,
ADD COLUMN IF NOT EXISTS `effective_rate` DECIMAL(10,2) GENERATED ALWAYS AS 
    (hourly_rate + IFNULL(burden_rate, 0) + IFNULL(vehicle_cost_per_hour, 0) + (hourly_rate * IFNULL(overhead_percentage, 0) / 100)) STORED;

-- Update existing price list items with proper codes if they exist
UPDATE PriceList SET item_code = 'fridge' WHERE name LIKE '%Refrigerator%' AND item_code IS NULL;
UPDATE PriceList SET item_code = 'micro' WHERE name LIKE '%Microwave%' AND item_code IS NULL;
UPDATE PriceList SET item_code = 'dw' WHERE name LIKE '%Dishwasher%' AND item_code IS NULL;
UPDATE PriceList SET item_code = 'hh' WHERE name LIKE '%high hat%' OR name LIKE '%recessed%' AND item_code IS NULL;
UPDATE PriceList SET item_code = 'O' WHERE name LIKE '%Outlet%' AND name NOT LIKE '%GFI%' AND item_code IS NULL;
UPDATE PriceList SET item_code = 'S' WHERE name LIKE '%Single Pole%Switch%' AND item_code IS NULL;
UPDATE PriceList SET item_code = '3W' WHERE name LIKE '%3-way%Switch%' OR name LIKE '%3w%Switch%' AND item_code IS NULL;
UPDATE PriceList SET item_code = 'Gfi' WHERE name LIKE '%GFI%' OR name LIKE '%GFCI%' AND item_code IS NULL;