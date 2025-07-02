# GitHub Actions Deployment Guide

This project uses GitHub Actions for CI/CD deployment of the Azure Function with Pulumi Infrastructure as Code.

## 🚀 **Workflows Overview**

### 1. **Main Deployment** (`deploy.yml`)
- **Triggers**: Push to `main`/`develop`, Manual dispatch
- **Jobs**: Build → Deploy Infrastructure → Deploy Function Code
- **Environments**: dev, staging, prod

### 2. **PR Preview** (`deploy.yml`)
- **Triggers**: Pull requests to `main`
- **Action**: Shows infrastructure changes without deploying
- **Comments**: Pulumi preview results on PR

### 3. **Destroy Infrastructure** (`destroy.yml`)
- **Triggers**: Manual dispatch only
- **Safety**: Requires typing "DESTROY" to confirm
- **Limitation**: Production destruction not allowed via workflow

## 🔐 **Required Secrets Setup**

### Step 1: Create Azure Service Principal

```bash
# Login to Azure
az login

# Create service principal
az ad sp create-for-rbac `
  --name "azure-function-pulumi-gh" `
  --role "Contributor" `
  --scopes "/subscriptions/6b2914e3-c249-4fc5-9d67-be6d8abdbb66" `
  --sdk-auth
```

Copy the JSON output - you'll need it for GitHub secrets.

### Step 2: Setup Pulumi Access Token

1. Visit [Pulumi Console](https://app.pulumi.com)
2. Go to **Settings** → **Access Tokens**
3. Create new token with name `github-actions-azure-function`
4. Copy the token value

### Step 3: Configure GitHub Repository Secrets

Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions**

Add these **Repository Secrets**:

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `AZURE_CREDENTIALS` | JSON from Step 1 | Azure Service Principal credentials |
| `PULUMI_ACCESS_TOKEN` | Token from Step 2 | Pulumi access token |

**Example AZURE_CREDENTIALS format:**
```json
{
  "clientId": "12345678-1234-1234-1234-123456789012",
  "clientSecret": "your-client-secret",
  "subscriptionId": "12345678-1234-1234-1234-123456789012",
  "tenantId": "12345678-1234-1234-1234-123456789012"
}
```

## 📋 **Deployment Process**

### **Automatic Deployment**
1. **Push to main branch** → Triggers deployment to `dev` environment
2. **GitHub Actions runs**:
   - ✅ Builds and tests Azure Function
   - ✅ Deploys infrastructure with Pulumi
   - ✅ Deploys function code to Azure
   - ✅ Verifies deployment

### **Manual Deployment**
1. Go to **Actions** tab in GitHub
2. Select **Deploy Azure Function with Pulumi**
3. Click **Run workflow**
4. Choose environment (dev/staging/prod)
5. Click **Run workflow**

### **Pull Request Preview**
1. **Create PR to main** → Triggers preview
2. **GitHub Actions**:
   - ✅ Builds and validates code
   - ✅ Shows infrastructure changes
   - ✅ Comments on PR with preview

## 🌍 **Environment Strategy**

### **Development (`dev`)**
- **Trigger**: Push to `develop` branch
- **Purpose**: Development testing
- **Resources**: Minimal cost configuration

### **Staging (`staging`)**
- **Trigger**: Manual deployment
- **Purpose**: Pre-production testing
- **Resources**: Production-like configuration

### **Production (`prod`)**
- **Trigger**: Manual deployment only
- **Purpose**: Live production environment
- **Resources**: Full production configuration

## 🔧 **Workflow Configuration**

### **Environment Variables**
```yaml
env:
  DOTNET_VERSION: '6.0.x'
  AZURE_FUNCTIONAPP_PACKAGE_PATH: './src'
```

### **Customization Options**

**Deploy to different branch:**
```yaml
on:
  push:
    branches: [ main, develop, feature/deploy ]
```

**Add environment protection:**
```yaml
environment:
  name: production
  url: https://your-function-app.azurewebsites.net
```

**Add manual approval:**
```yaml
environment:
  name: production
```
*(Configure protection rules in GitHub repository settings)*

## 🛡️ **Security Best Practices**

### **✅ Implemented**
- Service Principal with minimal required permissions
- Secrets stored in GitHub encrypted secrets
- Production destruction requires manual confirmation
- Infrastructure preview on pull requests

### **🚀 Recommended Enhancements**
- **Environment Protection Rules**: Require manual approval for production
- **Branch Protection**: Require PR reviews before merging to main
- **Azure Key Vault**: Store sensitive application settings
- **IP Restrictions**: Limit function app access if needed

## 🔍 **Monitoring & Troubleshooting**

### **Deployment Status**
- **GitHub Actions**: Check workflow runs in repository
- **Azure Portal**: Monitor Function App deployment status
- **Pulumi Console**: View infrastructure state and updates

### **Common Issues**

#### **Authentication Failed**
```
Error: Failed to get access token
```
**Solution**: Check `AZURE_CREDENTIALS` secret format and permissions

#### **Pulumi State Conflict**
```
Error: resource already exists
```
**Solution**: Import existing resources or use `pulumi refresh`

#### **Function Deployment Timeout**
```
Error: Deployment timed out
```
**Solution**: Check Azure Function logs and resource availability

### **Useful Commands**

**Check deployment status:**
```bash
# Via Azure CLI
az functionapp show \
  --resource-group azure-function-rg \
  --name your-function-app \
  --query "state"

# Via Pulumi
pulumi stack output --stack dev
```

**View function logs:**
```bash
# Stream logs
az functionapp log tail \
  --resource-group azure-function-rg \
  --name your-function-app
```

## 🎯 **Next Steps**

1. **Setup Secrets** (follow steps above)
2. **Test Deployment**: Push to develop branch
3. **Verify Function**: Check deployed endpoints
4. **Setup Production**: Configure environment protection
5. **Monitor**: Set up alerts and monitoring

## 📞 **Support**

- **GitHub Issues**: For workflow problems
- **Azure Support**: For Azure-specific issues
- **Pulumi Community**: For infrastructure questions

---

**Ready to deploy?** Push your code to the `main` branch and watch the magic happen! 🚀 