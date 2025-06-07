-- MySQL Database Schema for Electrical Contractor System
-- Integrated Database with Job Tracking and Estimating
-- Database name: electrical_contractor_db

-- -----------------------------------------------------
-- Table `Customers`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Customers` (
  `customer_id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  `address` VARCHAR(255) NULL,
  `city` VARCHAR(50) NULL,
  `state` VARCHAR(2) NULL,
  `zip` VARCHAR(10) NULL,
  `email` VARCHAR(100) NULL,
  `phone` VARCHAR(20) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`customer_id`)
);

-- -----------------------------------------------------
-- Table `Jobs`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Jobs` (
  `job_id` INT NOT NULL AUTO_INCREMENT,
  `job_number` VARCHAR(20) NOT NULL,
  `customer_id` INT NOT NULL,
  `job_name` VARCHAR(100) NOT NULL,
  `address` VARCHAR(255) NULL,
  `city` VARCHAR(50) NULL,
  `state` VARCHAR(2) NULL,
  `zip` VARCHAR(10) NULL,
  `square_footage` INT NULL,
  `num_floors` INT NULL,
  `status` ENUM('Estimate', 'In Progress', 'Complete') NOT NULL DEFAULT 'Estimate',
  `create_date` DATE NOT NULL,
  `completion_date` DATE NULL,
  `total_estimate` DECIMAL(10,2) NULL,
  `total_actual` DECIMAL(10,2) NULL,
  `notes` TEXT NULL,
  `estimate_id` INT NULL,
  PRIMARY KEY (`job_id`),
  UNIQUE INDEX `job_number_UNIQUE` (`job_number` ASC),
  INDEX `fk_Jobs_Estimates_idx` (`estimate_id` ASC),
  CONSTRAINT `fk_Jobs_Customers`
    FOREIGN KEY (`customer_id`)
    REFERENCES `Customers` (`customer_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `Employees`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Employees` (
  `employee_id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(50) NOT NULL,
  `hourly_rate` DECIMAL(10,2) NOT NULL,
  `burden_rate` DECIMAL(10,2) NULL,
  `status` ENUM('Active', 'Inactive') NOT NULL DEFAULT 'Active',
  `notes` TEXT NULL,
  PRIMARY KEY (`employee_id`)
);

-- -----------------------------------------------------
-- Table `Vendors`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Vendors` (
  `vendor_id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  `address` VARCHAR(255) NULL,
  `city` VARCHAR(50) NULL,
  `state` VARCHAR(2) NULL,
  `zip` VARCHAR(10) NULL,
  `phone` VARCHAR(20) NULL,
  `email` VARCHAR(100) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`vendor_id`)
);

