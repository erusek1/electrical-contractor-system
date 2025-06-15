-- Database migration to consolidate Materials and PriceList
-- This makes PriceList the single source of truth for all items

USE electrical_contractor_db;

-- Step 1: Add missing columns to PriceList if they don't exist
ALTER TABLE `PriceList` 
ADD COLUMN IF NOT EXISTS `unit_of_measure` VARCHAR(20) NOT NULL DEFAULT 'Each' AFTER `description`,
ADD COLUMN IF NOT EXISTS `current_price` DECIMAL(10,2) NULL AFTER `base_cost`,
ADD COLUMN IF NOT EXISTS `min_stock_level` INT NULL DEFAULT 0 AFTER `is_active`,
ADD COLUMN IF NOT EXISTS `max_stock_level` INT NULL DEFAULT 0 AFTER `min_stock_level`,
ADD COLUMN IF NOT EXISTS `preferred_vendor_id` INT NULL AFTER `max_stock_level`,
ADD COLUMN IF NOT EXISTS `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP AFTER `notes`,
ADD COLUMN IF NOT EXISTS `updated_date` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP AFTER `created_date`;

-- Add foreign key for vendor
ALTER TABLE `PriceList`
ADD CONSTRAINT `fk_PriceList_Vendors`
  FOREIGN KEY (`preferred_vendor_id`)
  REFERENCES `Vendors` (`vendor_id`)
  ON DELETE SET NULL
  ON UPDATE NO ACTION;

-- Step 2: Migrate data from Materials to PriceList (if Materials has data)
INSERT INTO `PriceList` (
    `item_code`, 
    `category`, 
    `name`, 
    `description`, 
    `unit_of_measure`,
    `base_cost`, 
    `current_price`,
    `tax_rate`, 
    `min_stock_level`,
    `max_stock_level`,
    `preferred_vendor_id`,
    `is_active`,
    `created_date`,
    `updated_date`
)
SELECT 
    m.material_code,
    m.category,
    m.name,
    m.description,
    m.unit_of_measure,
    m.current_price, -- Use current_price as base_cost
    m.current_price,
    m.tax_rate / 100, -- Convert percentage to decimal
    m.min_stock_level,
    m.max_stock_level,
    m.preferred_vendor_id,
    m.is_active,
    m.created_date,
    m.updated_date
FROM `Materials` m
WHERE NOT EXISTS (
    SELECT 1 FROM `PriceList` p 
    WHERE p.item_code = m.material_code
);

-- Step 3: Create PriceListHistory table (rename from MaterialPriceHistory)
CREATE TABLE IF NOT EXISTS `PriceListHistory` (
  `price_history_id` INT NOT NULL AUTO_INCREMENT,
  `item_id` INT NOT NULL,
  `price` DECIMAL(10,2) NOT NULL,
  `effective_date` DATE NOT NULL,
  `vendor_id` INT NULL,
  `purchase_order_number` VARCHAR(50) NULL,
  `quantity_purchased` DECIMAL(10,2) NULL,
  `notes` TEXT NULL,
  `created_by` VARCHAR(50) NOT NULL,
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`price_history_id`),
  INDEX `idx_item_date` (`item_id` ASC, `effective_date` DESC),
  CONSTRAINT `fk_PriceHistory_PriceList`
    FOREIGN KEY (`item_id`)
    REFERENCES `PriceList` (`item_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_PriceHistory_Vendors`
    FOREIGN KEY (`vendor_id`)
    REFERENCES `Vendors` (`vendor_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- Step 4: Migrate price history data
INSERT INTO `PriceListHistory` (
    `item_id`,
    `price`,
    `effective_date`,
    `vendor_id`,
    `purchase_order_number`,
    `quantity_purchased`,
    `notes`,
    `created_by`,
    `created_date`
)
SELECT 
    p.item_id,
    mph.price,
    mph.effective_date,
    mph.vendor_id,
    mph.purchase_order_number,
    mph.quantity_purchased,
    mph.notes,
    mph.created_by,
    mph.created_date
