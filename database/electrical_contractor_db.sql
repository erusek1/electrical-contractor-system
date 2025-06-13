-- MySQL Database Schema for Electrical Contractor System
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
  PRIMARY KEY (`job_id`),
  UNIQUE INDEX `job_number_UNIQUE` (`job_number` ASC),
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
  `vehicle_cost_per_hour` DECIMAL(10,2) NULL,
  `vehicle_cost_per_month` DECIMAL(10,2) NULL,
  `overhead_percentage` DECIMAL(5,2) NULL,
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
  `tax_rate` DECIMAL(5,3) NULL DEFAULT 0,
  `labor_minutes` INT NULL DEFAULT 0,
  `markup_percentage` DECIMAL(5,2) NULL DEFAULT 0,
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
-- Add initial data (sample employees)
-- -----------------------------------------------------
INSERT INTO `Employees` (`name`, `hourly_rate`, `burden_rate`, `vehicle_cost_per_hour`, `vehicle_cost_per_month`, `overhead_percentage`, `status`) VALUES 
('Erik', 85.00, 0, 0, 0, 0, 'Active'),
('Lee', 65.00, 0, 0, 0, 0, 'Active'),
('Carlos', 65.00, 0, 0, 0, 0, 'Active'),
('Jake', 65.00, 0, 0, 0, 0, 'Active'),
('Trevor', 65.00, 0, 0, 0, 0, 'Active'),
('Ryan', 65.00, 0, 0, 0, 0, 'Active');

-- -----------------------------------------------------
-- Add initial data (sample vendors)
-- -----------------------------------------------------
INSERT INTO `Vendors` (`name`) VALUES 
('Home Depot'),
('Cooper'),
('Warshauer'),
('Good Friend Electric'),
('Lowes');