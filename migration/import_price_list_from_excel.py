"""
Import Price List and Assemblies from Excel
This script imports materials and assemblies from your Excel template into the database

Usage: python import_price_list_from_excel.py [excel_file]
"""

import pandas as pd
import mysql.connector
import re
import sys
from datetime import datetime

# Database configuration
DB_CONFIG = {
    'host': 'localhost',
    'user': 'your_username',
    'password': 'your_password',
    'database': 'electrical_contractor_db'
}

def parse_formula(formula_str, cell_values):
    """Parse Excel formula to extract component references"""
    if not formula_str or not formula_str.startswith('='):
        return []
    
    # Extract cell references like G211, G186, etc.
    cell_refs = re.findall(r'G(\d+)', formula_str)
    components = []
    
    for ref in cell_refs:
        row_num = int(ref)
        if row_num in cell_values:
            # Check for quantity multipliers like (4*G184)
            pattern = rf'\((\d+)\*G{ref}\)'
            match = re.search(pattern, formula_str)
            quantity = int(match.group(1)) if match else 1
            
            components.append({
                'row': row_num,
                'quantity': quantity,
                'value': cell_values[row_num]
            })
    
    return components

def import_materials(df, cursor):
    """Import raw materials from the price list"""
    print("Importing materials...")
    
    materials_imported = 0
    material_map = {}  # Map row numbers to material IDs
    
    for index, row in df.iterrows():
        # Skip if no name in column D
        if pd.isna(row['D']) or not str(row['D']).strip():
            continue
        
        # Skip assembly items (those with formulas in columns I or J)
        if pd.notna(row.get('I')) and str(row['I']).startswith('='):
            continue
        if pd.notna(row.get('J')) and str(row['J']).startswith('='):
            continue
        
        material_code = str(row['C']) if pd.notna(row['C']) else f"MAT-{index}"
        name = str(row['D'])
        base_price = float(row['E']) if pd.notna(row['E']) else 0
        tax_amount = float(row['F']) if pd.notna(row['F']) else 0
        
        # Determine category from column A or B
        category = str(row['A']) if pd.notna(row['A']) else 'General'
        if category in ['', 'nan']:
            category = str(row['B']) if pd.notna(row['B']) else 'General'
        
        # Determine unit of measure from name
        unit_of_measure = 'Each'
        if 'per foot' in name.lower() or '/ft' in name.lower():
            unit_of_measure = 'Foot'
        elif 'per 250' in name.lower():
            unit_of_measure = 'Per 250ft'
        
        try:
            cursor.execute("""
                INSERT INTO Materials 
                (material_code, name, category, unit_of_measure, current_price, tax_rate, created_by)
                VALUES (%s, %s, %s, %s, %s, %s, %s)
                ON DUPLICATE KEY UPDATE
                current_price = VALUES(current_price),
                updated_date = NOW()
            """, (material_code, name, category, unit_of_measure, base_price, 6.4, 'Excel Import'))
            
            if cursor.lastrowid:
                material_map[index + 2] = cursor.lastrowid  # Excel rows start at 1, pandas at 0
                materials_imported += 1
        except Exception as e:
            print(f"Error importing material {name}: {e}")
    
    print(f"Imported {materials_imported} materials")
    return material_map