FROM `MaterialPriceHistory` mph
INNER JOIN `Materials` m ON mph.material_id = m.material_id
INNER JOIN `PriceList` p ON p.item_code = m.material_code;

-- Step 5: Update AssemblyComponents to reference PriceList
ALTER TABLE `AssemblyComponents` 
ADD COLUMN `item_id` INT NULL AFTER `material_id`;

-- Populate the new item_id column
UPDATE `AssemblyComponents` ac
INNER JOIN `Materials` m ON ac.material_id = m.material_id
INNER JOIN `PriceList` p ON p.item_code = m.material_code
SET ac.item_id = p.item_id;

-- Drop the old foreign key constraint
ALTER TABLE `AssemblyComponents` 
DROP FOREIGN KEY `fk_Components_Materials`;

-- Drop the old material_id column
ALTER TABLE `AssemblyComponents` 
DROP COLUMN `material_id`;

-- Rename item_id to material_id for consistency (optional, but keeps naming consistent)
ALTER TABLE `AssemblyComponents` 
CHANGE COLUMN `item_id` `price_list_item_id` INT NOT NULL;

-- Add the new foreign key constraint
ALTER TABLE `AssemblyComponents`
ADD CONSTRAINT `fk_Components_PriceList`
  FOREIGN KEY (`price_list_item_id`)
  REFERENCES `PriceList` (`item_id`)
  ON DELETE RESTRICT
  ON UPDATE NO ACTION;

-- Update unique constraint
ALTER TABLE `AssemblyComponents` 
DROP INDEX `unique_assembly_material`,
ADD UNIQUE INDEX `unique_assembly_item` (`assembly_id` ASC, `price_list_item_id` ASC);

