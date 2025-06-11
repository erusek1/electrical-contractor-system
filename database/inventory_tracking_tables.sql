-- Inventory Tracking System Tables
-- These tables integrate with the existing electrical contractor database
-- to provide inventory management across multiple locations

-- -----------------------------------------------------
-- Table `StorageLocations`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `StorageLocations` (
  `location_id` INT NOT NULL AUTO_INCREMENT,
  `location_name` VARCHAR(100) NOT NULL,
  `location_type` ENUM('Warehouse', 'Van', 'Storage', 'Outdoor', 'Other') NOT NULL,
  `parent_location_id` INT NULL,
  `address` VARCHAR(255) NULL,
  `description` TEXT NULL,
  `is_mobile` BOOLEAN NOT NULL DEFAULT FALSE,
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`location_id`),
  INDEX `fk_StorageLocations_Parent_idx` (`parent_location_id` ASC),
  CONSTRAINT `fk_StorageLocations_Parent`
    FOREIGN KEY (`parent_location_id`)
    REFERENCES `StorageLocations` (`location_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `StorageSubLocations`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `StorageSubLocations` (
  `sublocation_id` INT NOT NULL AUTO_INCREMENT,
  `location_id` INT NOT NULL,
  `sublocation_name` VARCHAR(100) NOT NULL,
  `sublocation_type` VARCHAR(50) NULL, -- e.g., 'Bin', 'Rack', 'Shelf', 'Case'
  `position` VARCHAR(50) NULL, -- e.g., 'A1', 'Top Shelf', 'Front Bin'
  `max_capacity` DECIMAL(10,2) NULL,
  `capacity_unit` VARCHAR(20) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`sublocation_id`),
  INDEX `fk_StorageSubLocations_Location_idx` (`location_id` ASC),
  CONSTRAINT `fk_StorageSubLocations_Location`
    FOREIGN KEY (`location_id`)
    REFERENCES `StorageLocations` (`location_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `VendorItems`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VendorItems` (
  `vendor_item_id` INT NOT NULL AUTO_INCREMENT,
  `vendor_id` INT NOT NULL,
  `item_code` VARCHAR(20) NOT NULL,
  `vendor_part_number` VARCHAR(100) NOT NULL,
  `vendor_description` VARCHAR(255) NULL,
  `unit_cost` DECIMAL(10,2) NULL,
  `case_quantity` INT NULL DEFAULT 1,
  `lead_time_days` INT NULL DEFAULT 0,
  `min_order_quantity` INT NULL DEFAULT 1,
  `is_preferred` BOOLEAN NOT NULL DEFAULT FALSE,
  `last_updated` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`vendor_item_id`),
  UNIQUE INDEX `vendor_item_unique` (`vendor_id` ASC, `item_code` ASC),
  INDEX `fk_VendorItems_Vendors_idx` (`vendor_id` ASC),
  INDEX `fk_VendorItems_PriceList_idx` (`item_code` ASC),
  CONSTRAINT `fk_VendorItems_Vendors`
    FOREIGN KEY (`vendor_id`)
    REFERENCES `Vendors` (`vendor_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_VendorItems_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

-- -----------------------------------------------------
-- Table `Inventory`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Inventory` (
  `inventory_id` INT NOT NULL AUTO_INCREMENT,
  `item_code` VARCHAR(20) NOT NULL,
  `location_id` INT NOT NULL,
  `sublocation_id` INT NULL,
  `quantity_on_hand` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `quantity_allocated` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `min_quantity` DECIMAL(10,2) NULL DEFAULT 0,
  `max_quantity` DECIMAL(10,2) NULL,
  `reorder_point` DECIMAL(10,2) NULL,
  `last_counted_date` DATETIME NULL,
  `last_counted_by` VARCHAR(50) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`inventory_id`),
  UNIQUE INDEX `inventory_location_unique` (`item_code` ASC, `location_id` ASC, `sublocation_id` ASC),
  INDEX `fk_Inventory_PriceList_idx` (`item_code` ASC),
  INDEX `fk_Inventory_StorageLocations_idx` (`location_id` ASC),
  INDEX `fk_Inventory_SubLocations_idx` (`sublocation_id` ASC),
  CONSTRAINT `fk_Inventory_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fk_Inventory_StorageLocations`
    FOREIGN KEY (`location_id`)
    REFERENCES `StorageLocations` (`location_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Inventory_SubLocations`
    FOREIGN KEY (`sublocation_id`)
    REFERENCES `StorageSubLocations` (`sublocation_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `InventoryTransactions`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `InventoryTransactions` (
  `transaction_id` INT NOT NULL AUTO_INCREMENT,
  `transaction_type` ENUM('Receive', 'Issue', 'Transfer', 'Adjust', 'Count', 'Return', 'Waste') NOT NULL,
  `transaction_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `item_code` VARCHAR(20) NOT NULL,
  `quantity` DECIMAL(10,2) NOT NULL,
  `from_location_id` INT NULL,
  `from_sublocation_id` INT NULL,
  `to_location_id` INT NULL,
  `to_sublocation_id` INT NULL,
  `job_id` INT NULL,
  `vendor_id` INT NULL,
  `invoice_number` VARCHAR(50) NULL,
  `unit_cost` DECIMAL(10,2) NULL,
  `created_by` VARCHAR(50) NOT NULL,
  `reason` VARCHAR(255) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`transaction_id`),
  INDEX `fk_InvTrans_PriceList_idx` (`item_code` ASC),
  INDEX `fk_InvTrans_FromLocation_idx` (`from_location_id` ASC),
  INDEX `fk_InvTrans_ToLocation_idx` (`to_location_id` ASC),
  INDEX `fk_InvTrans_Jobs_idx` (`job_id` ASC),
  INDEX `fk_InvTrans_Vendors_idx` (`vendor_id` ASC),
  INDEX `idx_transaction_date` (`transaction_date` ASC),
  CONSTRAINT `fk_InvTrans_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT `fk_InvTrans_FromLocation`
    FOREIGN KEY (`from_location_id`)
    REFERENCES `StorageLocations` (`location_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_InvTrans_ToLocation`
    FOREIGN KEY (`to_location_id`)
    REFERENCES `StorageLocations` (`location_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_InvTrans_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_InvTrans_Vendors`
    FOREIGN KEY (`vendor_id`)
    REFERENCES `Vendors` (`vendor_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `InventoryRequests`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `InventoryRequests` (
  `request_id` INT NOT NULL AUTO_INCREMENT,
  `request_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `requested_by` VARCHAR(50) NOT NULL,
  `request_type` ENUM('Job', 'Stock', 'Van', 'Other') NOT NULL,
  `job_id` INT NULL,
  `target_location_id` INT NULL,
  `status` ENUM('Pending', 'Partial', 'Fulfilled', 'Cancelled', 'Ordered') NOT NULL DEFAULT 'Pending',
  `need_by_date` DATE NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`request_id`),
  INDEX `fk_InvRequests_Jobs_idx` (`job_id` ASC),
  INDEX `fk_InvRequests_Locations_idx` (`target_location_id` ASC),
  CONSTRAINT `fk_InvRequests_Jobs`
    FOREIGN KEY (`job_id`)
    REFERENCES `Jobs` (`job_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_InvRequests_Locations`
    FOREIGN KEY (`target_location_id`)
    REFERENCES `StorageLocations` (`location_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `InventoryRequestItems`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `InventoryRequestItems` (
  `request_item_id` INT NOT NULL AUTO_INCREMENT,
  `request_id` INT NOT NULL,
  `item_code` VARCHAR(20) NOT NULL,
  `quantity_requested` DECIMAL(10,2) NOT NULL,
  `quantity_fulfilled` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `preferred_location_id` INT NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`request_item_id`),
  INDEX `fk_RequestItems_Requests_idx` (`request_id` ASC),
  INDEX `fk_RequestItems_PriceList_idx` (`item_code` ASC),
  INDEX `fk_RequestItems_Locations_idx` (`preferred_location_id` ASC),
  CONSTRAINT `fk_RequestItems_Requests`
    FOREIGN KEY (`request_id`)
    REFERENCES `InventoryRequests` (`request_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_RequestItems_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT `fk_RequestItems_Locations`
    FOREIGN KEY (`preferred_location_id`)
    REFERENCES `StorageLocations` (`location_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `PurchaseOrders`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `PurchaseOrders` (
  `po_id` INT NOT NULL AUTO_INCREMENT,
  `po_number` VARCHAR(50) NOT NULL,
  `vendor_id` INT NOT NULL,
  `order_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `requested_by` VARCHAR(50) NOT NULL,
  `status` ENUM('Draft', 'Sent', 'Partial', 'Complete', 'Cancelled') NOT NULL DEFAULT 'Draft',
  `expected_delivery_date` DATE NULL,
  `total_amount` DECIMAL(10,2) NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`po_id`),
  UNIQUE INDEX `po_number_UNIQUE` (`po_number` ASC),
  INDEX `fk_PurchaseOrders_Vendors_idx` (`vendor_id` ASC),
  CONSTRAINT `fk_PurchaseOrders_Vendors`
    FOREIGN KEY (`vendor_id`)
    REFERENCES `Vendors` (`vendor_id`)
    ON DELETE RESTRICT
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `PurchaseOrderItems`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `PurchaseOrderItems` (
  `po_item_id` INT NOT NULL AUTO_INCREMENT,
  `po_id` INT NOT NULL,
  `item_code` VARCHAR(20) NOT NULL,
  `vendor_part_number` VARCHAR(100) NULL,
  `quantity_ordered` DECIMAL(10,2) NOT NULL,
  `quantity_received` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `unit_cost` DECIMAL(10,2) NOT NULL,
  `request_id` INT NULL,
  `notes` TEXT NULL,
  PRIMARY KEY (`po_item_id`),
  INDEX `fk_POItems_PurchaseOrders_idx` (`po_id` ASC),
  INDEX `fk_POItems_PriceList_idx` (`item_code` ASC),
  INDEX `fk_POItems_Requests_idx` (`request_id` ASC),
  CONSTRAINT `fk_POItems_PurchaseOrders`
    FOREIGN KEY (`po_id`)
    REFERENCES `PurchaseOrders` (`po_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_POItems_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT `fk_POItems_Requests`
    FOREIGN KEY (`request_id`)
    REFERENCES `InventoryRequests` (`request_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `InventoryCounts`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `InventoryCounts` (
  `count_id` INT NOT NULL AUTO_INCREMENT,
  `count_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `location_id` INT NOT NULL,
  `counted_by` VARCHAR(50) NOT NULL,
  `status` ENUM('In Progress', 'Complete', 'Cancelled') NOT NULL DEFAULT 'In Progress',
  `notes` TEXT NULL,
  PRIMARY KEY (`count_id`),
  INDEX `fk_InventoryCounts_Locations_idx` (`location_id` ASC),
  CONSTRAINT `fk_InventoryCounts_Locations`
    FOREIGN KEY (`location_id`)
    REFERENCES `StorageLocations` (`location_id`)
    ON DELETE RESTRICT
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Table `InventoryCountItems`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `InventoryCountItems` (
  `count_item_id` INT NOT NULL AUTO_INCREMENT,
  `count_id` INT NOT NULL,
  `item_code` VARCHAR(20) NOT NULL,
  `sublocation_id` INT NULL,
  `expected_quantity` DECIMAL(10,2) NULL,
  `counted_quantity` DECIMAL(10,2) NOT NULL,
  `variance` DECIMAL(10,2) GENERATED ALWAYS AS (counted_quantity - IFNULL(expected_quantity, 0)) STORED,
  `notes` TEXT NULL,
  PRIMARY KEY (`count_item_id`),
  INDEX `fk_CountItems_Counts_idx` (`count_id` ASC),
  INDEX `fk_CountItems_PriceList_idx` (`item_code` ASC),
  INDEX `fk_CountItems_SubLocations_idx` (`sublocation_id` ASC),
  CONSTRAINT `fk_CountItems_Counts`
    FOREIGN KEY (`count_id`)
    REFERENCES `InventoryCounts` (`count_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_CountItems_PriceList`
    FOREIGN KEY (`item_code`)
    REFERENCES `PriceList` (`item_code`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT `fk_CountItems_SubLocations`
    FOREIGN KEY (`sublocation_id`)
    REFERENCES `StorageSubLocations` (`sublocation_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Add inventory-related fields to PriceList if not exists
-- -----------------------------------------------------
ALTER TABLE `PriceList` 
ADD COLUMN IF NOT EXISTS `unit_of_measure` VARCHAR(20) NULL DEFAULT 'Each' AFTER `notes`,
ADD COLUMN IF NOT EXISTS `track_inventory` BOOLEAN NOT NULL DEFAULT TRUE AFTER `unit_of_measure`,
ADD COLUMN IF NOT EXISTS `default_location_id` INT NULL AFTER `track_inventory`,
ADD CONSTRAINT `fk_PriceList_DefaultLocation`
  FOREIGN KEY (`default_location_id`)
  REFERENCES `StorageLocations` (`location_id`)
  ON DELETE SET NULL
  ON UPDATE NO ACTION;

-- -----------------------------------------------------
-- Sample Data for Testing
-- -----------------------------------------------------

-- Insert sample storage locations
INSERT INTO `StorageLocations` (`location_name`, `location_type`, `description`, `is_mobile`) VALUES
('Main Garage', 'Warehouse', 'Primary storage area with bakers racks', FALSE),
('Storage Shed 1', 'Storage', 'First outdoor storage shed', FALSE),
('Storage Shed 2', 'Storage', 'Second outdoor storage shed', FALSE),
('Outdoor Storage', 'Outdoor', 'PVC pipe and outdoor materials storage', FALSE),
('Van 1', 'Van', 'Primary work van', TRUE),
('Van 2', 'Van', 'Secondary work van', TRUE);

-- Insert sample sub-locations for garage
INSERT INTO `StorageSubLocations` (`location_id`, `sublocation_name`, `sublocation_type`, `position`) 
SELECT location_id, 'Baker Rack 1', 'Rack', 'Left Wall' FROM `StorageLocations` WHERE `location_name` = 'Main Garage';
INSERT INTO `StorageSubLocations` (`location_id`, `sublocation_name`, `sublocation_type`, `position`) 
SELECT location_id, 'Baker Rack 2', 'Rack', 'Center' FROM `StorageLocations` WHERE `location_name` = 'Main Garage';
INSERT INTO `StorageSubLocations` (`location_id`, `sublocation_name`, `sublocation_type`, `position`) 
SELECT location_id, 'Baker Rack 3', 'Rack', 'Right Wall' FROM `StorageLocations` WHERE `location_name` = 'Main Garage';

-- Insert sample sub-locations for Van 1
INSERT INTO `StorageSubLocations` (`location_id`, `sublocation_name`, `sublocation_type`, `position`) 
SELECT location_id, 'Milwaukee Storage Bin', 'Bin', 'Driver Side' FROM `StorageLocations` WHERE `location_name` = 'Van 1';
INSERT INTO `StorageSubLocations` (`location_id`, `sublocation_name`, `sublocation_type`, `position`) 
SELECT location_id, 'Wire Rack', 'Rack', 'Rear' FROM `StorageLocations` WHERE `location_name` = 'Van 1';
INSERT INTO `StorageSubLocations` (`location_id`, `sublocation_name`, `sublocation_type`, `position`) 
SELECT location_id, 'Parts Case', 'Case', 'Passenger Side' FROM `StorageLocations` WHERE `location_name` = 'Van 1';

-- Insert sample vendor items (linking existing vendors to existing price list items)
INSERT INTO `VendorItems` (`vendor_id`, `item_code`, `vendor_part_number`, `unit_cost`, `is_preferred`)
SELECT v.vendor_id, 'O', 'HD-OUTLET-DEC', 2.50, TRUE
FROM `Vendors` v WHERE v.name = 'Home Depot' AND EXISTS (SELECT 1 FROM `PriceList` WHERE `item_code` = 'O');

INSERT INTO `VendorItems` (`vendor_id`, `item_code`, `vendor_part_number`, `unit_cost`, `is_preferred`)
SELECT v.vendor_id, 'S', 'HD-SWITCH-DEC', 3.25, FALSE
FROM `Vendors` v WHERE v.name = 'Home Depot' AND EXISTS (SELECT 1 FROM `PriceList` WHERE `item_code` = 'S');

INSERT INTO `VendorItems` (`vendor_id`, `item_code`, `vendor_part_number`, `unit_cost`, `case_quantity`, `is_preferred`)
SELECT v.vendor_id, 'O', 'COP-5252-I', 1.95, 10, FALSE
FROM `Vendors` v WHERE v.name = 'Cooper' AND EXISTS (SELECT 1 FROM `PriceList` WHERE `item_code` = 'O');

-- Create stored procedures for common operations

DELIMITER //

-- Procedure to check inventory availability
CREATE PROCEDURE `CheckInventoryAvailability`(
    IN p_item_code VARCHAR(20),
    IN p_quantity_needed DECIMAL(10,2),
    OUT p_available_quantity DECIMAL(10,2),
    OUT p_locations_json TEXT
)
BEGIN
    SELECT SUM(quantity_on_hand - quantity_allocated) INTO p_available_quantity
    FROM Inventory
    WHERE item_code = p_item_code AND quantity_on_hand > quantity_allocated;
    
    SELECT JSON_ARRAYAGG(
        JSON_OBJECT(
            'location_name', sl.location_name,
            'sublocation_name', IFNULL(ssl.sublocation_name, ''),
            'available_qty', (i.quantity_on_hand - i.quantity_allocated)
        )
    ) INTO p_locations_json
    FROM Inventory i
    JOIN StorageLocations sl ON i.location_id = sl.location_id
    LEFT JOIN StorageSubLocations ssl ON i.sublocation_id = ssl.sublocation_id
    WHERE i.item_code = p_item_code 
    AND i.quantity_on_hand > i.quantity_allocated
    ORDER BY (i.quantity_on_hand - i.quantity_allocated) DESC;
END //

-- Procedure to create inventory transaction and update balances
CREATE PROCEDURE `ProcessInventoryTransaction`(
    IN p_transaction_type ENUM('Receive', 'Issue', 'Transfer', 'Adjust', 'Count', 'Return', 'Waste'),
    IN p_item_code VARCHAR(20),
    IN p_quantity DECIMAL(10,2),
    IN p_from_location_id INT,
    IN p_to_location_id INT,
    IN p_created_by VARCHAR(50),
    IN p_notes TEXT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;
    
    START TRANSACTION;
    
    -- Insert transaction record
    INSERT INTO InventoryTransactions (
        transaction_type, item_code, quantity,
        from_location_id, to_location_id,
        created_by, notes
    ) VALUES (
        p_transaction_type, p_item_code, p_quantity,
        p_from_location_id, p_to_location_id,
        p_created_by, p_notes
    );
    
    -- Update inventory balances based on transaction type
    IF p_transaction_type IN ('Issue', 'Transfer', 'Waste') AND p_from_location_id IS NOT NULL THEN
        UPDATE Inventory 
        SET quantity_on_hand = quantity_on_hand - p_quantity
        WHERE item_code = p_item_code AND location_id = p_from_location_id;
    END IF;
    
    IF p_transaction_type IN ('Receive', 'Transfer', 'Return') AND p_to_location_id IS NOT NULL THEN
        INSERT INTO Inventory (item_code, location_id, quantity_on_hand)
        VALUES (p_item_code, p_to_location_id, p_quantity)
        ON DUPLICATE KEY UPDATE quantity_on_hand = quantity_on_hand + p_quantity;
    END IF;
    
    IF p_transaction_type = 'Adjust' AND p_to_location_id IS NOT NULL THEN
        UPDATE Inventory 
        SET quantity_on_hand = p_quantity
        WHERE item_code = p_item_code AND location_id = p_to_location_id;
    END IF;
    
    COMMIT;
END //

DELIMITER ;

-- Create views for common queries

-- View for current inventory levels with location details
CREATE OR REPLACE VIEW `vw_inventory_levels` AS
SELECT 
    i.inventory_id,
    i.item_code,
    pl.name AS item_name,
    pl.category,
    sl.location_name,
    ssl.sublocation_name,
    i.quantity_on_hand,
    i.quantity_allocated,
    (i.quantity_on_hand - i.quantity_allocated) AS available_quantity,
    i.min_quantity,
    i.reorder_point,
    CASE 
        WHEN i.quantity_on_hand <= i.min_quantity THEN 'Critical'
        WHEN i.quantity_on_hand <= i.reorder_point THEN 'Low'
        ELSE 'OK'
    END AS stock_status
FROM Inventory i
JOIN PriceList pl ON i.item_code = pl.item_code
JOIN StorageLocations sl ON i.location_id = sl.location_id
LEFT JOIN StorageSubLocations ssl ON i.sublocation_id = ssl.sublocation_id
WHERE pl.is_active = TRUE;

-- View for items needing reorder
CREATE OR REPLACE VIEW `vw_reorder_needed` AS
SELECT 
    i.item_code,
    pl.name AS item_name,
    SUM(i.quantity_on_hand) AS total_on_hand,
    SUM(i.quantity_allocated) AS total_allocated,
    SUM(i.quantity_on_hand - i.quantity_allocated) AS total_available,
    MAX(i.reorder_point) AS reorder_point,
    GROUP_CONCAT(DISTINCT v.name) AS available_vendors
FROM Inventory i
JOIN PriceList pl ON i.item_code = pl.item_code
LEFT JOIN VendorItems vi ON i.item_code = vi.item_code
LEFT JOIN Vendors v ON vi.vendor_id = v.vendor_id
WHERE pl.is_active = TRUE
GROUP BY i.item_code, pl.name
HAVING total_available <= reorder_point;