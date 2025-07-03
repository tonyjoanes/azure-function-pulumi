# Test different Azure regions for quota availability
# This script helps you test multiple regions to find one with available quota

param(
    [string]$TestRegion = "West US 2",
    [string]$Environment = "dev"
)

Write-Host "🌍 Testing Azure region: $TestRegion" -ForegroundColor Yellow
Write-Host "📋 Environment: $Environment" -ForegroundColor Yellow

# High-probability regions (often have better quota availability)
$RegionsToTry = @(
    "West US 2",
    "Central US", 
    "West Europe",
    "Southeast Asia",
    "East US 2",
    "North Central US",
    "South Central US",
    "Australia East",
    "Japan East",
    "UK South"
)

Write-Host "`n🎯 Recommended regions to try (in order of success probability):" -ForegroundColor Green
for ($i = 0; $i -lt $RegionsToTry.Count; $i++) {
    $status = if ($RegionsToTry[$i] -eq $TestRegion) { "← TESTING NOW" } else { "" }
    Write-Host "  $($i + 1). $($RegionsToTry[$i]) $status" -ForegroundColor Cyan
}

Write-Host "`n📝 To test a region:" -ForegroundColor Green
Write-Host "  1. Update Pulumi.dev.yaml with new location" -ForegroundColor White
Write-Host "  2. Commit and push changes" -ForegroundColor White
Write-Host "  3. GitHub Actions will attempt deployment" -ForegroundColor White
Write-Host "  4. Check workflow results" -ForegroundColor White

Write-Host "`n🔧 Quick setup commands:" -ForegroundColor Green
Write-Host "  # Test West US 2" -ForegroundColor White
Write-Host "  git add -A && git commit -m 'Test West US 2' && git push" -ForegroundColor Gray
Write-Host ""
Write-Host "  # Test Central US" -ForegroundColor White  
Write-Host "  git add -A && git commit -m 'Test Central US' && git push" -ForegroundColor Gray

Write-Host "`n✨ Creating Pulumi config for $TestRegion..." -ForegroundColor Yellow

# Create or update Pulumi config
$pulumiConfig = @"
config:
  azure-function-pulumi:location: "$TestRegion"
  azure-function-pulumi:environment: "$Environment"
  azure-function-pulumi:welcomeMessage: "Hello from $TestRegion - $Environment environment!"
"@

$configFile = "Pulumi.$Environment.yaml"
Set-Content -Path $configFile -Value $pulumiConfig -Encoding UTF8

Write-Host "✅ Created $configFile with location: $TestRegion" -ForegroundColor Green
Write-Host "`n🚀 Next steps:" -ForegroundColor Yellow
Write-Host "  1. git add $configFile" -ForegroundColor White
Write-Host "  2. git commit -m 'Test $TestRegion region'" -ForegroundColor White
Write-Host "  3. git push" -ForegroundColor White
Write-Host "  4. Check GitHub Actions workflow" -ForegroundColor White

Write-Host "`n🔍 Monitor deployment at:" -ForegroundColor Yellow
Write-Host "  https://github.com/YOUR_USERNAME/azure-function-pulumi/actions" -ForegroundColor Cyan 