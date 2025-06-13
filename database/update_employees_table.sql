-- Add missing columns to Employees table
ALTER TABLE Employees 
ADD COLUMN IF NOT EXISTS vehicle_cost_per_month DECIMAL(10,2) NULL AFTER burden_rate,
ADD COLUMN IF NOT EXISTS overhead_percentage DECIMAL(5,2) NULL AFTER vehicle_cost_per_month;
