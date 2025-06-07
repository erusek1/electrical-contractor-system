-- Add Estimating Tables to Existing Database
-- Run this script on your existing electrical_contractor_db

-- Only add tables that don't exist yet
-- This script checks for existing tables to avoid conflicts

-- -----------------------------------------------------
-- Table `PriceListItems` (if not exists)
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `PriceListItems` (
  `item_id` INT NOT NULL AUTO_INCREMENT,
  `item_code` VARCHAR(20) NOT NULL,
  `name` VARCHAR(255) NOT NULL,
  `description` TEXT NULL,
  `base_price` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `tax_rate` DECIMAL(5,3) NOT NULL DEFAULT 0.064,
  `total_price` DECIMAL(10,2) GENERATED ALWAYS AS (base_price * (1 + tax_rate)) STORED,
  `unit` VARCHAR(20) NULL DEFAULT 'each',
  `category` VARCHAR(50) NULL,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  `created_date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `modified_date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`item_id`),
  UNIQUE INDEX `item_code_UNIQUE` (`item_code` ASC),
  INDEX `idx_category` (`category` ASC),
  INDEX `idx_active` (`is_active` ASC)
);

-- -----------------------------------------------------
-- Table `LaborMinutes`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `LaborMinutes` (
  `labor_id` INT NOT NULL AUTO_INCREMENT,
  `item_id` INT NOT NULL,
  `stage` ENUM('Rough', 'Finish', 'Service', 'Extra') NOT NULL,
  `minutes` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`labor_id`),
  UNIQUE INDEX `item_stage_unique` (`item_id` ASC, `stage` ASC),
  CONSTRAINT `fk_LaborMinutes_Items`
    FOREIGN KEY (`item_id`)
    REFERENCES `PriceListItems` (`item_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `MaterialStages`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `MaterialStages` (
  `material_stage_id` INT NOT NULL AUTO_INCREMENT,
  `item_id` INT NOT NULL,
  `stage` ENUM('Rough', 'Finish', 'Service', 'Extra') NOT NULL,
  `material_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  PRIMARY KEY (`material_stage_id`),
  UNIQUE INDEX `item_stage_unique` (`item_id` ASC, `stage` ASC),
  CONSTRAINT `fk_MaterialStages_Items`
    FOREIGN KEY (`item_id`)
    REFERENCES `PriceListItems` (`item_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
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
  `address` VARCHAR(255) NULL,
  `city` VARCHAR(50) NULL,
  `state` VARCHAR(2) NULL,
  `zip` VARCHAR(10) NULL,
  `square_footage` INT NULL,
  `num_floors` INT NULL,
  `status` ENUM('Draft', 'Sent', 'Approved', 'Rejected', 'Expired', 'Converted') NOT NULL DEFAULT 'Draft',
  `labor_rate` DECIMAL(10,2) NOT NULL DEFAULT 85.00,
  `material_markup` DECIMAL(5,2) NOT NULL DEFAULT 22.00,
  `total_labor_hours` DECIMAL(10,2) NULL,
  `total_material_cost` DECIMAL(10,2) NULL,
  `total_cost` DECIMAL(10,2) NULL,
  `notes` TEXT NULL,
  `created_date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `sent_date` DATETIME NULL,
  `expiry_date` DATE NULL,
  `created_by` VARCHAR(50) NULL,
  `converted_to_job_id` INT NULL,
  `converted_date` DATETIME NULL,
  PRIMARY KEY (`estimate_id`),
  UNIQUE INDEX `estimate_version_unique` (`estimate_number` ASC, `version` ASC),
  CONSTRAINT `fk_Estimates_Customers`
    FOREIGN KEY (`customer_id`)
    REFERENCES `Customers` (`customer_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Estimates_Jobs`
    FOREIGN KEY (`converted_to_job_id`)
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
  `line_item_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
  `room_id` INT NOT NULL,
  `item_id` INT NOT NULL,
  `quantity` INT NOT NULL DEFAULT 1,
  `item_code` VARCHAR(20) NOT NULL,
  `description` VARCHAR(255) NOT NULL,
  `unit_price` DECIMAL(10,2) NOT NULL,
  `total_price` DECIMAL(10,2) GENERATED ALWAYS AS (quantity * unit_price) STORED,
  `line_order` INT NOT NULL DEFAULT 0,
  `notes` TEXT NULL,
  PRIMARY KEY (`line_item_id`),
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
  CONSTRAINT `fk_EstimateLineItems_Items`
    FOREIGN KEY (`item_id`)
    REFERENCES `PriceListItems` (`item_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `RoomTemplates`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `RoomTemplates` (
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
CREATE TABLE IF NOT EXISTS `RoomTemplateItems` (
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
    REFERENCES `PriceListItems` (`item_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `EstimateStageSummary`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `EstimateStageSummary` (
  `summary_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
  `stage` ENUM('Demo', 'Rough', 'Service', 'Finish', 'Extra', 'Temp Service', 'Inspection', 'Other') NOT NULL,
  `estimated_hours` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `estimated_material` DECIMAL(10,2) NOT NULL DEFAULT 0,
  PRIMARY KEY (`summary_id`),
  UNIQUE INDEX `estimate_stage_unique` (`estimate_id` ASC, `stage` ASC),
  CONSTRAINT `fk_EstimateStageSummary_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Add estimate_id to Jobs table for linking
-- -----------------------------------------------------
ALTER TABLE `Jobs` 
  ADD COLUMN `estimate_id` INT NULL AFTER `notes`,
  ADD CONSTRAINT `fk_Jobs_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION;

-- -----------------------------------------------------
-- Sample Price List Items (matching your Excel codes)
-- -----------------------------------------------------
INSERT INTO `PriceListItems` (`item_code`, `name`, `base_price`, `category`) VALUES 
('fridge', 'Refrigerator receptacle', 68.73, 'Kitchen'),
('micro', 'Microwave receptacle', 68.73, 'Kitchen'),
('dw', 'Dishwasher receptacle', 68.73, 'Kitchen'),
('hood', 'Hood receptacle', 68.73, 'Kitchen'),
('oven', '240 volt 60 amp electric oven 50\' with gfci protection as per 2020 code change', 415.63, 'Kitchen'),
('cook', '240 volt 30 amp cooktop 50\'', 267.21, 'Kitchen'),
('hh', '4" LED recessed light (high hat)', 82.59, 'Lighting'),
('pend', 'Owner supplied pendant light', 81.36, 'Lighting'),
('O', 'Decora Outlet', 68.73, 'General'),
('S', 'Single Pole Decora Switch', 68.17, 'General'),
('3W', '3-way Decora Switch', 69.12, 'General'),
('Dim', 'Diva style dimmer switch', 84.18, 'General'),
('Gfi', '15a TP GFI', 83.74, 'General'),
('ARL', 'Outdoor gfi receptacle with weather proof cover', 103.87, 'Exterior'),
('Sc', 'Owner supplied wall sconce', 81.91, 'Lighting'),
('Van', 'Owner supplied vanity light', 81.36, 'Lighting'),
('Ex-l', 'Panasonic 110cfm ex Fan/Light', 0, 'Ventilation'),
('yellow', '12/2 wire', 112.00, 'Wire'),
('3yellow', '12/3 wire', 164.63, 'Wire'),
('white', '14/2 wire', 83.13, 'Wire'),
('3white', '14/3 wire', 108.93, 'Wire'),
('Plugmold', 'Plugmold', 195.00, 'Kitchen'),
('tape', 'Under cabinet LED tape lighting 15\'', 325.00, 'Lighting')
ON DUPLICATE KEY UPDATE 
  name = VALUES(name),
  base_price = VALUES(base_price),
  category = VALUES(category);

-- -----------------------------------------------------
-- Sample Labor Minutes for items
-- -----------------------------------------------------
INSERT INTO `LaborMinutes` (`item_id`, `stage`, `minutes`) 
SELECT p.item_id, 'Rough', 30 FROM PriceListItems p WHERE p.item_code IN ('fridge', 'micro', 'dw', 'hood')
ON DUPLICATE KEY UPDATE minutes = VALUES(minutes);

INSERT INTO `LaborMinutes` (`item_id`, `stage`, `minutes`) 
SELECT p.item_id, 'Finish', 15 FROM PriceListItems p WHERE p.item_code IN ('fridge', 'micro', 'dw', 'hood')
ON DUPLICATE KEY UPDATE minutes = VALUES(minutes);

INSERT INTO `LaborMinutes` (`item_id`, `stage`, `minutes`) 
SELECT p.item_id, 'Rough', 90 FROM PriceListItems p WHERE p.item_code = 'oven'
ON DUPLICATE KEY UPDATE minutes = VALUES(minutes);

INSERT INTO `LaborMinutes` (`item_id`, `stage`, `minutes`) 
SELECT p.item_id, 'Finish', 30 FROM PriceListItems p WHERE p.item_code = 'oven'
ON DUPLICATE KEY UPDATE minutes = VALUES(minutes);

INSERT INTO `LaborMinutes` (`item_id`, `stage`, `minutes`) 
SELECT p.item_id, 'Rough', 45 FROM PriceListItems p WHERE p.item_code = 'hh'
ON DUPLICATE KEY UPDATE minutes = VALUES(minutes);

INSERT INTO `LaborMinutes` (`item_id`, `stage`, `minutes`) 
SELECT p.item_id, 'Finish', 20 FROM PriceListItems p WHERE p.item_code = 'hh'
ON DUPLICATE KEY UPDATE minutes = VALUES(minutes);

-- -----------------------------------------------------
-- Sample Room Templates
-- -----------------------------------------------------
INSERT INTO `RoomTemplates` (`template_name`, `room_type`, `description`) VALUES 
('Standard Kitchen', 'Kitchen', 'Typical kitchen with standard appliances'),
('Master Bedroom', 'Bedroom', 'Master bedroom with ceiling fan and multiple outlets'),
('Standard Bathroom', 'Bathroom', 'Bathroom with vanity light and exhaust fan'),
('Living Room', 'Living Room', 'Living room with recessed lighting'),
('Standard Bedroom', 'Bedroom', 'Regular bedroom with basic electrical'),
('Half Bath', 'Bathroom', 'Powder room with basic electrical')
ON DUPLICATE KEY UPDATE description = VALUES(description);

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
FROM PriceListItems 
WHERE item_code IN ('fridge', 'micro', 'dw', 'hood', 'oven', 'hh', 'O', 'Gfi', 'S', '3W')
ON DUPLICATE KEY UPDATE quantity = VALUES(quantity);

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
  e.total_cost,
  e.status,
  e.created_date,
  CASE 
    WHEN e.converted_to_job_id IS NOT NULL THEN 'Already Converted'
    WHEN e.status = 'Approved' THEN 'Ready to Convert'
    ELSE 'Not Ready'
  END as conversion_status
FROM Estimates e
JOIN Customers c ON e.customer_id = c.customer_id
WHERE e.status IN ('Approved', 'Converted')
ORDER BY e.created_date DESC;