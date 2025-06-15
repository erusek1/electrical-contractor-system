-- Add pricing and assembly tables to electrical_contractor_db
-- Run this script to add the new tables for the pricing and assembly features

USE electrical_contractor_db;

-- Materials table
CREATE TABLE IF NOT EXISTS Materials (
    material_id INT NOT NULL AUTO_INCREMENT,
    material_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(100) NOT NULL,
    unit_of_measure VARCHAR(50) NOT NULL DEFAULT 'each',
    current_price DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    tax_rate DECIMAL(5,3) NOT NULL DEFAULT 0.064,
    min_stock_level INT NOT NULL DEFAULT 0,
    max_stock_level INT NOT NULL DEFAULT 0,
    preferred_vendor_id INT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_date DATETIME,
    PRIMARY KEY (material_id),
    INDEX idx_material_code (material_code),
    INDEX idx_category (category),
    FOREIGN KEY (preferred_vendor_id) REFERENCES Vendors(vendor_id) ON DELETE SET NULL
);

-- Material Price History
CREATE TABLE IF NOT EXISTS MaterialPriceHistory (
    price_history_id INT NOT NULL AUTO_INCREMENT,
    material_id INT NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    effective_date DATETIME NOT NULL,
    vendor_id INT,
    purchase_order_number VARCHAR(50),
    quantity_purchased DECIMAL(10,2),
    notes TEXT,
    created_by VARCHAR(100) NOT NULL,
    created_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (price_history_id),
    INDEX idx_material_date (material_id, effective_date),
    FOREIGN KEY (material_id) REFERENCES Materials(material_id) ON DELETE CASCADE,
    FOREIGN KEY (vendor_id) REFERENCES Vendors(vendor_id) ON DELETE SET NULL
);

-- Assembly Templates
CREATE TABLE IF NOT EXISTS AssemblyTemplates (
    assembly_id INT NOT NULL AUTO_INCREMENT,
    assembly_code VARCHAR(20) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(100) NOT NULL,
    rough_minutes INT NOT NULL DEFAULT 0,
    finish_minutes INT NOT NULL DEFAULT 0,
    service_minutes INT NOT NULL DEFAULT 0,
    extra_minutes INT NOT NULL DEFAULT 0,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INT NOT NULL DEFAULT 0,
    created_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100) NOT NULL,
    updated_date DATETIME,
    PRIMARY KEY (assembly_id),
    INDEX idx_assembly_code (assembly_code),
    INDEX idx_category (category)
);

-- Assembly Components
CREATE TABLE IF NOT EXISTS AssemblyComponents (
    component_id INT NOT NULL AUTO_INCREMENT,
    assembly_id INT NOT NULL,
    material_id INT NOT NULL,
    quantity DECIMAL(10,2) NOT NULL DEFAULT 1.00,
    is_optional BOOLEAN NOT NULL DEFAULT FALSE,
    notes TEXT,
    PRIMARY KEY (component_id),
    UNIQUE KEY uk_assembly_material (assembly_id, material_id),
    FOREIGN KEY (assembly_id) REFERENCES AssemblyTemplates(assembly_id) ON DELETE CASCADE,
    FOREIGN KEY (material_id) REFERENCES Materials(material_id) ON DELETE CASCADE
);

-- Assembly Variants
CREATE TABLE IF NOT EXISTS AssemblyVariants (
    variant_id INT NOT NULL AUTO_INCREMENT,
    parent_assembly_id INT NOT NULL,
    variant_assembly_id INT NOT NULL,
    sort_order INT NOT NULL DEFAULT 0,
    PRIMARY KEY (variant_id),
    UNIQUE KEY uk_parent_variant (parent_assembly_id, variant_assembly_id),
    FOREIGN KEY (parent_assembly_id) REFERENCES AssemblyTemplates(assembly_id) ON DELETE CASCADE,
    FOREIGN KEY (variant_assembly_id) REFERENCES AssemblyTemplates(assembly_id) ON DELETE CASCADE
);

