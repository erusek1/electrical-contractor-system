# Database Scripts Differences Analysis

## Comparison: GitHub vs Local Versions

### 1. add_pricing_tables.sql

#### Key Differences:

**GitHub Version:**
- Materials table: `tax_rate` default is 6.4
- Has `preferred_vendor_id` field
- Labor adjustment columns named differently (e.g., `rough_minutes` vs `labor_minutes_rough`)
- Has 15 difficulty presets
- Includes views: `vw_current_material_prices` and `vw_assembly_costs`

**Local Version:**
- Materials table: `tax_rate` default is 0.064 (6.4%)
- Has `supplier` field instead of `preferred_vendor_id`
- Labor columns named `labor_minutes_rough`, `labor_minutes_finish`, etc.
- Has 24 difficulty presets (more comprehensive)
- Includes additional views: `vw_assembly_with_variants` and `vw_material_price_changes`
- Has `AssemblyUsageStats` table for tracking usage

#### Missing in GitHub:
- More detailed difficulty presets for:
  - Occupied Home - Minimal
  - Full Gut Renovation
  - Multi-Story 3rd Floor+
  - Finished Basement
  - Construction Site - Multiple Trades
  - Permit/Inspection Required
  - Insurance Job

### 2. New Files Not in GitHub:

#### add_integration_columns.sql
This file adds critical integration between estimates and jobs:
- Adds `estimate_id` to Jobs table
- Adds `assembly_id` to EstimateLineItems
- Adds `service_type_id` to Estimates and Jobs
- Creates `EstimateConversions` table to track conversions
- Adds distance and drive time tracking
- Creates `AssemblyUsageStats` table
- Adds `quick_code` support to PriceList

#### populate_sample_assemblies.sql
Provides sample data for common assemblies:
- Materials: outlets, switches, wire, boxes, plates, etc.
- Assembly templates for codes: o, s, 3w, hh, gfi, ex
- Links assemblies to their component materials
- Creates assembly variants (e.g., Decora outlet vs Duplex)

#### run_all_updates.sql
Master script to run everything in correct order:
- Runs all scripts sequentially
- Verifies tables were created
- Shows counts of default data

#### README_PRICING_UPDATES.md
Complete documentation for the pricing system updates

### 3. Features Missing from GitHub Implementation:

1. **Estimate to Job Integration**
   - No tracking of which estimates became jobs
   - No conversion metrics
   - No source estimate reference in jobs

2. **Service Type Integration**
   - Service types not linked to estimates/jobs
   - No minimum hours by service type
   - No drive time calculations

3. **Assembly Usage Tracking**
   - No statistics on which assemblies are used most
   - No tracking of labor adjustments per assembly

4. **Quick Code Support**
   - PriceList doesn't have quick_code field
   - No backward compatibility with Excel codes

5. **Distance/Drive Time**
   - Jobs don't track distance or drive time
   - No automatic calculation based on distance

## Recommendations:

1. **Critical Additions Needed:**
   - Run `add_integration_columns.sql` to link estimates with jobs
   - Add the missing difficulty presets
   - Implement assembly usage tracking

2. **Data Population:**
   - Run `populate_sample_assemblies.sql` to add common assemblies
   - Import your Excel price list with quick codes

3. **Integration Improvements:**
   - Link service types to estimates and jobs
   - Track estimate conversion rates
   - Add distance-based pricing calculations