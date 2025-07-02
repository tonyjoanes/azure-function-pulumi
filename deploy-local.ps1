# Local Development Deployment Script
# For production deployments, use GitHub Actions instead!
# See DEPLOYMENT.md for GitHub Actions setup

param(
    [string]$StackName = "dev-local",
    [switch]$Preview = $false
)

Write-Host "🧪 LOCAL DEVELOPMENT DEPLOYMENT" -ForegroundColor Yellow
Write-Host "⚠️  For production deployments, use GitHub Actions!" -ForegroundColor Red
Write-Host "📖 See DEPLOYMENT.md for GitHub Actions setup" -ForegroundColor Cyan
Write-Host ""
Write-Host "Stack: $StackName" -ForegroundColor Yellow

# Step 1: Build the Azure Function
Write-Host "📦 Building Azure Function..." -ForegroundColor Green
Push-Location src
try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Function build failed"
    }
    Write-Host "✅ Function build successful" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Step 2: Restore Pulumi dependencies
Write-Host "📦 Restoring Pulumi dependencies..." -ForegroundColor Green
dotnet restore Infrastructure.csproj
if ($LASTEXITCODE -ne 0) {
    throw "Pulumi dependency restore failed"
}

# Step 3: Check Pulumi login
Write-Host "🔐 Checking Pulumi authentication..." -ForegroundColor Green
$pulumiUser = pulumi whoami 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Not logged into Pulumi. Run 'pulumi login' first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Logged in as: $pulumiUser" -ForegroundColor Green

# Step 4: Check Azure login
Write-Host "🔐 Checking Azure authentication..." -ForegroundColor Green
$azAccount = az account show --query "name" -o tsv 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Not logged into Azure. Run 'az login' first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Azure account: $azAccount" -ForegroundColor Green

# Step 5: Deploy infrastructure
Write-Host "🏗️  Deploying infrastructure..." -ForegroundColor Green
if ($Preview) {
    Write-Host "👀 Running preview mode..." -ForegroundColor Yellow
    pulumi preview --stack $StackName
}
else {
    pulumi up --stack $StackName --yes
    if ($LASTEXITCODE -ne 0) {
        throw "Infrastructure deployment failed"
    }
    Write-Host "✅ Infrastructure deployed successfully!" -ForegroundColor Green
    
    # Get the function app name for deployment
    $functionAppName = pulumi stack output functionAppName --stack $StackName
    Write-Host "Function App Name: $functionAppName" -ForegroundColor Yellow
    
    # Step 6: Deploy function code
    Write-Host "📤 Deploying function code..." -ForegroundColor Green
    Push-Location src
    try {
        # Create deployment package
        Remove-Item -Path "publish" -Recurse -Force -ErrorAction SilentlyContinue
        dotnet publish --configuration Release --output publish
        
        # Create zip package
        if (Test-Path "publish.zip") {
            Remove-Item "publish.zip" -Force
        }
        Compress-Archive -Path "publish\*" -DestinationPath "publish.zip" -Force
        
        # Deploy to Azure (requires Azure CLI)
        Write-Host "Deploying to Azure Function App: $functionAppName" -ForegroundColor Yellow
        az functionapp deployment source config-zip --resource-group azure-function-rg --name $functionAppName --src publish.zip
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Function code deployed successfully!" -ForegroundColor Green
            
            # Display endpoints
            $functionAppUrl = pulumi stack output functionAppUrl --stack $StackName
            Write-Host ""
            Write-Host "🌐 Your Azure Function is available at:" -ForegroundColor Cyan
            Write-Host "   • Hello World: $functionAppUrl/api/HelloWorld" -ForegroundColor White
            Write-Host "   • Config Demo: $functionAppUrl/api/ConfigDemo" -ForegroundColor White
            Write-Host "   • Config Health: $functionAppUrl/api/ConfigHealth" -ForegroundColor White
            Write-Host ""
            Write-Host "📊 Monitor your function:" -ForegroundColor Cyan
            Write-Host "   • Azure Portal: https://portal.azure.com" -ForegroundColor White
            Write-Host "   • Pulumi Console: https://app.pulumi.com" -ForegroundColor White
        }
        else {
            Write-Host "⚠️  Function code deployment failed. Check Azure CLI configuration." -ForegroundColor Yellow
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
Write-Host "🎉 Local deployment complete!" -ForegroundColor Green
Write-Host "💡 For production deployments, use GitHub Actions:" -ForegroundColor Yellow
Write-Host "   1. Setup secrets (see DEPLOYMENT.md)" -ForegroundColor White
Write-Host "   2. Push to main branch or run manually" -ForegroundColor White
Write-Host "   3. Monitor in GitHub Actions tab" -ForegroundColor White 