def import_assemblies(df, cursor, material_map):
    """Import assemblies with their components"""
    print("\nImporting assemblies...")
    
    # Build cell value map for G column
    cell_values = {}
    for index, row in df.iterrows():
        if pd.notna(row.get('G')):
            cell_values[index + 2] = float(row['G'])  # Excel rows start at 1
    
    assemblies_imported = 0
    
    for index, row in df.iterrows():
        # Look for assemblies (items with code in C and formulas in I or J)
        if pd.isna(row['C']) or pd.isna(row['D']):
            continue
        
        # Check if this is an assembly (has formula in I or J)
        has_material_formula = pd.notna(row.get('I')) and str(row['I']).startswith('=')
        has_labor_formula = pd.notna(row.get('J')) and str(row['J']).startswith('=')
        
        if not (has_material_formula or has_labor_formula):
            continue
        
        assembly_code = str(row['C'])
        name = str(row['D'])
        description = name
        
        # Get labor minutes from columns M-P
        rough_minutes = int(row['M']) if pd.notna(row.get('M')) else 0
        finish_minutes = int(row['N']) if pd.notna(row.get('N')) else 0
        service_minutes = int(row['O']) if pd.notna(row.get('O')) else 0
        extra_minutes = int(row['P']) if pd.notna(row.get('P')) else 0
        
        # Determine category
        category = str(row['A']) if pd.notna(row['A']) else 'General'
        if category in ['', 'nan', 'receptacles', 'switches', 'lighting']:
            category = 'Electrical'
        
        # Determine if this is a default variant
        is_default = True
        if assembly_code in ['old-o']:  # Non-default variants
            is_default = False
        
        try:
            # Insert assembly
            cursor.execute("""
                INSERT INTO AssemblyTemplates 
                (assembly_code, name, description, category, rough_minutes, finish_minutes, 
                 service_minutes, extra_minutes, is_default, is_active, created_by)
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
            """, (assembly_code, name, description, category, rough_minutes, finish_minutes,
                  service_minutes, extra_minutes, is_default, True, 'Excel Import'))
            
            assembly_id = cursor.lastrowid
            
            if assembly_id:
                assemblies_imported += 1
                
                # Parse material formula to get components
                if has_material_formula:
                    components = parse_formula(str(row['I']), cell_values)
                    
                    for comp in components:
                        # Find the material ID for this component
                        if comp['row'] in material_map:
                            material_id = material_map[comp['row']]
                            
                            cursor.execute("""
                                INSERT INTO AssemblyComponents 
                                (assembly_id, material_id, quantity)
                                VALUES (%s, %s, %s)
                            """, (assembly_id, material_id, comp['quantity']))
                
                print(f"Imported assembly: {assembly_code} - {name}")
                
        except Exception as e:
            print(f"Error importing assembly {assembly_code}: {e}")
    
    print(f"Imported {assemblies_imported} assemblies")

def main():
    # Get Excel file path
    excel_file = sys.argv[1] if len(sys.argv) > 1 else 'template 3.xlsx'
    
    print(f"Reading Excel file: {excel_file}")
    
    try:
        # Read the Price List sheet
        df = pd.read_excel(excel_file, sheet_name='Price List', header=None)
        
        # Rename columns to A, B, C, etc. for easier reference
        df.columns = [chr(65 + i) for i in range(len(df.columns))]
        
        # Connect to database
        conn = mysql.connector.connect(**DB_CONFIG)
        cursor = conn.cursor()
        
        print("Connected to database")
        
        # Import materials first
        material_map = import_materials(df, cursor)
        
        # Import assemblies
        import_assemblies(df, cursor, material_map)
        
        # Create price history records for imported materials
        print("\nCreating initial price history records...")
        cursor.execute("""
            INSERT INTO MaterialPriceHistory (material_id, price, effective_date, created_by)
            SELECT material_id, current_price, NOW(), 'Initial Import'
            FROM Materials
            WHERE created_by = 'Excel Import'
            AND material_id NOT IN (SELECT DISTINCT material_id FROM MaterialPriceHistory)
        """)
        
        # Commit changes
        conn.commit()
        print("\nImport completed successfully!")
        
        # Show summary
        cursor.execute("SELECT COUNT(*) FROM Materials WHERE created_by = 'Excel Import'")
        material_count = cursor.fetchone()[0]
        
        cursor.execute("SELECT COUNT(*) FROM AssemblyTemplates WHERE created_by = 'Excel Import'")
        assembly_count = cursor.fetchone()[0]
        
        print(f"\nSummary:")
        print(f"- Total materials in database: {material_count}")
        print(f"- Total assemblies in database: {assembly_count}")
        
    except Exception as e:
        print(f"Error: {e}")
        if 'conn' in locals():
            conn.rollback()
    finally:
        if 'cursor' in locals():
            cursor.close()
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    main()
