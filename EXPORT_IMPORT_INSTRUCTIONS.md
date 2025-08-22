# Asset Export/Import Instructions

## Overview
This document explains how to export all assets, delete them, and then re-import the exported data.

## Prerequisites
- You must be logged in as an Admin user
- The application must be running at `http://localhost:5147`

## Step-by-Step Process

### Step 1: Export All Assets
1. Navigate to the Assets page: `http://localhost:5147/Assets`
2. Look for the "Export All" button in the top toolbar (blue button with Excel icon)
3. Click the "Export All" button
4. The system will download an Excel file named `All_Assets_Export_YYYYMMDD_HHMMSS.xlsx`
5. Save this file to a safe location on your computer

### Step 2: Delete All Assets
1. On the same Assets page, look for the "Delete All" button (red button with trash icon)
2. Click the "Delete All" button
3. You will see multiple confirmation dialogs:
   - First warning about permanent deletion
   - Second confirmation dialog
   - Final prompt asking you to type "DELETE ALL"
4. Type "DELETE ALL" exactly as shown and click OK
5. The system will delete all assets and related records
6. The page will automatically reload showing no assets

### Step 3: Re-import the Exported Data
1. Navigate to the Import page: `http://localhost:5147/Assets/Import`
2. Click "Choose File" and select the Excel file you exported in Step 1
3. Click "Upload and Preview" to see the data
4. Review the preview to ensure all data looks correct
5. Click "Import Assets" to complete the import
6. The system will import all the assets back into the database

## Important Notes

### Security
- Only Admin users can access the "Delete All" functionality
- Admin and IT users can access the "Export All" functionality
- The delete operation requires multiple confirmations to prevent accidental deletion

### Data Integrity
- The export includes ALL asset fields including audit information
- The delete operation removes:
  - All assets
  - All asset history records
  - All asset requests
- The import will recreate all assets with their original data

### File Format
- The exported file is in Excel format (.xlsx)
- The file contains all asset columns in a standardized format
- Date fields are formatted as MM/dd/yyyy
- Timestamps include both date and time

### Backup Recommendation
- Always keep a backup of the exported file before proceeding with deletion
- Consider testing the process with a small dataset first
- The exported file serves as your backup - don't delete it until you've verified the re-import worked

## Troubleshooting

### If the export fails:
- Check that you have Admin or IT permissions
- Ensure the application is running properly
- Check the browser console for any JavaScript errors

### If the delete fails:
- Ensure you're logged in as an Admin user
- Check that you typed "DELETE ALL" exactly as required
- Look for any error messages in the toast notifications

### If the import fails:
- Check that the Excel file format is correct
- Ensure all required fields are present
- Review any error messages in the import preview
- The import service will show detailed error information

## Alternative Methods

### Using the existing Export functionality:
- You can also use the "Export" button (green button) to export only visible columns
- This is useful if you want to export specific columns only

### Using the existing Import functionality:
- You can import any Excel file that matches the expected format
- The import service will validate the data before importing

## Support
If you encounter any issues, check the application logs for detailed error information.
