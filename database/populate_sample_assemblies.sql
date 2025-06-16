-- Sample Assembly Data Population
-- Run this after the tables are created to add common assemblies

USE electrical_contractor_db;

-- First, add some common materials
INSERT INTO `Materials` (`material_code`, `name`, `description`, `unit_of_measure`, `current_price`, `category`) VALUES
-- Devices
('RECEP-DEC', 'Decora Receptacle 15A', 'Leviton Decora 15A 125V receptacle', 'each', 4.50, 'Devices'),
('RECEP-DUP', 'Duplex Receptacle 15A', 'Standard duplex 15A 125V receptacle', 'each', 2.75, 'Devices'),
('RECEP-GFCI', 'GFCI Receptacle 15A', 'Leviton 15A GFCI receptacle', 'each', 22.00, 'Devices'),
('SWITCH-SP', 'Single Pole Decora Switch', 'Leviton Decora single pole switch', 'each', 4.25, 'Devices'),
('SWITCH-3W', '3-Way Decora Switch', 'Leviton Decora 3-way switch', 'each', 8.50, 'Devices'),
('SWITCH-DIM', 'Dimmer Switch', 'Lutron CL dimmer switch', 'each', 18.00, 'Devices'),

-- Boxes
('BOX-1G-OLD', '1-Gang Old Work Box', 'Carlon 1-gang old work box', 'each', 2.85, 'Boxes'),
('BOX-1G-NEW', '1-Gang New Work Box', 'Carlon 1-gang new work box with nails', 'each', 1.25, 'Boxes'),
('BOX-2G-OLD', '2-Gang Old Work Box', 'Carlon 2-gang old work box', 'each', 4.50, 'Boxes'),
('BOX-4SQ', '4-Square Box', '4" square metal box', 'each', 3.75, 'Boxes'),

-- Wire
('WIRE-12-2', '12-2 Romex', '12 AWG 2-conductor with ground NM-B', 'feet', 0.85, 'Wire'),
('WIRE-14-2', '14-2 Romex', '14 AWG 2-conductor with ground NM-B', 'feet', 0.55, 'Wire'),
('WIRE-12-3', '12-3 Romex', '12 AWG 3-conductor with ground NM-B', 'feet', 1.25, 'Wire'),

-- Wire Nuts
('WIRENUT-YEL', 'Yellow Wire Nuts', 'Ideal yellow wire nuts', 'each', 0.08, 'Wire Connectors'),
('WIRENUT-RED', 'Red Wire Nuts', 'Ideal red wire nuts', 'each', 0.10, 'Wire Connectors'),

-- Plates
('PLATE-1G-DEC', '1-Gang Decora Plate', 'Leviton 1-gang Decora wallplate', 'each', 1.25, 'Plates'),
('PLATE-2G-DEC', '2-Gang Decora Plate', 'Leviton 2-gang Decora wallplate', 'each', 2.50, 'Plates'),

-- Lighting
('CAN-4-LED', '4" LED Recessed Light', 'Halo 4" LED recessed light', 'each', 28.00, 'Lighting'),
('FAN-BATH', 'Bathroom Exhaust Fan', 'Panasonic 110CFM exhaust fan', 'each', 125.00, 'Lighting');

-- Now create assembly templates with your common codes

-- Outlet assemblies
INSERT INTO `AssemblyTemplates` (`assembly_code`, `name`, `description`, `category`, `is_default`, `labor_minutes_rough`, `labor_minutes_finish`) VALUES
('o', 'Outlet - Decora (Default)', 'Standard Decora outlet installation', 'Outlets', TRUE, 30, 20),
('o', 'Outlet - Duplex', 'Standard duplex outlet installation', 'Outlets', FALSE, 30, 20),
('gfi', 'Outlet - GFCI', 'GFCI protected outlet installation', 'Outlets', FALSE, 30, 30);

-- Get the assembly IDs we just created
SET @outlet_decora_id = (SELECT assembly_id FROM AssemblyTemplates WHERE name = 'Outlet - Decora (Default)');
SET @outlet_duplex_id = (SELECT assembly_id FROM AssemblyTemplates WHERE name = 'Outlet - Duplex');
SET @outlet_gfci_id = (SELECT assembly_id FROM AssemblyTemplates WHERE name = 'Outlet - GFCI');

