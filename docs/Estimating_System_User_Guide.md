# Electrical Contractor Estimating System - User Guide

## Table of Contents
1. [Getting Started](#getting-started)
2. [Creating Estimates](#creating-estimates)
3. [Managing Price Lists](#managing-price-lists)
4. [Keyboard Shortcuts](#keyboard-shortcuts)
5. [Tips and Tricks](#tips-and-tricks)

## Getting Started

### First Time Setup

1. **Import Your Price List**
   - Go to Price List → Import from Excel
   - Select your template3.xlsx file
   - The system will import all items with their labor minutes

2. **Set Your Default Labor Rate**
   - Go to Settings → Preferences
   - Enter your standard hourly rate (default is $85/hour)
   - Set your material markup percentage (default is 22%)

3. **Add Your Customers**
   - Go to Customers → Manage Customers
   - Click "Add New Customer"
   - Enter customer information

## Creating Estimates

### Step 1: Start a New Estimate

1. Click **File → New Estimate** or press `Ctrl+N`
2. Select existing customer or create new one
3. Enter job details:
   - Job name/description
   - Address
   - Square footage (optional)
   - Number of floors (optional)

### Step 2: Add Rooms

1. Click **"Add Room"** button in left panel
2. Double-click room name to rename (e.g., "Kitchen", "Master Bedroom")
3. Rooms can be reordered using the up/down arrows

### Step 3: Add Items to Rooms

#### Method 1: Quick Code Entry
1. In the Price List search box, type the item code:
   - `hh` for recessed lights
   - `O` for outlets
   - `S` for switches
2. Press Enter or click "Add Selected Item"
3. Adjust quantity in the room items grid

#### Method 2: Browse and Select
1. Browse the price list on the right
2. Click to select an item
3. Click "Add Selected Item"
4. Item appears in the current room

#### Method 3: Search by Description
1. Type part of the item name in search box
2. Select from filtered results
3. Add to room

### Step 4: Review and Adjust

- **Change Quantities**: Click in the Qty column and type new number
- **Remove Items**: Click the red X button next to any item
- **Duplicate Rooms**: Select a room and click "Duplicate" (useful for similar bedrooms)
- **View Totals**: Bottom bar shows:
  - Total labor hours
  - Material cost
  - Total estimate amount

### Step 5: Save and Export

1. Click **"Save Estimate"** to save to database
2. Click **"Generate PDF"** to create customer proposal
3. PDF options:
   - Include/exclude pricing
   - Show room-by-room breakdown
   - Add terms and conditions

## Managing Price Lists

### Adding New Items

1. Go to **Price List → Manage Price List**
2. Click **"Add New Item"**
3. Enter:
   - Item code (short code for quick entry)
   - Description
   - Base price (before tax)
   - Category
   - Labor minutes for each stage:
     - Rough minutes
     - Finish minutes
     - Service minutes
     - Extra minutes

### Updating Prices

1. Select items to update
2. Click **"Bulk Price Update"**
3. Enter percentage increase/decrease
4. Preview changes before applying

### Categories

Organize items by category:
- Kitchen
- Lighting  
- General
- Exterior
- Wire
- Ventilation

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Estimate |
| Ctrl+O | Open Estimate |
| Ctrl+S | Save Estimate |
| Ctrl+P | Print/PDF |
| Delete | Remove selected item |
| Ctrl+D | Duplicate selected room |
| F2 | Edit selected cell |
| Tab | Move to next field |
| Ctrl+F | Focus search box |

## Tips and Tricks

### 1. Room Templates

Create templates for common room types:
- **Standard Kitchen**: Save a kitchen with typical items
- **Master Bedroom**: Outlets, switches, ceiling fan
- **Bathroom**: Vanity light, exhaust fan, GFCI

### 2. Quick Estimation Workflow

1. Start with room count and types
2. Use "Duplicate Room" for similar spaces
3. Adjust quantities last
4. Use search to quickly add items by code

### 3. Labor Hour Optimization

- System automatically calculates labor based on:
  - Item quantity × labor minutes ÷ 60
  - Grouped by stage (Rough, Finish, etc.)
- Review stage totals to plan crew assignments

### 4. Profit Tracking

- System tracks estimated vs actual (when job is complete)
- Material markup is applied automatically
- Labor rate × hours + marked up materials = total

### 5. Common Item Codes Reference

**Kitchen**
- `fridge` - Refrigerator outlet
- `micro` - Microwave outlet
- `dw` - Dishwasher outlet
- `hood` - Range hood outlet
- `oven` - 240V oven circuit
- `cook` - 240V cooktop circuit

**Lighting**
- `hh` - 4" recessed light
- `pend` - Pendant light
- `Sc` - Wall sconce
- `Van` - Vanity light

**General**
- `O` - Decora outlet
- `S` - Single pole switch
- `3W` - 3-way switch
- `Dim` - Dimmer switch
- `Gfi` - GFCI outlet

**Exterior**
- `ARL` - Outdoor GFCI with cover

### 6. Estimate Versions

- Each time you revise an estimate, save as new version
- System tracks version history
- Customer sees clean version number
- You can compare versions to see changes

## Troubleshooting

### Problem: Can't find an item code
**Solution**: Use the search box to search by description instead

### Problem: Labor hours seem wrong
**Solution**: Check the labor minutes in the price list for that item

### Problem: PDF won't generate
**Solution**: Make sure estimate is saved first, check printer drivers

### Problem: Database connection error
**Solution**: 
1. Check MySQL service is running
2. Verify connection string in App.config
3. Test username/password in MySQL Workbench

## Getting Help

- Email: erik@erikrusekelectric.com
- Check for updates: Help → Check for Updates
- Report bugs: Help → Report Issue