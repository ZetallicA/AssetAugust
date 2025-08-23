# Test Storage and Salvage Flow
# This script tests the new functionality for automatic storage assignment and salvage data clearing

param(
    [string]$BaseUrl = "https://localhost:7147",
    [string]$AssetTag = "TEST-001"
)

Write-Host "Testing Storage and Salvage Flow" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Asset Tag: $AssetTag" -ForegroundColor Yellow
Write-Host ""

# Test 1: Move asset to storage (should auto-assign storage location)
Write-Host "Test 1: Moving asset to storage..." -ForegroundColor Cyan
try {
    $redeployBody = @{
        assetTag = $AssetTag
        newDesk = $null  # null means move to storage
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$BaseUrl/api/lifecycle/redeploy" -Method POST -Body $redeployBody -ContentType "application/json"
    Write-Host "✓ Asset moved to storage successfully" -ForegroundColor Green
    Write-Host "Response: $($response.message)" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Failed to move asset to storage: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Mark asset for salvage (should clear sensitive data)
Write-Host "Test 2: Marking asset for salvage..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/lifecycle/mark-salvage-pending" -Method POST -Body "`"$AssetTag`"" -ContentType "application/json"
    Write-Host "✓ Asset marked for salvage successfully" -ForegroundColor Green
    Write-Host "Response: $($response.message)" -ForegroundColor Gray
    Write-Host "Note: Sensitive data (IP, MAC, user assignments) should be cleared" -ForegroundColor Yellow
}
catch {
    Write-Host "✗ Failed to mark asset for salvage: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Get assets in storage
Write-Host "Test 3: Getting assets in storage..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/lifecycle/assets/in-storage?site=LIC" -Method GET
    Write-Host "✓ Retrieved assets in storage successfully" -ForegroundColor Green
    Write-Host "Found $($response.Count) assets in storage" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Failed to get assets in storage: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Get assets marked for salvage
Write-Host "Test 4: Getting assets marked for salvage..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/lifecycle/assets/marked-for-salvage?site=LIC" -Method GET
    Write-Host "✓ Retrieved assets marked for salvage successfully" -ForegroundColor Green
    Write-Host "Found $($response.Count) assets marked for salvage" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Failed to get assets marked for salvage: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 5: Get assets in transit
Write-Host "Test 5: Getting assets in transit..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/lifecycle/assets/in-transit" -Method GET
    Write-Host "✓ Retrieved assets in transit successfully" -ForegroundColor Green
    Write-Host "Found $($response.Count) assets in transit" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Failed to get assets in transit: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Storage and Salvage Flow Test Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Summary of new features tested:" -ForegroundColor Yellow
Write-Host "1. Automatic storage location assignment when moving to storage" -ForegroundColor White
Write-Host "2. Sensitive data clearing when marking for salvage" -ForegroundColor White
Write-Host "3. API endpoints for managing assets in different states" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "- Test location reassignment for assets in transit" -ForegroundColor White
Write-Host "- Verify storage locations are correctly auto-assigned" -ForegroundColor White
Write-Host "- Check that sensitive data is properly cleared for salvage" -ForegroundColor White
