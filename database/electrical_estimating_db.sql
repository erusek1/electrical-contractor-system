-- MySQL Database Schema for Electrical Contractor Estimating System
-- Database name: electrical_estimating_db

-- -----------------------------------------------------
-- Table `PriceListItems`
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
  `created_date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`customer_id`),
  INDEX `idx_name` (`name` ASC)
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
  `status` ENUM('Draft', 'Sent', 'Approved', 'Rejected', 'Expired') NOT NULL DEFAULT 'Draft',
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
  PRIMARY KEY (`estimate_id`),
  UNIQUE INDEX `estimate_version_unique` (`estimate_number` ASC, `version` ASC),
  CONSTRAINT `fk_Estimates_Customers`
    FOREIGN KEY (`customer_id`)
    REFERENCES `Customers` (`customer_id`)
    ON DELETE NO ACTION
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
-- Table `PermitItemTypes`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `PermitItemTypes` (
  `permit_type_id` INT NOT NULL AUTO_INCREMENT,
  `category` VARCHAR(50) NOT NULL,
  `description` VARCHAR(255) NOT NULL,
  `unit` VARCHAR(20) NOT NULL DEFAULT 'each',
  `notes` TEXT NULL,
  PRIMARY KEY (`permit_type_id`)
);

-- -----------------------------------------------------
-- Table `EstimatePermitItems`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `EstimatePermitItems` (
  `permit_item_id` INT NOT NULL AUTO_INCREMENT,
  `estimate_id` INT NOT NULL,
  `permit_type_id` INT NOT NULL,
  `quantity` INT NOT NULL DEFAULT 0,
  `notes` TEXT NULL,
  PRIMARY KEY (`permit_item_id`),
  CONSTRAINT `fk_EstimatePermitItems_Estimates`
    FOREIGN KEY (`estimate_id`)
    REFERENCES `Estimates` (`estimate_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_EstimatePermitItems_Types`
    FOREIGN KEY (`permit_type_id`)
    REFERENCES `PermitItemTypes` (`permit_type_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Initial Data - Common Permit Item Types
-- -----------------------------------------------------
INSERT INTO `PermitItemTypes` (`category`, `description`) VALUES 
('Switches', 'Total number of switches'),
('Receptacles', 'Total number of receptacles'),
('Lights', 'Total number of light fixtures'),
('Fans', 'Total number of fans'),
('240V Circuits', 'Total number of 240V circuits'),
('GFCI', 'Total number of GFCI devices'),
('AFCI', 'Total number of AFCI breakers'),
('Panels', 'Total number of panels'),
('Sub Panels', 'Total number of sub panels'),
('Smoke Detectors', 'Total number of smoke detectors'),
('CO Detectors', 'Total number of CO detectors');

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
('3white', '14/3 wire', 108.93, 'Wire');

-- -----------------------------------------------------
-- Sample Labor Minutes for items (matching your Price List columns)
-- -----------------------------------------------------
INSERT INTO `LaborMinutes` (`item_id`, `stage`, `minutes`) VALUES 
(1, 'Rough', 30), (1, 'Finish', 15),  -- fridge
(2, 'Rough', 30), (2, 'Finish', 15),  -- micro
(3, 'Rough', 30), (3, 'Finish', 15),  -- dw
(4, 'Rough', 30), (4, 'Finish', 15),  -- hood
(5, 'Rough', 90), (5, 'Finish', 30),  -- oven
(6, 'Rough', 60), (6, 'Finish', 30),  -- cook
(7, 'Rough', 45), (7, 'Finish', 20),  -- hh
(8, 'Rough', 30), (8, 'Finish', 30),  -- pend
(9, 'Rough', 20), (9, 'Finish', 10),  -- O
(10, 'Rough', 20), (10, 'Finish', 10), -- S
(11, 'Rough', 30), (11, 'Finish', 15), -- 3W
(12, 'Rough', 30), (12, 'Finish', 20); -- Dim

-- -----------------------------------------------------
-- Sample Room Templates
-- -----------------------------------------------------
INSERT INTO `RoomTemplates` (`template_name`, `room_type`, `description`) VALUES 
('Standard Kitchen', 'Kitchen', 'Typical kitchen with standard appliances'),
('Master Bedroom', 'Bedroom', 'Master bedroom with ceiling fan and multiple outlets'),
('Standard Bathroom', 'Bathroom', 'Bathroom with vanity light and exhaust fan'),
('Living Room', 'Living Room', 'Living room with recessed lighting');

-- -----------------------------------------------------
-- Views for reporting
-- -----------------------------------------------------
CREATE VIEW `v_estimate_totals` AS
SELECT 
  e.estimate_id,
  e.estimate_number,
  e.version,
  c.name as customer_name,
  e.job_name,
  e.status,
  COUNT(DISTINCT er.room_id) as room_count,
  COUNT(eli.line_item_id) as item_count,
  SUM(eli.total_price) as items_total,
  e.total_labor_hours,
  e.total_material_cost,
  e.total_cost,
  e.created_date
FROM Estimates e
JOIN Customers c ON e.customer_id = c.customer_id
LEFT JOIN EstimateRooms er ON e.estimate_id = er.estimate_id
LEFT JOIN EstimateLineItems eli ON e.estimate_id = eli.estimate_id
GROUP BY e.estimate_id;

CREATE VIEW `v_room_item_details` AS
SELECT 
  er.estimate_id,
  er.room_id,
  er.room_name,
  eli.quantity,
  eli.item_code,
  eli.description,
  eli.unit_price,
  eli.total_price,
  pli.category as item_category
FROM EstimateRooms er
JOIN EstimateLineItems eli ON er.room_id = eli.room_id
LEFT JOIN PriceListItems pli ON eli.item_id = pli.item_id
ORDER BY er.room_order, eli.line_order;