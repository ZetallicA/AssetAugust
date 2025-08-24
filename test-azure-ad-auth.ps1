# Azure AD Authentication Test Script
# This script helps test and validate your Azure AD authentication setup

param(
    [string]$AppUrl = "https://localhost:7001",
    [string]$ConfigFile = "AssetManagement.Web/appsettings.json"
)

Write-Host "=== Azure AD Authentication Test Script ===" -ForegroundColor Green
Write-Host ""

# Check if configuration file exists
if (Test-Path $ConfigFile) {
    Write-Host "✓ Configuration file found: $ConfigFile" -ForegroundColor Green
} else {
    Write-Host "✗ Configuration file not found: $ConfigFile" -ForegroundColor Red
    exit 1
}

# Read and validate Azure AD configuration
try {
    $config = Get-Content $ConfigFile | ConvertFrom-Json
    
    if ($config.AzureAd) {
        Write-Host "✓ Azure AD configuration found" -ForegroundColor Green
        
        # Check required fields
        $requiredFields = @("Instance", "TenantId", "ClientId", "ClientSecret", "CallbackPath", "SignedOutCallbackPath")
        $missingFields = @()
        
        foreach ($field in $requiredFields) {
            if (-not $config.AzureAd.$field) {
                $missingFields += $field
            }
        }
        
        if ($missingFields.Count -eq 0) {
            Write-Host "✓ All required Azure AD configuration fields present" -ForegroundColor Green
        } else {
            Write-Host "✗ Missing required fields: $($missingFields -join ', ')" -ForegroundColor Red
        }
        
        # Validate Tenant ID format
        if ($config.AzureAd.TenantId -match "^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$") {
            Write-Host "✓ Tenant ID format is valid" -ForegroundColor Green
        } else {
            Write-Host "✗ Tenant ID format appears invalid" -ForegroundColor Yellow
        }
        
        # Validate Client ID format
        if ($config.AzureAd.ClientId -match "^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$") {
            Write-Host "✓ Client ID format is valid" -ForegroundColor Green
        } else {
            Write-Host "✗ Client ID format appears invalid" -ForegroundColor Yellow
        }
        
    } else {
        Write-Host "✗ Azure AD configuration not found in $ConfigFile" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Error reading configuration file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Testing Application Availability ===" -ForegroundColor Green

# Test if the application is running
try {
    $response = Invoke-WebRequest -Uri $AppUrl -Method Head -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✓ Application is accessible at $AppUrl" -ForegroundColor Green
    Write-Host "  Status Code: $($response.StatusCode)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Application is not accessible at $AppUrl" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Make sure the application is running with: dotnet run --project AssetManagement.Web" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Testing Authentication Endpoints ===" -ForegroundColor Green

# Test authentication endpoints
$endpoints = @(
    "/Account/SignIn",
    "/Account/SignOut",
    "/Account/AccessDenied",
    "/Account/SignedOut"
)

foreach ($endpoint in $endpoints) {
    $url = "$AppUrl$endpoint"
    try {
        $response = Invoke-WebRequest -Uri $url -Method Head -TimeoutSec 5 -ErrorAction Stop
        Write-Host "✓ $endpoint - Status: $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "✗ $endpoint - Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Security Checklist ===" -ForegroundColor Green

# Security recommendations
$securityChecks = @(
    @{ Check = "HTTPS is enabled"; Status = $AppUrl.StartsWith("https") },
    @{ Check = "Client Secret is configured"; Status = $config.AzureAd.ClientSecret -and $config.AzureAd.ClientSecret.Length -gt 0 },
    @{ Check = "Callback paths are configured"; Status = $config.AzureAd.CallbackPath -and $config.AzureAd.SignedOutCallbackPath },
    @{ Check = "Tenant ID is configured"; Status = $config.AzureAd.TenantId -and $config.AzureAd.TenantId.Length -gt 0 },
    @{ Check = "Client ID is configured"; Status = $config.AzureAd.ClientId -and $config.AzureAd.ClientId.Length -gt 0 }
)

foreach ($check in $securityChecks) {
    if ($check.Status) {
        Write-Host "✓ $($check.Check)" -ForegroundColor Green
    } else {
        Write-Host "✗ $($check.Check)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Green
Write-Host "1. Open your browser and navigate to: $AppUrl" -ForegroundColor White
Write-Host "2. Click 'Sign In' to test the authentication flow" -ForegroundColor White
Write-Host "3. Verify you can sign in with your Azure AD credentials" -ForegroundColor White
Write-Host "4. Check that user information is displayed correctly" -ForegroundColor White
Write-Host "5. Test the sign-out functionality" -ForegroundColor White
Write-Host ""
Write-Host "For detailed setup instructions, see: AZURE_AD_SETUP_GUIDE.md" -ForegroundColor Cyan
Write-Host ""

# Check if running in development environment
if ($AppUrl.Contains("localhost")) {
    Write-Host "=== Development Environment Notes ===" -ForegroundColor Yellow
    Write-Host "• Make sure your Azure AD app registration includes: $AppUrl/signin-oidc" -ForegroundColor White
    Write-Host "• Make sure your Azure AD app registration includes: $AppUrl/signout-callback-oidc" -ForegroundColor White
    Write-Host "• For production, update these URLs to your actual domain" -ForegroundColor White
    Write-Host ""
}
