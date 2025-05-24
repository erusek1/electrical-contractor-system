#!/usr/bin/env python3
# job_sheet_migration.py
# Script to import individual job sheets (e.g., 619.xlsx) to the MySQL database

import pandas as pd
import mysql.connector
import os
import sys
import glob

def main():
    print("Electrical Contractor System - Job Sheet Migration")
    print("=================================================")
    
    # Configuration - Update these settings
    db_config = {
        "host": "localhost",
        "user": "root",
        "password": "215Osborn",
        "database": "electrical_contractor_db"
    }
    
    job_sheets_dir = "job_sheets"  # Directory containing individual job sheets
    
    # Verify directory exists
    if not os.path.exists(job_sheets_dir):
        print(f"Error: Directory {job_sheets_dir} not found.")
        print(f"Please create the directory and place job sheet Excel files in it.")
        sys.exit(1)
    
    try:
        # Connect to the database
        print(f"Connecting to MySQL database...")
        conn = mysql.connector.connect(**db_config)
        cursor = conn.cursor()
        
        # Get job mapping (job_number -> job_id)
        cursor.execute("SELECT job_id, job_number FROM Jobs")
        job_map = {str(job_number): job_id for job_id, job_number in cursor.fetchall()}
        print(f"Found {len(job_map)} jobs in database.")
        
        # Find job sheet files in the directory
        job_files = glob.glob(os.path.join(job_sheets_dir, "*.xlsx"))
        print(f"Found {len(job_files)} job sheet files.")
        
        if not job_files:
            print("No job sheet files found. Please add Excel files to the job_sheets directory.")
            sys.exit(0)
        
        # Initialize counters
        jobs_processed = 0
        stages_updated = 0
        room_specs_added = 0
        permit_items_added = 0
        errors = 0
        
        # Process each job file
        for file_path in job_files:
            file_name = os.path.basename(file_path)
            job_number = os.path.splitext(file_name)[0]  # Remove extension to get job number
            
            print(f"\nProcessing job {job_number} from file {file_name}...")
            
            # Skip if job doesn't exist in database
            if job_number not in job_map:
                print(f"Job {job_number} not found in database, skipping.")
                errors += 1
                continue
            
            job_id = job_map[job_number]
            
            try:
                # Read the Estimate sheet
                try:
                    estimate_df = pd.read_excel(file_path, sheet_name='Estimate')
                    print(f"Read Estimate sheet.")
                except Exception as e:
                    print(f"Warning: Could not read Estimate sheet: {e}")
                    estimate_df = None
                
                # Update job with square footage and floors if available
                if estimate_df is not None:
                    try:
                        # Look for square footage row
                        sq_ft_rows = estimate_df[estimate_df.iloc[:, 0].str.contains('Square footage', na=False)]
                        if not sq_ft_rows.empty:
                            sq_ft_row = sq_ft_rows.iloc[0]
                            sq_ft_value = None
                            # Try to find square footage in this row (might be in different columns)
                            for col in sq_ft_row.index:
                                if pd.notna(sq_ft_row[col]) and isinstance(sq_ft_row[col], (int, float)):
                                    sq_ft_value = int(sq_ft_row[col])
                                    break
                            
                            if sq_ft_value:
                                cursor.execute(
                                    "UPDATE Jobs SET square_footage = %s WHERE job_id = %s",
                                    (sq_ft_value, job_id)
                                )
                                conn.commit()
                                print(f"Updated job with square footage: {sq_ft_value}")
                        
                        # Look for floors row
                        floors_rows = estimate_df[estimate_df.iloc[:, 0].str.contains('floors', na=False, case=False)]
                        if not floors_rows.empty:
                            floors_row = floors_rows.iloc[0]
                            floors_value = None
                            # Try to find floors in this row
                            for col in floors_row.index:
                                if pd.notna(floors_row[col]) and isinstance(floors_row[col], (int, float)):
                                    floors_value = int(floors_row[col])
                                    break
                            
                            if floors_value:
                                cursor.execute(
                                    "UPDATE Jobs SET num_floors = %s WHERE job_id = %s",
                                    (floors_value, job_id)
                                )
                                conn.commit()
                                print(f"Updated job with number of floors: {floors_value}")
                                
                    except Exception as e:
                        print(f"Error updating job details: {e}")
                
                # Process stages data
                if estimate_df is not None:
                    stages = ['Demo', 'Inspection', 'Temp Service', 'Rough', 'Service', 'Finish', 'Extra']
                    
                    for stage in stages:
                        try:
                            # Find rows matching this stage
                            stage_rows = estimate_df[estimate_df.iloc[:, 0].str.contains(f'^{stage}$', na=False, regex=True)]
                            
                            if stage_rows.empty:
                                continue
                            
                            estimated_hours = 0
                            actual_hours = 0
                            estimated_material = 0
                            actual_material = 0
                            
                            # First row should be hours
                            stage_row = stage_rows.iloc[0]
                            if 'Estimated' in stage_row and pd.notna(stage_row['Estimated']):
                                estimated_hours = float(stage_row['Estimated'])
                            if 'Actual' in stage_row and pd.notna(stage_row['Actual']):
                                actual_hours = float(stage_row['Actual'])
                            
                            # Look for material row (typically stage + " Material")
                            material_rows = estimate_df[estimate_df.iloc[:, 0].str.contains(f'^{stage} Material$', na=False, regex=True)]
                            if not material_rows.empty:
                                material_row = material_rows.iloc[0]
                                if 'Estimated' in material_row and pd.notna(material_row['Estimated']):
                                    estimated_material = float(material_row['Estimated'])
                                if 'Actual' in material_row and pd.notna(material_row['Actual']):
                                    actual_material = float(material_row['Actual'])
                            
                            # Check if stage exists
                            cursor.execute(
                                "SELECT stage_id FROM JobStages WHERE job_id = %s AND stage_name = %s",
                                (job_id, stage)
                            )
                            result = cursor.fetchone()
                            
                            if result:
                                # Update existing stage
                                cursor.execute(
                                    """UPDATE JobStages SET 
                                       estimated_hours = %s, actual_hours = %s,
                                       estimated_material_cost = %s, actual_material_cost = %s
                                       WHERE stage_id = %s""",
                                    (estimated_hours, actual_hours, estimated_material, actual_material, result[0])
                                )
                                stages_updated += 1
                            else:
                                # Create new stage
                                cursor.execute(
                                    """INSERT INTO JobStages 
                                       (job_id, stage_name, estimated_hours, actual_hours,
                                        estimated_material_cost, actual_material_cost)
                                       VALUES (%s, %s, %s, %s, %s, %s)""",
                                    (job_id, stage, estimated_hours, actual_hours, 
                                     estimated_material, actual_material)
                                )
                                stages_updated += 1
                                
                            conn.commit()
                            print(f"Processed stage: {stage}")
                            
                        except Exception as e:
                            print(f"Error processing stage {stage}: {e}")
                            errors += 1
                
                # Process room specifications
                try:
                    template_df = pd.read_excel(file_path, sheet_name='Template')
                    print(f"Read Template sheet for room specifications.")
                    
                    # Delete existing room specifications for this job
                    cursor.execute("DELETE FROM RoomSpecifications WHERE job_id = %s", (job_id,))
                    conn.commit()
                    
                    current_room = None
                    
                    # Process template rows
                    for index, row in template_df.iterrows():
                        # Skip empty rows
                        if all(pd.isna(val) for val in row):
                            continue
                        
                        # Check if this is a room header row
                        first_col = row.iloc[0] if pd.notna(row.iloc[0]) else ""
                        if isinstance(first_col, str) and first_col.strip() and not first_col.startswith('  '):
                            current_room = first_col.strip()
                            continue
                        
                        # If we have a current room and this looks like an item row
                        if current_room and pd.notna(row.iloc[1]) and pd.notna(row.iloc[2]):
                            # Get quantity from column B
                            try:
                                quantity = int(row.iloc[1]) if pd.notna(row.iloc[1]) else 1
                            except (ValueError, TypeError):
                                quantity = 1
                            
                            # Get item description from column C
                            item_description = str(row.iloc[2]) if pd.notna(row.iloc[2]) else ""
                            
                            # Get item code if available (column name may vary)
                            item_code = None
                            
                            # Get unit price from a pricing column (may vary)
                            unit_price = 0
                            for col_idx in range(3, min(8, len(row))):  # Check reasonable range for price
                                if pd.notna(row.iloc[col_idx]) and isinstance(row.iloc[col_idx], (int, float)):
                                    unit_price = float(row.iloc[col_idx])
                                    break
                            
                            # Insert room specification
                            if item_description:
                                cursor.execute(
                                    """INSERT INTO RoomSpecifications 
                                       (job_id, room_name, item_description, quantity, 
                                        item_code, unit_price, total_price)
                                       VALUES (%s, %s, %s, %s, %s, %s, %s)""",
                                    (
                                        job_id, current_room, item_description, quantity,
                                        item_code, unit_price, quantity * unit_price
                                    )
                                )
                                conn.commit()
                                room_specs_added += 1
                    
                    print(f"Added {room_specs_added} room specifications.")
                    
                except Exception as e:
                    print(f"Warning: Could not process Template sheet: {e}")
                
                # Process permit items
                try:
                    permits_df = pd.read_excel(file_path, sheet_name='Permits')
                    print(f"Read Permits sheet.")
                    
                    # Delete existing permit items for this job
                    cursor.execute("DELETE FROM PermitItems WHERE job_id = %s", (job_id,))
                    conn.commit()
                    
                    # Process permit rows
                    permit_rows_processed = 0
                    for index, row in permits_df.iterrows():
                        # Skip rows without category or quantity
                        if pd.isna(row.iloc[0]) or pd.isna(row.iloc[1]):
                            continue
                        
                        category = str(row.iloc[0]).strip()
                        
                        # Skip header rows or empty categories
                        if not category or category.lower() in ['item', 'description', 'category']:
                            continue
                        
                        try:
                            quantity = int(row.iloc[1]) if pd.notna(row.iloc[1]) else 0
                        except (ValueError, TypeError):
                            quantity = 0
                        
                        # Only add items with quantity > 0
                        if quantity > 0:
                            description = str(row.iloc[2]) if len(row) > 2 and pd.notna(row.iloc[2]) else None
                            
                            cursor.execute(
                                """INSERT INTO PermitItems
                                   (job_id, category, quantity, description)
                                   VALUES (%s, %s, %s, %s)""",
                                (job_id, category, quantity, description)
                            )
                            conn.commit()
                            permit_items_added += 1
                            permit_rows_processed += 1
                    
                    print(f"Added {permit_rows_processed} permit items.")
                    
                except Exception as e:
                    print(f"Warning: Could not process Permits sheet: {e}")
                
                jobs_processed += 1
                print(f"Successfully processed job {job_number}")
                
            except Exception as e:
                print(f"Error processing job file {file_name}: {e}")
                errors += 1
        
        print("\nMigration Summary:")
        print(f"Job sheets processed: {jobs_processed}")
        print(f"Stages created/updated: {stages_updated}")
        print(f"Room specifications added: {room_specs_added}")
        print(f"Permit items added: {permit_items_added}")
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