-- Difficulty Presets
CREATE TABLE IF NOT EXISTS DifficultyPresets (
    preset_id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    category VARCHAR(50) NOT NULL,
    rough_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    finish_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    service_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    extra_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INT NOT NULL DEFAULT 0,
    PRIMARY KEY (preset_id)
);

-- Labor Adjustments
CREATE TABLE IF NOT EXISTS LaborAdjustments (
    adjustment_id INT NOT NULL AUTO_INCREMENT,
    estimate_id INT,
    job_id INT,
    adjustment_type ENUM('PRESET', 'CUSTOM', 'GLOBAL') NOT NULL,
    preset_id INT,
    rough_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    finish_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    service_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    extra_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    reason VARCHAR(255),
    created_by VARCHAR(100) NOT NULL,
    created_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (adjustment_id),
    FOREIGN KEY (estimate_id) REFERENCES Estimates(estimate_id) ON DELETE CASCADE,
    FOREIGN KEY (job_id) REFERENCES Jobs(job_id) ON DELETE CASCADE,
    FOREIGN KEY (preset_id) REFERENCES DifficultyPresets(preset_id) ON DELETE SET NULL
);

-- Service Types
CREATE TABLE IF NOT EXISTS ServiceTypes (
    service_type_id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    rate_multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INT NOT NULL DEFAULT 0,
    PRIMARY KEY (service_type_id)
);

-- Views for assembly calculations
CREATE OR REPLACE VIEW vw_assembly_costs AS
SELECT 
    at.assembly_id,
    at.assembly_code,
    at.name AS assembly_name,
    COALESCE(SUM(ac.quantity * m.current_price), 0) AS total_material_cost,
    at.rough_minutes + at.finish_minutes + at.service_minutes + at.extra_minutes AS total_labor_minutes
FROM AssemblyTemplates at
LEFT JOIN AssemblyComponents ac ON at.assembly_id = ac.assembly_id
LEFT JOIN Materials m ON ac.material_id = m.material_id
WHERE at.is_active = TRUE
GROUP BY at.assembly_id;

-- View for material price alerts
CREATE OR REPLACE VIEW vw_material_price_alerts AS
SELECT 
    m.material_id,
    m.material_code,
    m.name,
    m.current_price,
    mph.price AS previous_price,
    mph.effective_date AS previous_date,
    ((m.current_price - mph.price) / mph.price * 100) AS percentage_change,
    CASE 
        WHEN ABS((m.current_price - mph.price) / mph.price * 100) >= 15 THEN 'IMMEDIATE'
        WHEN ABS((m.current_price - mph.price) / mph.price * 100) >= 5 THEN 'REVIEW'
        ELSE 'NONE'
    END AS alert_level
FROM Materials m
JOIN MaterialPriceHistory mph ON m.material_id = mph.material_id
WHERE mph.effective_date = (
    SELECT MAX(effective_date) 
    FROM MaterialPriceHistory 
    WHERE material_id = m.material_id 
    AND effective_date < CURRENT_DATE
);

-- Insert default difficulty presets
INSERT INTO DifficultyPresets (name, description, category, rough_multiplier, finish_multiplier, service_multiplier, extra_multiplier, sort_order) VALUES
('New Construction', 'New construction with open walls', 'Construction Type', 0.90, 0.95, 1.00, 1.00, 1),
('Standard Renovation', 'Standard renovation project', 'Construction Type', 1.00, 1.00, 1.00, 1.00, 2),
('Old House (Pre-1960)', 'Older construction with potential issues', 'Construction Type', 1.20, 1.10, 1.10, 1.10, 3),
('Beach House', 'Coastal property with corrosion concerns', 'Location', 1.20, 1.10, 1.05, 1.05, 4),
('Occupied Home', 'Working around furniture and residents', 'Conditions', 1.10, 1.10, 1.10, 1.10, 5),
('December Work', 'Holiday decorations and limited access', 'Seasonal', 1.15, 1.15, 1.15, 1.15, 6);

