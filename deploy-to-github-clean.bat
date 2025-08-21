@echo off
echo ========================================
echo Asset Management - Clean Deploy to GitHub
echo ========================================
echo.

REM Check if git is installed
git --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Git is not installed or not in PATH
    echo Please install Git from https://git-scm.com/
    pause
    exit /b 1
)

echo WARNING: This will completely reset the git history to remove large files.
echo This is necessary because GitHub has a 100MB file size limit.
echo.
set /p confirm="Are you sure you want to continue? (y/N): "
if /i not "%confirm%"=="y" (
    echo Operation cancelled.
    pause
    exit /b 0
)

echo.
echo Cleaning git repository...

REM Remove the .git directory to start fresh
if exist ".git" (
    echo Removing existing git repository...
    rmdir /s /q ".git"
)

REM Initialize a new git repository
echo Initializing new git repository...
git init

REM Add .gitignore first
echo Adding .gitignore...
git add .gitignore
git commit -m "Add .gitignore to exclude build artifacts and log files"

REM Add all other files (excluding those in .gitignore)
echo Adding project files...
git add .

REM Commit all changes
echo.
echo Committing project files...
git commit -m "Asset Management System - Complete Application

- ASP.NET Core 9.0 MVC application
- Entity Framework Core with SQL Server
- Excel import functionality with preview and error handling
- Role-based authentication
- Asset management with CRUD operations
- Bootstrap 5 UI with responsive design
- Session-based file handling
- Audit logging
- Enhanced error handling and review workflow
- Customizable column settings and pagination
- Excel export with visible columns only
- Horizontal table navigation
- Bulk operations and soft delete functionality"

REM Add remote origin
echo.
echo Setting up remote repository...
git remote add origin https://github.com/ZetallicA/AssetAugust.git

REM Push to GitHub
echo.
echo Pushing to GitHub...
git branch -M main
git push -u origin main --force

if errorlevel 1 (
    echo.
    echo ERROR: Failed to push to GitHub.
    echo This might be due to:
    echo 1. Large files still present
    echo 2. Network issues
    echo 3. Repository access issues
    echo.
    echo Please check the error message above.
) else (
    echo.
    echo ========================================
    echo Deployment completed successfully!
    echo ========================================
    echo.
    echo Your project is now available at:
    echo https://github.com/ZetallicA/AssetAugust
)

echo.
pause