-- Add components to Decora outlet assembly
INSERT INTO `AssemblyComponents` (`assembly_id`, `material_id`, `quantity`) VALUES
(@outlet_decora_id, (SELECT material_id FROM Materials WHERE material_code = 'RECEP-DEC'), 1),
(@outlet_decora_id, (SELECT material_id FROM Materials WHERE material_code = 'BOX-1G-OLD'), 1),
(@outlet_decora_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRE-12-2'), 25),
(@outlet_decora_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRENUT-YEL'), 3),
(@outlet_decora_id, (SELECT material_id FROM Materials WHERE material_code = 'PLATE-1G-DEC'), 1);

-- Add components to Duplex outlet assembly
INSERT INTO `AssemblyComponents` (`assembly_id`, `material_id`, `quantity`) VALUES
(@outlet_duplex_id, (SELECT material_id FROM Materials WHERE material_code = 'RECEP-DUP'), 1),
(@outlet_duplex_id, (SELECT material_id FROM Materials WHERE material_code = 'BOX-1G-OLD'), 1),
(@outlet_duplex_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRE-12-2'), 25),
(@outlet_duplex_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRENUT-YEL'), 3),
(@outlet_duplex_id, (SELECT material_id FROM Materials WHERE material_code = 'PLATE-1G-DEC'), 1);

-- Add components to GFCI outlet assembly
INSERT INTO `AssemblyComponents` (`assembly_id`, `material_id`, `quantity`) VALUES
(@outlet_gfci_id, (SELECT material_id FROM Materials WHERE material_code = 'RECEP-GFCI'), 1),
(@outlet_gfci_id, (SELECT material_id FROM Materials WHERE material_code = 'BOX-1G-OLD'), 1),
(@outlet_gfci_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRE-12-2'), 25),
(@outlet_gfci_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRENUT-YEL'), 4),
(@outlet_gfci_id, (SELECT material_id FROM Materials WHERE material_code = 'PLATE-1G-DEC'), 1);

-- Create outlet variants
INSERT INTO `AssemblyVariants` (`parent_assembly_id`, `variant_assembly_id`, `sort_order`) VALUES
(@outlet_decora_id, @outlet_decora_id, 0),  -- Default comes first
(@outlet_decora_id, @outlet_duplex_id, 1),
(@outlet_decora_id, @outlet_gfci_id, 2);

-- Switch assemblies
INSERT INTO `AssemblyTemplates` (`assembly_code`, `name`, `description`, `category`, `is_default`, `labor_minutes_rough`, `labor_minutes_finish`) VALUES
('s', 'Switch - Single Pole Decora (Default)', 'Standard single pole switch installation', 'Switches', TRUE, 30, 20),
('s', 'Switch - Dimmer', 'Dimmer switch installation', 'Switches', FALSE, 30, 25),
('3w', 'Switch - 3-Way Decora', '3-way switch installation', 'Switches', TRUE, 45, 25);

-- Get the switch assembly IDs
SET @switch_sp_id = (SELECT assembly_id FROM AssemblyTemplates WHERE name = 'Switch - Single Pole Decora (Default)');
SET @switch_dim_id = (SELECT assembly_id FROM AssemblyTemplates WHERE name = 'Switch - Dimmer');
SET @switch_3w_id = (SELECT assembly_id FROM AssemblyTemplates WHERE name = 'Switch - 3-Way Decora');

-- Add components to single pole switch
INSERT INTO `AssemblyComponents` (`assembly_id`, `material_id`, `quantity`) VALUES
(@switch_sp_id, (SELECT material_id FROM Materials WHERE material_code = 'SWITCH-SP'), 1),
(@switch_sp_id, (SELECT material_id FROM Materials WHERE material_code = 'BOX-1G-OLD'), 1),
(@switch_sp_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRE-14-2'), 25),
(@switch_sp_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRENUT-YEL'), 3),
(@switch_sp_id, (SELECT material_id FROM Materials WHERE material_code = 'PLATE-1G-DEC'), 1);

-- Add components to dimmer switch
INSERT INTO `AssemblyComponents` (`assembly_id`, `material_id`, `quantity`) VALUES
(@switch_dim_id, (SELECT material_id FROM Materials WHERE material_code = 'SWITCH-DIM'), 1),
(@switch_dim_id, (SELECT material_id FROM Materials WHERE material_code = 'BOX-1G-OLD'), 1),
(@switch_dim_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRE-14-2'), 25),
(@switch_dim_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRENUT-YEL'), 3),
(@switch_dim_id, (SELECT material_id FROM Materials WHERE material_code = 'PLATE-1G-DEC'), 1);

-- Add components to 3-way switch
INSERT INTO `AssemblyComponents` (`assembly_id`, `material_id`, `quantity`) VALUES
(@switch_3w_id, (SELECT material_id FROM Materials WHERE material_code = 'SWITCH-3W'), 1),
(@switch_3w_id, (SELECT material_id FROM Materials WHERE material_code = 'BOX-1G-OLD'), 1),
(@switch_3w_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRE-12-3'), 35),
(@switch_3w_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRENUT-YEL'), 4),
(@switch_3w_id, (SELECT material_id FROM Materials WHERE material_code = 'PLATE-1G-DEC'), 1);

-- Create switch variants
INSERT INTO `AssemblyVariants` (`parent_assembly_id`, `variant_assembly_id`, `sort_order`) VALUES
(@switch_sp_id, @switch_sp_id, 0),  -- Default
(@switch_sp_id, @switch_dim_id, 1);

-- Lighting assemblies
INSERT INTO `AssemblyTemplates` (`assembly_code`, `name`, `description`, `category`, `is_default`, `labor_minutes_rough`, `labor_minutes_finish`) VALUES
('hh', '4" LED Recessed Light', 'LED recessed light installation', 'Lighting', TRUE, 20, 15),
('ex', 'Bathroom Exhaust Fan', 'Exhaust fan installation', 'Lighting', TRUE, 45, 30);

-- Get lighting assembly IDs
SET @light_hh_id = (SELECT assembly_id FROM AssemblyTemplates WHERE assembly_code = 'hh');
SET @fan_ex_id = (SELECT assembly_id FROM AssemblyTemplates WHERE assembly_code = 'ex');

-- Add components to recessed light
INSERT INTO `AssemblyComponents` (`assembly_id`, `material_id`, `quantity`) VALUES
(@light_hh_id, (SELECT material_id FROM Materials WHERE material_code = 'CAN-4-LED'), 1),
(@light_hh_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRE-14-2'), 25),
(@light_hh_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRENUT-YEL'), 3);

-- Add components to exhaust fan
INSERT INTO `AssemblyComponents` (`assembly_id`, `material_id`, `quantity`) VALUES
(@fan_ex_id, (SELECT material_id FROM Materials WHERE material_code = 'FAN-BATH'), 1),
(@fan_ex_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRE-14-2'), 30),
(@fan_ex_id, (SELECT material_id FROM Materials WHERE material_code = 'WIRENUT-YEL'), 4);

-- Add some common quick codes to PriceList for backward compatibility
UPDATE PriceList SET quick_code = 'o' WHERE name LIKE '%outlet%' AND quick_code IS NULL;
UPDATE PriceList SET quick_code = 's' WHERE name LIKE '%switch%' AND name LIKE '%single%' AND quick_code IS NULL;
UPDATE PriceList SET quick_code = '3w' WHERE name LIKE '%3%way%' AND quick_code IS NULL;
UPDATE PriceList SET quick_code = 'hh' WHERE name LIKE '%recessed%' AND quick_code IS NULL;
UPDATE PriceList SET quick_code = 'gfi' WHERE name LIKE '%gfci%' OR name LIKE '%gfi%' AND quick_code IS NULL;

-- Show what we created
SELECT 'Assembly Summary:' AS Report;
SELECT 
    assembly_code AS Code, 
    COUNT(*) AS Variants,
    GROUP_CONCAT(name ORDER BY is_default DESC SEPARATOR ' | ') AS Assembly_Names
FROM AssemblyTemplates
GROUP BY assembly_code;

SELECT 'Total assemblies created:' AS Report, COUNT(*) AS Count FROM AssemblyTemplates;
SELECT 'Total materials created:' AS Report, COUNT(*) AS Count FROM Materials;
