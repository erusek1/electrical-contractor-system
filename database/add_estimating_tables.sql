-- Add Estimating Tables to Existing Database
-- Run this script on your existing electrical_contractor_db

-- Only add tables that don't exist yet
-- This script checks for existing tables to avoid conflicts

-- -----------------------------------------------------
-- Table `PriceList` (matching existing system)
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `PriceList` (
  `item_id` INT NOT NULL AUTO_INCREMENT,
  `category` VARCHAR(50) NULL,
  `item_code` VARCHAR(20) NOT NULL,
  `name` VARCHAR(100) NOT NULL,
  `description` TEXT NULL,
  `base_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `tax_rate` DECIMAL(5,3) NOT NULL DEFAULT 0.064,
  `labor_minutes` INT NOT NULL DEFAULT 0,
  `markup_percentage` DECIMAL(5,2) NOT NULL DEFAULT 0,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  `notes` TEXT NULL,
  PRIMARY KEY (`item_id`),
  UNIQUE INDEX `item_code_UNIQUE` (`item_code` ASC),
  INDEX `idx_category` (`category` ASC),
  INDEX `idx_active` (`is_active` ASC)
);

-- -----------------------------------------------------
-- Table `Estimates`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Estimates` (
  `estimate_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_number` VARCHAR(20) NOT NULL,
  `version` INT NOT NULL DEFAULT 1,
  `customer_id` INT NOT NULL,
  `job_name` VARCHAR(255) NOT NULL,
  `job_address` VARCHAR(255) NULL,
  `job_city` VARCHAR(50) NULL,
  `job_state` VARCHAR(2) NULL,
  `job_zip` VARCHAR(10) NULL,
  `square_footage` INT NULL,
  `num_floors` INT NULL,
  `status` VARCHAR(20) NOT NULL DEFAULT 'Draft',
  `created_date` DATETIME NOT NULL,
  `created_by` VARCHAR(50) NULL,
  `modified_date` DATETIME NULL,
  `modified_by` VARCHAR(50) NULL,
  `sent_date` DATETIME NULL,
  `approved_date` DATETIME NULL,
  `rejected_date` DATETIME NULL,
  `expiration_date` DATETIME NULL,
  `notes` TEXT NULL,
  `tax_rate` DECIMAL(5,3) NOT NULL DEFAULT 0.064,
  `material_markup` DECIMAL(5,2) NOT NULL DEFAULT 22.00,
  `labor_rate` DECIMAL(10,2) NOT NULL DEFAULT 85.00,
  `total_material_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `total_labor_minutes` INT NOT NULL DEFAULT 0,
  `total_labor_hours` DECIMAL(10,2) GENERATED ALWAYS AS (total_labor_minutes / 60) STORED,
  `total_labor_cost` DECIMAL(10,2) GENERATED ALWAYS AS ((total_labor_minutes / 60) * labor_rate) STORED,
  `total_price` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `job_id` INT NULL,
  PRIMARY KEY (`estimate_id`),
  UNIQUE INDEX `estimate_number_version_UNIQUE` (`estimate_number` ASC, `version` ASC),
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

-- -----------------------------------------------------
-- Table `EstimateRooms`
-- -----------------------------------------------------
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

-- -----------------------------------------------------
-- Table `EstimateLineItems`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `EstimateLineItems` (
  `line_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
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
  CONSTRAINT `fk_EstimateLineItems_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_EstimateLineItems_Rooms`
    FOREIGN KEY (`room_id`)
    REFERENCES `EstimateRooms` (`room_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_EstimateLineItems_PriceList`
    FOREIGN KEY (`item_id`)
    REFERENCES `PriceList` (`item_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `EstimateStageSummary`
-- -----------------------------------------------------
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

-- -----------------------------------------------------
-- Table `RoomTemplates` (fix structure)
-- -----------------------------------------------------
DROP TABLE IF EXISTS `RoomTemplateItems`;
DROP TABLE IF EXISTS `RoomTemplates`;

CREATE TABLE `RoomTemplates` (
  `template_id` INT NOT NULL AUTO_INCREMENT,
  `template_name` VARCHAR(100) NOT NULL,
  `room_type` VARCHAR(50) NOT NULL,
  `description` TEXT NULL,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  `created_date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`template_id`),
  INDEX `idx_room_type` (`room_type` ASC)
);

-- -----------------------------------------------------
-- Table `RoomTemplateItems`
-- -----------------------------------------------------
CREATE TABLE `RoomTemplateItems` (
  `template_item_id` INT NOT NULL AUTO_INCREMENT,
  `template_id` INT NOT NULL,
  `item_id` INT NOT NULL,
  `quantity` INT NOT NULL DEFAULT 1,
  `is_optional` BOOLEAN NOT NULL DEFAULT FALSE,
  `notes` TEXT NULL,
  PRIMARY KEY (`template_item_id`),
  CONSTRAINT `fk_RoomTemplateItems_Templates`
    FOREIGN KEY (`template_id`)
    REFERENCES `RoomTemplates` (`template_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_RoomTemplateItems_Items`
    FOREIGN KEY (`item_id`)
    REFERENCES `PriceList` (`item_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Add estimate_id to Jobs table for linking (if not exists)
-- -----------------------------------------------------
SET @col_exists = 0;
SELECT COUNT(*) INTO @col_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Jobs' 
  AND COLUMN_NAME = 'estimate_id';

SET @sql = IF(@col_exists = 0,
  'ALTER TABLE `Jobs` 
   ADD COLUMN `estimate_id` INT NULL AFTER `notes`,
   ADD CONSTRAINT `fk_Jobs_Estimates`
     FOREIGN KEY (`estimate_id`)
     REFERENCES `Estimates` (`estimate_id`)
     ON DELETE SET NULL
     ON UPDATE NO ACTION',
  'SELECT "Column estimate_id already exists in Jobs table"');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------
-- Sample Price List Items (matching your Excel codes)
-- -----------------------------------------------------
INSERT INTO `PriceList` (`item_code`, `name`, `base_cost`, `category`, `labor_minutes`) VALUES 
('fridge', 'Refrigerator receptacle', 68.73, 'Kitchen', 45),
('micro', 'Microwave receptacle', 68.73, 'Kitchen', 45),
('dw', 'Dishwasher receptacle', 68.73, 'Kitchen', 45),
('hood', 'Hood receptacle', 68.73, 'Kitchen', 45),
('oven', '240 volt 60 amp electric oven 50\' with gfci protection as per 2020 code change', 415.63, 'Kitchen', 120),
('cook', '240 volt 30 amp cooktop 50\'', 267.21, 'Kitchen', 90),
('hh', '4" LED recessed light (high hat)', 82.59, 'Lighting', 65),
('pend', 'Owner supplied pendant light', 81.36, 'Lighting', 45),
('O', 'Decora Outlet', 68.73, 'General', 30),
('S', 'Single Pole Decora Switch', 68.17, 'General', 30),
('3W', '3-way Decora Switch', 69.12, 'General', 45),
('Dim', 'Diva style dimmer switch', 84.18, 'General', 35),
('Gfi', '15a TP GFI', 83.74, 'General', 35),
('ARL', 'Outdoor gfi receptacle with weather proof cover', 103.87, 'Exterior', 45),
('Sc', 'Owner supplied wall sconce', 81.91, 'Lighting', 45),
('Van', 'Owner supplied vanity light', 81.36, 'Lighting', 45),
('Ex-l', 'Panasonic 110cfm ex Fan/Light', 150.00, 'Ventilation', 60),
('yellow', '12/2 wire (per 100\')', 112.00, 'Wire', 0),
('3yellow', '12/3 wire (per 100\')', 164.63, 'Wire', 0),
('white', '14/2 wire (per 100\')', 83.13, 'Wire', 0),
('3white', '14/3 wire (per 100\')', 108.93, 'Wire', 0),
('Plugmold', 'Plugmold', 195.00, 'Kitchen', 60),
('tape', 'Under cabinet LED tape lighting 15\'', 325.00, 'Lighting', 90)
ON DUPLICATE KEY UPDATE 
  name = VALUES(name),
  base_cost = VALUES(base_cost),
  category = VALUES(category),
  labor_minutes = VALUES(labor_minutes);

-- -----------------------------------------------------
-- Sample Room Templates
-- -----------------------------------------------------
INSERT INTO `RoomTemplates` (`template_name`, `room_type`, `description`) VALUES 
('Standard Kitchen', 'Kitchen', 'Typical kitchen with standard appliances'),
('Master Bedroom', 'Bedroom', 'Master bedroom with ceiling fan and multiple outlets'),
('Standard Bathroom', 'Bathroom', 'Bathroom with vanity light and exhaust fan'),
('Living Room', 'Living Room', 'Living room with recessed lighting'),
('Standard Bedroom', 'Bedroom', 'Regular bedroom with basic electrical'),
('Half Bath', 'Bathroom', 'Powder room with basic electrical');

-- Create kitchen template items
INSERT INTO `RoomTemplateItems` (`template_id`, `item_id`, `quantity`)
SELECT 
  (SELECT template_id FROM RoomTemplates WHERE template_name = 'Standard Kitchen'),
  item_id,
  CASE 
    WHEN item_code = 'hh' THEN 6
    WHEN item_code = 'O' THEN 4
    WHEN item_code = 'Gfi' THEN 2
    ELSE 1
  END
FROM PriceList 
WHERE item_code IN ('fridge', 'micro', 'dw', 'hood', 'oven', 'hh', 'O', 'Gfi', 'S', '3W');

-- -----------------------------------------------------
-- Create view for estimate conversion readiness
-- -----------------------------------------------------
CREATE OR REPLACE VIEW `v_estimates_ready_for_conversion` AS
SELECT 
  e.estimate_id,
  e.estimate_number,
  e.version,
  c.name as customer_name,
  e.job_name,
  e.total_price as total_cost,
  e.status,
  e.created_date,
  CASE 
    WHEN e.job_id IS NOT NULL THEN 'Already Converted'
    WHEN e.status = 'Approved' THEN 'Ready to Convert'
    ELSE 'Not Ready'
  END as conversion_status
FROM Estimates e
JOIN Customers c ON e.customer_id = c.customer_id
WHERE e.status IN ('Approved', 'Converted')
ORDER BY e.created_date DESC;
