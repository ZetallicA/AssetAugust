@echo off
echo ========================================
echo Asset Management - Deploy to GitHub
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

REM Initialize git repository if not already done
if not exist ".git" (
    echo Initializing git repository...
    git init
    echo.
)

REM Remove large log files from git tracking if they exist
echo Checking for large files...
git rm --cached "AssetManagement.Web/Logs/*.txt" 2>nul
git rm --cached "AssetManagement.Web/Logs/log-*.txt" 2>nul

REM Add all files (excluding those in .gitignore)
echo Adding files to git...
git add .

REM Commit changes
echo.
echo Committing changes...
git commit -m "Asset Management System - Initial commit

- ASP.NET Core 9.0 MVC application
- Entity Framework Core with SQL Server
- Excel import functionality with preview
- Role-based authentication
- Asset management with CRUD operations
- Bootstrap 5 UI
- Session-based file handling
- Audit logging
- Enhanced error handling and review workflow
- Customizable column settings and pagination
- Excel export with visible columns only"

REM Add remote origin if not exists
echo.
echo Checking remote repository...
git remote -v | findstr "origin" >nul 2>&1
if errorlevel 1 (
    echo Adding remote origin...
    git remote add origin https://github.com/ZetallicA/AssetAugust.git
)

REM Push to GitHub
echo.
echo Pushing to GitHub...
git branch -M main
git push -u origin main

echo.
echo ========================================
echo Deployment completed successfully!
echo ========================================
echo.
echo Your project is now available at:
echo https://github.com/ZetallicA/AssetAugust
echo.
pause