-- Step 6: Create Inventory table that references PriceList
CREATE TABLE IF NOT EXISTS `Inventory` (
  `inventory_id` INT NOT NULL AUTO_INCREMENT,
  `item_id` INT NOT NULL,
  `location_id` INT NULL,
  `quantity_on_hand` DECIMAL(10,3) NOT NULL DEFAULT 0,
  `quantity_allocated` DECIMAL(10,3) NOT NULL DEFAULT 0,
  `quantity_available` DECIMAL(10,3) GENERATED ALWAYS AS (quantity_on_hand - quantity_allocated) STORED,
  `reorder_point` DECIMAL(10,3) NULL,
  `reorder_quantity` DECIMAL(10,3) NULL,
  `last_count_date` DATE NULL,
  `last_count_by` VARCHAR(50) NULL,
  `notes` TEXT NULL,
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_date` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`inventory_id`),
  UNIQUE INDEX `unique_item_location` (`item_id` ASC, `location_id` ASC),
  CONSTRAINT `fk_Inventory_PriceList`
    FOREIGN KEY (`item_id`)
    REFERENCES `PriceList` (`item_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Inventory_Locations`
    FOREIGN KEY (`location_id`)
    REFERENCES `StorageLocation` (`location_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- Step 7: Create Inventory Transactions table
CREATE TABLE IF NOT EXISTS `InventoryTransactions` (
  `transaction_id` INT NOT NULL AUTO_INCREMENT,
  `item_id` INT NOT NULL,
  `transaction_type` ENUM('RECEIVE', 'ISSUE', 'ADJUST', 'RETURN', 'TRANSFER') NOT NULL,
  `quantity` DECIMAL(10,3) NOT NULL,
  `unit_cost` DECIMAL(10,2) NULL,
  `job_id` INT NULL,
  `vendor_id` INT NULL,
  `purchase_order_number` VARCHAR(50) NULL,
  `invoice_number` VARCHAR(50) NULL,
  `from_location_id` INT NULL,
  `to_location_id` INT NULL,
  `reason` TEXT NULL,
  `transaction_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `created_by` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`transaction_id`),
  INDEX `idx_item_date` (`item_id` ASC, `transaction_date` DESC),
  INDEX `idx_job` (`job_id` ASC),
  CONSTRAINT `fk_InvTrans_PriceList`
    FOREIGN KEY (`item_id`)
    REFERENCES `PriceList` (`item_id`)
    ON DELETE RESTRICT
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
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_InvTrans_FromLocation`
    FOREIGN KEY (`from_location_id`)
    REFERENCES `StorageLocation` (`location_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_InvTrans_ToLocation`
    FOREIGN KEY (`to_location_id`)
    REFERENCES `StorageLocation` (`location_id`)
    ON DELETE SET NULL
    ON UPDATE NO ACTION
);

-- Step 8: Update views to use PriceList
DROP VIEW IF EXISTS vw_current_material_prices;
DROP VIEW IF EXISTS vw_assembly_costs;

CREATE VIEW vw_current_price_list AS
SELECT 
    p.item_id,
    p.item_code,
    p.name,
    p.category,
    p.base_cost,
    COALESCE(p.current_price, p.base_cost) as current_price,
    p.tax_rate,
    ROUND(COALESCE(p.current_price, p.base_cost) * (1 + p.tax_rate), 2) as price_with_tax,
    p.markup_percentage,
    ROUND(COALESCE(p.current_price, p.base_cost) * (1 + p.markup_percentage/100), 2) as sell_price,
    p.unit_of_measure,
    COALESCE(i.quantity_available, 0) as stock_available,
    p.min_stock_level,
    p.max_stock_level,
    (\n        SELECT AVG(plh.price) \n        FROM PriceListHistory plh \n        WHERE plh.item_id = p.item_id \n        AND plh.effective_date >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)\n    ) as avg_30_day_price,\n    (\n        SELECT AVG(plh.price) \n        FROM PriceListHistory plh \n        WHERE plh.item_id = p.item_id \n        AND plh.effective_date >= DATE_SUB(CURDATE(), INTERVAL 90 DAY)\n    ) as avg_90_day_price\nFROM PriceList p\nLEFT JOIN Inventory i ON p.item_id = i.item_id AND i.location_id IS NULL\nWHERE p.is_active = TRUE;

CREATE VIEW vw_assembly_costs AS
SELECT \n    at.assembly_id,\n    at.assembly_code,\n    at.name,\n    at.rough_minutes,\n    at.finish_minutes,\n    at.service_minutes,\n    at.extra_minutes,\n    (at.rough_minutes + at.finish_minutes + at.service_minutes + at.extra_minutes) / 60.0 as total_labor_hours,\n    SUM(ac.quantity * COALESCE(p.current_price, p.base_cost) * (1 + p.tax_rate)) as total_material_cost,\n    SUM(ac.quantity * COALESCE(p.current_price, p.base_cost) * (1 + p.markup_percentage/100) * (1 + p.tax_rate)) as total_sell_price\nFROM AssemblyTemplates at\nLEFT JOIN AssemblyComponents ac ON at.assembly_id = ac.assembly_id\nLEFT JOIN PriceList p ON ac.price_list_item_id = p.item_id\nWHERE at.is_active = TRUE\nGROUP BY at.assembly_id;

-- Step 9: Drop the old Materials tables (only after verifying migration)
-- IMPORTANT: Run these commands only after confirming all data is migrated correctly!
-- DROP TABLE IF EXISTS `MaterialPriceHistory`;
-- DROP TABLE IF EXISTS `Materials`;

-- Step 10: Update current_price from base_cost where null
UPDATE `PriceList` 
SET `current_price` = `base_cost` 
WHERE `current_price` IS NULL;

-- Add helpful indexes
CREATE INDEX idx_pricelist_code ON PriceList(item_code);
CREATE INDEX idx_pricelist_category ON PriceList(category);
CREATE INDEX idx_pricelist_active ON PriceList(is_active);
CREATE INDEX idx_inventory_item ON Inventory(item_id);
CREATE INDEX idx_inventory_available ON Inventory(quantity_available);

COMMIT;
