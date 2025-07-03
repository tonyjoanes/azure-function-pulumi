# Test Different Azure Regions for Quota
# This script helps you test which regions might have available quota

Write-Host "🌍 Testing Azure Regions for Function App Deployment" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Common regions to test (ordered by typical quota availability)
$regions = @(
    "West US 2",
    "West Europe", 
    "Southeast Asia",
    "UK South",
    "Australia East",
    "Central US",
    "North Europe",
    "West US",
    "East US 2",
    "East US"
)

Write-Host "💡 Recommended approach:" -ForegroundColor Yellow
Write-Host "1. Try regions in order below" -ForegroundColor Yellow
Write-Host "2. Set region config: pulumi config set location `"West US 2`"" -ForegroundColor Yellow
Write-Host "3. Run deployment: pulumi up" -ForegroundColor Yellow
Write-Host ""

Write-Host "🎯 Regions to try (in order of likely success):" -ForegroundColor Cyan
for ($i = 0; $i -lt $regions.Length; $i++) {
    $region = $regions[$i]
    Write-Host "  $($i + 1). $region" -ForegroundColor White
    
    if ($i -eq 0) {
        Write-Host "     👆 Start with this one (typically has most quota)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "🔧 Commands to test a region:" -ForegroundColor Cyan
Write-Host "  cd infrastructure" -ForegroundColor Gray
Write-Host "  pulumi config set location `"West US 2`"" -ForegroundColor Gray
Write-Host "  pulumi up --dry-run  # Test without actually deploying" -ForegroundColor Gray
Write-Host "  pulumi up            # Deploy if dry-run succeeds" -ForegroundColor Gray

Write-Host ""
Write-Host "📊 Check current quota limits:" -ForegroundColor Cyan
Write-Host "  Azure Portal → Subscriptions → Usage + quotas" -ForegroundColor Gray
Write-Host "  Search for: 'Dynamic VMs' or 'Basic VMs'" -ForegroundColor Gray

Write-Host ""
Write-Host "🚀 If all regions fail, request quota increase:" -ForegroundColor Yellow
Write-Host "  1. Azure Portal → Subscriptions → Usage + quotas" -ForegroundColor Gray
Write-Host "  2. Search 'Dynamic VMs' → Request quota increase" -ForegroundColor Gray
Write-Host "  3. Request 10+ VMs for your preferred region" -ForegroundColor Gray
Write-Host "  4. Approval usually takes 24 hours" -ForegroundColor Gray 