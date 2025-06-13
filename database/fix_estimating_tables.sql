-- Fix Estimating Tables Script
-- Run this after the initial add_estimating_tables.sql if you encountered errors

USE electrical_contractor_db;

-- Temporarily disable foreign key checks to allow cleanup
SET FOREIGN_KEY_CHECKS = 0;

-- Drop all estimating-related tables in the correct order
DROP TABLE IF EXISTS `EstimateItems`;
DROP TABLE IF EXISTS `EstimateLineItems`;
DROP TABLE IF EXISTS `EstimateRooms`;
DROP TABLE IF EXISTS `EstimateStageSummary`;
DROP TABLE IF EXISTS `Estimates`;
DROP TABLE IF EXISTS `RoomTemplateItems`;
DROP TABLE IF EXISTS `RoomTemplates`;
DROP TABLE IF EXISTS `MaterialStages`;
DROP TABLE IF EXISTS `LaborMinutes`;
DROP TABLE IF EXISTS `PriceListItems`;

-- Remove the estimate_id column from Jobs if it exists
SET @col_exists = 0;
SELECT COUNT(*) INTO @col_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Jobs' 
  AND COLUMN_NAME = 'estimate_id';

SET @sql = IF(@col_exists = 1,
  'ALTER TABLE `Jobs` DROP FOREIGN KEY `fk_Jobs_Estimates`',
  'SELECT "No foreign key to drop"');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = IF(@col_exists = 1,
  'ALTER TABLE `Jobs` DROP COLUMN `estimate_id`',
  'SELECT "Column estimate_id does not exist in Jobs table"');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- Now you're ready to run the updated add_estimating_tables.sql script
SELECT 'Cleanup complete. Now run the add_estimating_tables.sql script.' as message;
