-- Migration script to merge estimating tables into existing electrical_contractor_db
-- Run this script on your existing electrical_contractor_db database

-- First, add the estimate_id column to Jobs table if it doesn't exist
ALTER TABLE `Jobs` 
ADD COLUMN IF NOT EXISTS `estimate_id` INT NULL AFTER `notes`,
ADD INDEX IF NOT EXISTS `fk_Jobs_Estimates_idx` (`estimate_id` ASC);

-- -----------------------------------------------------
-- Create Estimating Tables
-- -----------------------------------------------------

-- Table `Estimates`
CREATE TABLE IF NOT EXISTS `Estimates` (
  `estimate_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_number` VARCHAR(20) NOT NULL,
  `customer_id` INT NOT NULL,
  `job_name` VARCHAR(100) NOT NULL,
  `job_address` VARCHAR(255) NULL,
  `job_city` VARCHAR(50) NULL,
  `job_state` VARCHAR(2) NULL,
  `job_zip` VARCHAR(10) NULL,
  `square_footage` INT NULL,
  `num_floors` INT NULL,
  `status` ENUM('Draft', 'Sent', 'Approved', 'Rejected', 'Expired', 'Converted') NOT NULL DEFAULT 'Draft',
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `expiration_date` DATE NULL,
  `notes` TEXT NULL,
  `tax_rate` DECIMAL(5,3) NOT NULL DEFAULT 0.064,
  `material_markup` DECIMAL(5,2) NOT NULL DEFAULT 22.00,
  `labor_rate` DECIMAL(10,2) NOT NULL DEFAULT 75.00,
  `total_material_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `total_labor_minutes` INT NOT NULL DEFAULT 0,
  `total_price` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `job_id` INT NULL,
  PRIMARY KEY (`estimate_id`),
  UNIQUE INDEX `estimate_number_UNIQUE` (`estimate_number` ASC),
  INDEX `fk_Estimates_Customers_idx` (`customer_id` ASC),
  INDEX `fk_Estimates_Jobs_idx` (`job_id` ASC),
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

-- Table `EstimateRooms`
CREATE TABLE IF NOT EXISTS `EstimateRooms` (
  `room_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
  `room_name` VARCHAR(100) NOT NULL,
  `room_order` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`room_id`),
  INDEX `fk_EstimateRooms_Estimates_idx` (`estimate_id` ASC),
  CONSTRAINT `fk_EstimateRooms_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- Table `EstimateItems`
CREATE TABLE IF NOT EXISTS `EstimateItems` (
  `item_id` INT NOT NULL AUTO_INCREMENT,
  `room_id` INT NOT NULL,
  `item_code` VARCHAR(20) NULL,
  `item_name` VARCHAR(200) NOT NULL,
  `quantity` INT NOT NULL DEFAULT 1,
  `unit_price` DECIMAL(10,2) NOT NULL,
  `material_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `labor_minutes` INT NOT NULL DEFAULT 0,
  `total_price` DECIMAL(10,2) NOT NULL,
  `line_order` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`item_id`),
  INDEX `fk_EstimateItems_EstimateRooms_idx` (`room_id` ASC),
  INDEX `fk_EstimateItems_PriceList_idx` (`item_code` ASC),
  CONSTRAINT `fk_EstimateItems_EstimateRooms`
    FOREIGN KEY (`room_id`)
    REFERENCES `EstimateRooms` (`room_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_EstimateItems_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
);

-- Table `EstimateStageSummary`
CREATE TABLE IF NOT EXISTS `EstimateStageSummary` (
  `summary_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
  `stage_name` ENUM('Demo', 'Rough', 'Service', 'Finish', 'Extra', 'Temp Service', 'Inspection', 'Other') NOT NULL,
  `total_labor_minutes` INT NOT NULL DEFAULT 0,
  `total_labor_hours` DECIMAL(8,2) NOT NULL DEFAULT 0,
  `total_material_cost` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `stage_order` INT NOT NULL,
  PRIMARY KEY (`summary_id`),
  INDEX `fk_EstimateStageSummary_Estimates_idx` (`estimate_id` ASC),
  CONSTRAINT `fk_EstimateStageSummary_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- Table `RoomTemplates`
CREATE TABLE IF NOT EXISTS `RoomTemplates` (
  `template_id` INT NOT NULL AUTO_INCREMENT,
  `template_name` VARCHAR(100) NOT NULL,
  `description` TEXT NULL,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  PRIMARY KEY (`template_id`)
);

-- Table `RoomTemplateItems`
CREATE TABLE IF NOT EXISTS `RoomTemplateItems` (
  `template_item_id` INT NOT NULL AUTO_INCREMENT,
  `template_id` INT NOT NULL,
  `item_code` VARCHAR(20) NOT NULL,
  `default_quantity` INT NOT NULL DEFAULT 1,
  `display_order` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`template_item_id`),
  INDEX `fk_RoomTemplateItems_RoomTemplates_idx` (`template_id` ASC),
  INDEX `fk_RoomTemplateItems_PriceList_idx` (`item_code` ASC),
  CONSTRAINT `fk_RoomTemplateItems_RoomTemplates`
    FOREIGN KEY (`template_id`)
    REFERENCES `RoomTemplates` (`template_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_RoomTemplateItems_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

-- -----------------------------------------------------
-- Update PriceList table to ensure it has required columns
-- -----------------------------------------------------
ALTER TABLE `PriceList`
ADD COLUMN IF NOT EXISTS `markup_percentage` DECIMAL(5,2) NULL DEFAULT 22 AFTER `labor_minutes`,
MODIFY COLUMN `tax_rate` DECIMAL(5,3) NULL DEFAULT 0.064;

-- -----------------------------------------------------
-- Add foreign key constraint from Jobs to Estimates
-- -----------------------------------------------------
ALTER TABLE `Jobs` 
ADD CONSTRAINT IF NOT EXISTS `fk_Jobs_Estimates`
  FOREIGN KEY (`estimate_id`)
  REFERENCES `Estimates` (`estimate_id`)
  ON DELETE SET NULL
  ON UPDATE NO ACTION;

-- -----------------------------------------------------
-- Add sample price list items if they don't exist
-- -----------------------------------------------------
INSERT IGNORE INTO `PriceList` (`category`, `item_code`, `name`, `description`, `base_cost`, `labor_minutes`) VALUES
('Lighting', 'hh', '4" LED recessed light (high hat)', 'LED recessed lighting fixture', 62.00, 20),
('Devices', 'O', 'Decora Outlet', 'Standard duplex receptacle', 45.00, 15),
('Devices', 'S', 'Single Pole Decora Switch', '15A single pole switch', 48.00, 15),
('Devices', '3W', '3-way Decora Switch', '15A 3-way switch', 52.00, 20),
('Devices', 'Gfi', '15a TP GFI', 'GFCI protected outlet', 72.00, 20),
('Devices', 'ARL', 'Outdoor gfi receptacle with weather proof cover', 'Arlington weatherproof GFCI', 95.00, 30),
('Devices', 'fridge', 'Refrigerator receptacle', '20A dedicated circuit', 85.00, 25),
('Lighting', 'Sc', 'Wall sconce', 'Owner supplied wall sconce installation', 65.00, 25),
('Lighting', 'Van', 'Vanity light', 'Owner supplied vanity light installation', 65.00, 25),
('Exhaust', 'Ex-l', 'Panasonic 110cfm ex Fan/Light', 'Bathroom exhaust fan with light', 125.00, 45),
('Wire', '12/2', '12/2 Romex Wire', 'Per 100ft', 95.00, 0),
('Wire', '14/2', '14/2 Romex Wire', 'Per 100ft', 65.00, 0),
('Wire', '12/3', '12/3 Romex Wire', 'Per 100ft', 125.00, 0),
('Wire', '14/3', '14/3 Romex Wire', 'Per 100ft', 95.00, 0);

-- -----------------------------------------------------
-- Add sample room templates
-- -----------------------------------------------------
INSERT IGNORE INTO `RoomTemplates` (`template_name`, `description`) VALUES
('Standard Bedroom', 'Typical bedroom electrical layout'),
('Standard Bathroom', 'Typical bathroom with GFCI and exhaust'),
('Kitchen Basic', 'Basic kitchen electrical requirements'),
('Living Room', 'Standard living room configuration');

-- Add items to bedroom template (check if template exists first)
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`)
SELECT rt.template_id, 'O', 4, 1 
FROM `RoomTemplates` rt 
WHERE rt.template_name = 'Standard Bedroom'
  AND NOT EXISTS (
    SELECT 1 FROM `RoomTemplateItems` rti 
    WHERE rti.template_id = rt.template_id AND rti.item_code = 'O'
  );

INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`)
SELECT rt.template_id, 'S', 1, 2 
FROM `RoomTemplates` rt 
WHERE rt.template_name = 'Standard Bedroom'
  AND NOT EXISTS (
    SELECT 1 FROM `RoomTemplateItems` rti 
    WHERE rti.template_id = rt.template_id AND rti.item_code = 'S'
  );

INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`)
SELECT rt.template_id, 'hh', 1, 3 
FROM `RoomTemplates` rt 
WHERE rt.template_name = 'Standard Bedroom'
  AND NOT EXISTS (
    SELECT 1 FROM `RoomTemplateItems` rti 
    WHERE rti.template_id = rt.template_id AND rti.item_code = 'hh'
  );

-- Add items to bathroom template
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`)
SELECT rt.template_id, 'Gfi', 1, 1 
FROM `RoomTemplates` rt 
WHERE rt.template_name = 'Standard Bathroom'
  AND NOT EXISTS (
    SELECT 1 FROM `RoomTemplateItems` rti 
    WHERE rti.template_id = rt.template_id AND rti.item_code = 'Gfi'
  );

INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`)
SELECT rt.template_id, 'Van', 1, 2 
FROM `RoomTemplates` rt 
WHERE rt.template_name = 'Standard Bathroom'
  AND NOT EXISTS (
    SELECT 1 FROM `RoomTemplateItems` rti 
    WHERE rti.template_id = rt.template_id AND rti.item_code = 'Van'
  );

INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`)
SELECT rt.template_id, 'Ex-l', 1, 3 
FROM `RoomTemplates` rt 
WHERE rt.template_name = 'Standard Bathroom'
  AND NOT EXISTS (
    SELECT 1 FROM `RoomTemplateItems` rti 
    WHERE rti.template_id = rt.template_id AND rti.item_code = 'Ex-l'
  );

INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`)
SELECT rt.template_id, 'S', 2, 4 
FROM `RoomTemplates` rt 
WHERE rt.template_name = 'Standard Bathroom'
  AND NOT EXISTS (
    SELECT 1 FROM `RoomTemplateItems` rti 
    WHERE rti.template_id = rt.template_id AND rti.item_code = 'S'
  );

-- -----------------------------------------------------
-- Report successful completion
-- -----------------------------------------------------
SELECT 'Database migration completed successfully!' AS Result;
SELECT 'The following tables have been added:' AS Info;
SELECT table_name FROM information_schema.tables 
WHERE table_schema = DATABASE() 
  AND table_name IN ('Estimates', 'EstimateRooms', 'EstimateItems', 'EstimateStageSummary', 'RoomTemplates', 'RoomTemplateItems')
ORDER BY table_name;
