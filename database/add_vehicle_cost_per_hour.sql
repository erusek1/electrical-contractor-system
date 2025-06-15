-- Add vehicle_cost_per_hour column to Employees table
ALTER TABLE Employees 
ADD COLUMN vehicle_cost_per_hour DECIMAL(10,2) NULL AFTER burden_rate;

-- Update existing records to calculate hourly rate from monthly if needed
UPDATE Employees 
SET vehicle_cost_per_hour = vehicle_cost_per_month / 173.33 
WHERE vehicle_cost_per_month IS NOT NULL AND vehicle_cost_per_hour IS NULL;