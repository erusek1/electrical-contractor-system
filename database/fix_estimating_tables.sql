-- Fix Estimating Tables Script
-- Run this after the initial add_estimating_tables.sql if you encountered errors

USE electrical_contractor_db;

-- First, let's clean up any partial installations
DROP TABLE IF EXISTS `EstimateLineItems`;
DROP TABLE IF EXISTS `EstimateRooms`;
DROP TABLE IF EXISTS `EstimateStageSummary`;
DROP TABLE IF EXISTS `Estimates`;
DROP TABLE IF EXISTS `RoomTemplateItems`;
DROP TABLE IF EXISTS `RoomTemplates`;
DROP TABLE IF EXISTS `MaterialStages`;
DROP TABLE IF EXISTS `LaborMinutes`;
DROP TABLE IF EXISTS `PriceListItems`;

-- Remove the estimate_id column from Jobs if it exists (we'll re-add it properly)
SET @col_exists = 0;
SELECT COUNT(*) INTO @col_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Jobs' 
  AND COLUMN_NAME = 'estimate_id';

SET @sql = IF(@col_exists = 1,
  'ALTER TABLE `Jobs` DROP FOREIGN KEY `fk_Jobs_Estimates`, DROP COLUMN `estimate_id`',
  'SELECT "Column estimate_id does not exist in Jobs table"');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Now run the updated add_estimating_tables.sql script
-- The updated script uses the correct table and column names