-- -----------------------------------------------------
-- Table `JobStages`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `JobStages` (
  `stage_id` INT NOT NULL AUTO_INCREMENT,
  `job_id` INT NOT NULL,
  `stage_name` ENUM('Demo', 'Rough', 'Service', 'Finish', 'Extra', 'Temp Service', 'Inspection', 'Other') NOT NULL,
  `estimated_hours` DECIMAL(8,2) NULL DEFAULT 0,
  `estimated_material_cost` DECIMAL(10,2) NULL DEFAULT 0,
  `actual_hours` DECIMAL(8,2) NULL DEFAULT 0,
  `actual_material_cost` DECIMAL(10,2) NULL DEFAULT 0,
  `notes` TEXT NULL,
  PRIMARY KEY (`stage_id`),
  CONSTRAINT `fk_JobStages_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `LaborEntries`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `LaborEntries` (
  `entry_id` INT NOT NULL AUTO_INCREMENT,
  `job_id` INT NOT NULL,
  `employee_id` INT NOT NULL,
  `stage_id` INT NOT NULL,
  `date` DATE NOT NULL,
  `hours` DECIMAL(5,2) NOT NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`entry_id`),
  CONSTRAINT `fk_LaborEntries_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_LaborEntries_Employees`
    FOREIGN KEY (`employee_id`)
    REFERENCES `Employees` (`employee_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_LaborEntries_JobStages`
    FOREIGN KEY (`stage_id`)
    REFERENCES `JobStages` (`stage_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `MaterialEntries`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `MaterialEntries` (
  `entry_id` INT NOT NULL AUTO_INCREMENT,
  `job_id` INT NOT NULL,
  `stage_id` INT NOT NULL,
  `vendor_id` INT NOT NULL,
  `date` DATE NOT NULL,
  `cost` DECIMAL(10,2) NOT NULL,
  `invoice_number` VARCHAR(50) NULL,
  `invoice_total` DECIMAL(10,2) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`entry_id`),
  CONSTRAINT `fk_MaterialEntries_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_MaterialEntries_JobStages`
    FOREIGN KEY (`stage_id`)
    REFERENCES `JobStages` (`stage_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_MaterialEntries_Vendors`
    FOREIGN KEY (`vendor_id`)
    REFERENCES `Vendors` (`vendor_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `PriceList`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `PriceList` (
  `item_id` INT NOT NULL AUTO_INCREMENT,
  `category` VARCHAR(50) NOT NULL,
  `item_code` VARCHAR(20) NOT NULL,
  `name` VARCHAR(100) NOT NULL,
  `description` TEXT NULL,
  `base_cost` DECIMAL(10,2) NOT NULL,
  `tax_rate` DECIMAL(5,3) NULL DEFAULT 0.064,
  `labor_minutes` INT NULL DEFAULT 0,
  `markup_percentage` DECIMAL(5,2) NULL DEFAULT 22,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  `notes` TEXT NULL,
  PRIMARY KEY (`item_id`),
  UNIQUE INDEX `item_code_UNIQUE` (`item_code` ASC)
);

-- -----------------------------------------------------
-- Table `RoomSpecifications`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `RoomSpecifications` (
  `spec_id` INT NOT NULL AUTO_INCREMENT,
  `job_id` INT NOT NULL,
  `room_name` VARCHAR(50) NOT NULL,
  `item_description` TEXT NOT NULL,
  `quantity` INT NOT NULL DEFAULT 1,
  `item_code` VARCHAR(20) NULL,
  `unit_price` DECIMAL(10,2) NOT NULL,
  `total_price` DECIMAL(10,2) NOT NULL,
  PRIMARY KEY (`spec_id`),
  CONSTRAINT `fk_RoomSpecifications_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_RoomSpecifications_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
);

-- -----------------------------------------------------
-- Table `PermitItems`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `PermitItems` (
  `permit_id` INT NOT NULL AUTO_INCREMENT,
  `job_id` INT NOT NULL,
  `category` VARCHAR(50) NOT NULL,
  `quantity` INT NOT NULL DEFAULT 0,
  `description` VARCHAR(255) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`permit_id`),
  CONSTRAINT `fk_PermitItems_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- ESTIMATING TABLES
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Table `Estimates`
-- -----------------------------------------------------
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

-- -----------------------------------------------------
-- Table `EstimateRooms`
-- -----------------------------------------------------
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

-- -----------------------------------------------------
-- Table `EstimateItems`
-- -----------------------------------------------------
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

-- -----------------------------------------------------
-- Table `EstimateStageSummary`
-- -----------------------------------------------------
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

-- -----------------------------------------------------
-- Table `RoomTemplates`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `RoomTemplates` (
  `template_id` INT NOT NULL AUTO_INCREMENT,
  `template_name` VARCHAR(100) NOT NULL,
  `description` TEXT NULL,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  PRIMARY KEY (`template_id`)
);

-- -----------------------------------------------------
-- Table `RoomTemplateItems`
-- -----------------------------------------------------
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
-- Add foreign key constraint from Jobs to Estimates
-- -----------------------------------------------------
ALTER TABLE `Jobs` 
ADD CONSTRAINT `fk_Jobs_Estimates`
  FOREIGN KEY (`estimate_id`)
  REFERENCES `Estimates` (`estimate_id`)
  ON DELETE SET NULL
  ON UPDATE NO ACTION;

-- -----------------------------------------------------
-- Add initial data (sample employees)
-- -----------------------------------------------------
INSERT INTO `Employees` (`name`, `hourly_rate`, `status`) VALUES 
('Erik', 85.00, 'Active'),
('Lee', 65.00, 'Active'),
('Carlos', 65.00, 'Active'),
('Jake', 65.00, 'Active'),
('Trevor', 65.00, 'Active'),
('Ryan', 65.00, 'Active');

-- -----------------------------------------------------
-- Add initial data (sample vendors)
-- -----------------------------------------------------
INSERT INTO `Vendors` (`name`) VALUES 
('Home Depot'),
('Cooper'),
('Warshauer'),
('Good Friend Electric'),
('Lowes');

-- -----------------------------------------------------
-- Add sample price list items with your common codes
-- -----------------------------------------------------
INSERT INTO `PriceList` (`category`, `item_code`, `name`, `description`, `base_cost`, `labor_minutes`) VALUES
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
INSERT INTO `RoomTemplates` (`template_name`, `description`) VALUES
('Standard Bedroom', 'Typical bedroom electrical layout'),
('Standard Bathroom', 'Typical bathroom with GFCI and exhaust'),
('Kitchen Basic', 'Basic kitchen electrical requirements'),
('Living Room', 'Standard living room configuration');

-- Add items to bedroom template
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`) 
SELECT 1, 'O', 4, 1 WHERE EXISTS (SELECT 1 FROM `RoomTemplates` WHERE `template_id` = 1);
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`) 
SELECT 1, 'S', 1, 2 WHERE EXISTS (SELECT 1 FROM `RoomTemplates` WHERE `template_id` = 1);
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`) 
SELECT 1, 'hh', 1, 3 WHERE EXISTS (SELECT 1 FROM `RoomTemplates` WHERE `template_id` = 1);

-- Add items to bathroom template
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`) 
SELECT 2, 'Gfi', 1, 1 WHERE EXISTS (SELECT 1 FROM `RoomTemplates` WHERE `template_id` = 2);
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`) 
SELECT 2, 'Van', 1, 2 WHERE EXISTS (SELECT 1 FROM `RoomTemplates` WHERE `template_id` = 2);
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`) 
SELECT 2, 'Ex-l', 1, 3 WHERE EXISTS (SELECT 1 FROM `RoomTemplates` WHERE `template_id` = 2);
INSERT INTO `RoomTemplateItems` (`template_id`, `item_code`, `default_quantity`, `display_order`) 
SELECT 2, 'S', 2, 4 WHERE EXISTS (SELECT 1 FROM `RoomTemplates` WHERE `template_id` = 2);
