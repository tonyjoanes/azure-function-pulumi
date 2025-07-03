# Deploy to different environments
# Usage: .\deploy-environment.ps1 -Environment staging

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment
)

Write-Host "🚀 Deploying to $Environment environment..." -ForegroundColor Green

# Commit any changes
git add -A
git commit -m "Deploy to $Environment environment" -ErrorAction SilentlyContinue

# Push to trigger deployment
git push

Write-Host "✅ Deployment triggered for $Environment environment" -ForegroundColor Green
Write-Host "🔍 Monitor at: https://github.com/tonyjoanes/azure-function-pulumi/actions" -ForegroundColor Cyan

# Wait for deployment to complete
Write-Host "⏳ Waiting for deployment to complete..." -ForegroundColor Yellow
Write-Host "   You can manually trigger deployment for specific environment using:" -ForegroundColor White
Write-Host "   GitHub Actions → Deploy Infrastructure → Run workflow → Select '$Environment'" -ForegroundColor Gray

Write-Host "`n🎯 Expected resources for $Environment environment:" -ForegroundColor Yellow
Write-Host "   • Resource Group: rg-azure-functions-$Environment" -ForegroundColor White
Write-Host "   • Function App: func-azure-functions-$Environment-[random]" -ForegroundColor White
Write-Host "   • Storage Account: safunc$Environment[random]" -ForegroundColor White
Write-Host "   • App Service Plan: asp-azure-functions-$Environment" -ForegroundColor White
Write-Host "   • Application Insights: ai-azure-functions-$Environment" -ForegroundColor White 