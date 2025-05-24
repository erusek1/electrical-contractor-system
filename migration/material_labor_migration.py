#!/usr/bin/env python3
# material_labor_migration.py
# Script to import material and labor entries from ERE.xlsx to the MySQL database

import pandas as pd
import mysql.connector
from datetime import datetime
import os
import sys

def main():
    print("Electrical Contractor System - Material and Labor Migration")
    print("=========================================================")
    
    # Configuration - Update these settings
    db_config = {
        "host": "localhost",
        "user": "root",
        "password": "215Osborn",
        "database": "electrical_contractor_db"
    }
    
    excel_file = "ERE.xlsx"
    sheet_name = "Material and Labor"  # Adjust if your sheet name is different
    
    # Verify file exists
    if not os.path.exists(excel_file):
        print(f"Error: File {excel_file} not found.")
        print(f"Please place {excel_file} in the same directory as this script.")
        sys.exit(1)
    
    try:
        # Connect to the database
        print(f"Connecting to MySQL database...")
        conn = mysql.connector.connect(**db_config)
        cursor = conn.cursor()
        
        # Read the Excel file
        print(f"Reading {excel_file}, sheet '{sheet_name}'...")
        ml_df = pd.read_excel(excel_file, sheet_name=sheet_name)
        
        print(f"Found {len(ml_df)} records.")
        
        # Get job mapping (job_number -> job_id)
        cursor.execute("SELECT job_id, job_number FROM Jobs")
        job_map = {str(job_number): job_id for job_id, job_number in cursor.fetchall()}
        print(f"Found {len(job_map)} jobs in database.")
        
        # Get employee mapping (name -> employee_id)
        cursor.execute("SELECT employee_id, name FROM Employees")
        employee_map = {name.lower(): employee_id for employee_id, name in cursor.fetchall()}
        print(f"Found {len(employee_map)} employees in database.")
        
        # Get vendor mapping (name -> vendor_id)
        cursor.execute("SELECT vendor_id, name FROM Vendors")
        vendor_map = {name.lower(): vendor_id for vendor_id, name in cursor.fetchall()}
        print(f"Found {len(vendor_map)} vendors in database.")
        
        # Track job stages to avoid recreating them
        stages_map = {}  # format: "job_id_stage_name" -> stage_id
        
        # Initialize counters
        labor_entries_added = 0
        material_entries_added = 0
        stages_added = 0
        vendors_added = 0
        errors = 0
        
        # Process each row
        for index, row in ml_df.iterrows():
            try:
                # Skip rows without job number
                if 'Job number' not in row or pd.isna(row['Job number']):
                    print(f"Skipping row {index}: No job number")
                    continue
                
                job_number = str(row['Job number']).strip()
                
                # Skip if job doesn't exist in database
                if job_number not in job_map:
                    print(f"Skipping row {index}: Job {job_number} not found in database")
                    continue
                
                job_id = job_map[job_number]
                
                # Get or create stage
                stage_name = str(row['Stage']) if 'Stage' in row and pd.notna(row['Stage']) else 'Other'
                stage_key = f"{job_id}_{stage_name}"
                
                if stage_key not in stages_map:
                    # Check if stage exists
                    cursor.execute(
                        "SELECT stage_id FROM JobStages WHERE job_id = %s AND stage_name = %s",
                        (job_id, stage_name)
                    )
                    stage_result = cursor.fetchone()
                    
                    if stage_result:
                        stages_map[stage_key] = stage_result[0]
                    else:
                        # Create new stage
                        cursor.execute(
                            "INSERT INTO JobStages (job_id, stage_name) VALUES (%s, %s)",
                            (job_id, stage_name)
                        )
                        conn.commit()
                        stages_map[stage_key] = cursor.lastrowid
                        stages_added += 1
                
                stage_id = stages_map[stage_key]
                
                # Get date
                entry_date = row['Date'] if 'Date' in row and pd.notna(row['Date']) else datetime.now().date()
                
                # Process labor entry if hours exist
                if 'Hours' in row and pd.notna(row['Hours']) and float(row['Hours']) > 0:
                    if 'Employee' not in row or pd.isna(row['Employee']):
                        print(f"Warning: Row {index} has hours but no employee specified")
                    else:
                        employee_name = str(row['Employee']).lower().strip()
                        
                        # Find employee ID
                        employee_id = None
                        if employee_name in employee_map:
                            employee_id = employee_map[employee_name]
                        else:
                            print(f"Warning: Employee '{row['Employee']}' not found in database")
                            continue
                        
                        if employee_id:
                            # Insert labor entry
                            cursor.execute(
                                """INSERT INTO LaborEntries 
                                   (job_id, employee_id, stage_id, date, hours) 
                                   VALUES (%s, %s, %s, %s, %s)""",
                                (job_id, employee_id, stage_id, entry_date, float(row['Hours']))
                            )
                            conn.commit()
                            labor_entries_added += 1
                
                # Process material entry if cost exists
                if 'Cost' in row and pd.notna(row['Cost']) and float(row['Cost']) > 0:
                    # Handle vendor
                    vendor_id = None
                    if 'Vendor' in row and pd.notna(row['Vendor']):
                        vendor_name = str(row['Vendor']).lower().strip()
                        
                        # Find or create vendor
                        if vendor_name in vendor_map:
                            vendor_id = vendor_map[vendor_name]
                        else:
                            # Create new vendor
                            cursor.execute(
                                "INSERT INTO Vendors (name) VALUES (%s)",
                                (row['Vendor'],)
                            )
                            conn.commit()
                            vendor_id = cursor.lastrowid
                            vendor_map[vendor_name] = vendor_id
                            vendors_added += 1
                    else:
                        # Use default vendor if none specified
                        vendor_id = next(iter(vendor_map.values())) if vendor_map else None
                    
                    if vendor_id:
                        # Get invoice info
                        invoice_number = str(row['Invoice #']) if 'Invoice #' in row and pd.notna(row['Invoice #']) else None
                        invoice_total = float(row['Invoice Total']) if 'Invoice Total' in row and pd.notna(row['Invoice Total']) else None
                        notes = str(row['Notes']) if 'Notes' in row and pd.notna(row['Notes']) else None
                        
                        # Insert material entry
                        cursor.execute(
                            """INSERT INTO MaterialEntries 
                               (job_id, stage_id, vendor_id, date, cost, invoice_number, invoice_total, notes) 
                               VALUES (%s, %s, %s, %s, %s, %s, %s, %s)""",
                            (
                                job_id, stage_id, vendor_id, entry_date, 
                                float(row['Cost']), invoice_number, invoice_total, notes
                            )
                        )
                        conn.commit()
                        material_entries_added += 1
                
            except Exception as e:
                print(f"Error processing row {index}: {e}")
                errors += 1
        
        print("\nMigration Summary:")
        print(f"Labor entries added: {labor_entries_added}")
        print(f"Material entries added: {material_entries_added}")
        print(f"Job stages created: {stages_added}")
        print(f"Vendors added: {vendors_added}")
        print(f"Errors encountered: {errors}")
        print("Migration completed!")
        
    except mysql.connector.Error as err:
        print(f"Database error: {err}")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)
    finally:
        if 'conn' in locals() and conn.is_connected():
            cursor.close()
            conn.close()
            print("Database connection closed.")

if __name__ == "__main__":
    main()
