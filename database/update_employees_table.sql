-- Update script to add missing columns to the Employees table
-- Run this script on your existing database to add the new employee cost tracking columns

-- Add vehicle_cost_per_hour column
ALTER TABLE `Employees` 
ADD COLUMN `vehicle_cost_per_hour` DECIMAL(10,2) NULL AFTER `burden_rate`;

-- Add vehicle_cost_per_month column
ALTER TABLE `Employees` 
ADD COLUMN `vehicle_cost_per_month` DECIMAL(10,2) NULL AFTER `vehicle_cost_per_hour`;

-- Add overhead_percentage column
ALTER TABLE `Employees` 
ADD COLUMN `overhead_percentage` DECIMAL(5,2) NULL AFTER `vehicle_cost_per_month`;

-- Update the existing employees with default values if needed
UPDATE `Employees` 
SET 
    `vehicle_cost_per_hour` = 0,
    `vehicle_cost_per_month` = 0,
    `overhead_percentage` = 0
WHERE `vehicle_cost_per_hour` IS NULL;