-- Insert default service types
INSERT INTO ServiceTypes (name, description, rate_multiplier, sort_order) VALUES
('Residential', 'Standard residential rate', 1.00, 1),
('Service Call', 'Service call rate', 1.50, 2),
('Commercial', 'Commercial rate', 1.10, 3),
('Emergency', 'Emergency/after-hours rate', 2.00, 4);

-- Insert sample materials
INSERT INTO Materials (material_code, name, description, category, unit_of_measure, current_price, tax_rate) VALUES
('1900', '1 Gang Plastic Box', 'Single gang plastic electrical box', 'Boxes', 'each', 0.58, 0.064),
('RX15', 'Decora Receptacle 15A', 'Decorator style receptacle, 15 amp', 'Devices', 'each', 4.25, 0.064),
('S15', 'Decora Switch 15A', 'Decorator style switch, 15 amp', 'Devices', 'each', 3.85, 0.064),
('3WS15', '3-Way Decora Switch 15A', 'Decorator style 3-way switch, 15 amp', 'Devices', 'each', 8.50, 0.064),
('GFI15', 'GFCI Receptacle 15A', 'Ground fault circuit interrupter, 15 amp', 'Devices', 'each', 18.75, 0.064),
('12-2NM', '12-2 Romex', '12 AWG 2-conductor NM cable', 'Wire', 'foot', 0.68, 0.064),
('14-2NM', '14-2 Romex', '14 AWG 2-conductor NM cable', 'Wire', 'foot', 0.42, 0.064),
('LED4', '4" LED Recessed Light', '4 inch LED recessed light fixture', 'Lighting', 'each', 28.50, 0.064);

-- Insert sample assemblies
INSERT INTO AssemblyTemplates (assembly_code, name, description, category, rough_minutes, finish_minutes, is_default, created_by) VALUES
('o', 'Decora Outlet', 'Standard decorator outlet installation', 'Devices', 30, 20, TRUE, 'System'),
('s', 'Single Pole Switch', 'Standard decorator switch installation', 'Devices', 30, 20, TRUE, 'System'),
('3w', '3-Way Switch', '3-way decorator switch installation', 'Devices', 35, 25, TRUE, 'System'),
('gfi', 'GFCI Outlet', 'Ground fault circuit interrupter installation', 'Devices', 35, 25, TRUE, 'System'),
('hh', '4" LED Recessed Light', '4 inch LED recessed light installation', 'Lighting', 40, 25, TRUE, 'System');

-- Insert assembly components (linking assemblies to materials)
-- Outlet assembly
INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 'o' AND m.material_code = '1900';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 'o' AND m.material_code = 'RX15';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 15 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 'o' AND m.material_code = '12-2NM';

-- Switch assembly
INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 's' AND m.material_code = '1900';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 's' AND m.material_code = 'S15';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 15 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 's' AND m.material_code = '14-2NM';

-- 3-way switch assembly
INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = '3w' AND m.material_code = '1900';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = '3w' AND m.material_code = '3WS15';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 20 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = '3w' AND m.material_code = '14-2NM';

-- GFCI assembly
INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 'gfi' AND m.material_code = '1900';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 'gfi' AND m.material_code = 'GFI15';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 15 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 'gfi' AND m.material_code = '12-2NM';

-- LED recessed light assembly
INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 1 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 'hh' AND m.material_code = 'LED4';

INSERT INTO AssemblyComponents (assembly_id, material_id, quantity) 
SELECT a.assembly_id, m.material_id, 15 
FROM AssemblyTemplates a, Materials m 
WHERE a.assembly_code = 'hh' AND m.material_code = '14-2NM';

-- Add initial price history for all materials
INSERT INTO MaterialPriceHistory (material_id, price, effective_date, created_by)
SELECT material_id, current_price, CURRENT_DATE, 'System'
FROM Materials;

COMMIT;