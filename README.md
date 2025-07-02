# Azure Function with Pulumi Infrastructure

![Build and Deploy](https://github.com/tonyjoanes/azure-function-pulumi/workflows/Deploy%20to%20Azure/badge.svg)
![.NET](https://img.shields.io/badge/.NET-6.0-blue.svg)
![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)
![Pulumi](https://img.shields.io/badge/Pulumi-C%23-purple.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)

A production-ready Azure Functions project with Infrastructure as Code using Pulumi (C#) and automated CI/CD deployment via GitHub Actions.

## 🚀 Features

- **Azure Functions v4** with isolated worker model
- **Configuration Management** with strongly-typed settings and Options pattern
- **Infrastructure as Code** using Pulumi C# SDK
- **Multi-Environment Support** (dev/staging/prod) with separate Azure resources
- **GitHub Actions CI/CD** with automatic deployment and infrastructure provisioning
- **Monitoring & Logging** with Application Insights integration

## 🏗️ Architecture

```
azure-function-pulumi/
├── src/                          # Azure Function Code
│   ├── HelloWorldFunction.cs     # Hello World HTTP trigger
│   ├── ConfigDemoFunction.cs     # Configuration patterns demo
│   ├── EnvironmentDemoFunction.cs # Environment variables demo
│   ├── AppSettings.cs            # Strongly-typed configuration
│   └── Program.cs                # Function host setup
├── infrastructure/               # Pulumi Infrastructure Code
│   ├── Infrastructure.csproj     # Pulumi C# project
│   ├── Program.cs                # Infrastructure definition
│   └── Pulumi.yaml              # Pulumi project config
└── .github/workflows/           # CI/CD Pipelines
    ├── deploy.yml               # Build & Deploy workflow
    └── destroy.yml              # Safe infrastructure cleanup
```

## 🛠️ Prerequisites

- **Azure Subscription** with Contributor access
- **Azure CLI** installed and authenticated (`az login`)
- **.NET 6.0 SDK** or later
- **Pulumi Account** (free at https://app.pulumi.com)
- **Git** and **GitHub** repository

## ⚡ Quick Start

### 1. Clone and Setup
```bash
git clone https://github.com/tonyjoanes/azure-function-pulumi.git
cd azure-function-pulumi
```

### 2. Configure GitHub Secrets
Set up the following secrets in your GitHub repository:

- `AZURE_CREDENTIALS`: Azure service principal JSON (see [setup guide](DEPLOYMENT.md))
- `PULUMI_ACCESS_TOKEN`: Pulumi access token from https://app.pulumi.com

### 3. Deploy to Azure
Push to main branch or manually trigger the "Deploy to Azure" workflow:
```bash
git push origin main
```

### 4. Test Your Function
Once deployed, test the endpoints:
```bash
# Hello World
curl https://azure-function-pulumi-dev-func.azurewebsites.net/api/HelloWorld

# Configuration Demo
curl https://azure-function-pulumi-dev-func.azurewebsites.net/api/ConfigDemo

# Environment Variables
curl https://azure-function-pulumi-dev-func.azurewebsites.net/api/EnvironmentDemo
```

## 🌍 Multi-Environment Support

This project supports separate environments with isolated Azure resources:

| Environment | Trigger | Azure Resources |
|-------------|---------|-----------------|
| **Dev** | Push to `main` | `azure-function-pulumi-dev-*` |
| **Staging** | Pull Request | `azure-function-pulumi-staging-*` |
| **Prod** | Manual workflow dispatch | `azure-function-pulumi-prod-*` |

Each environment has its own:
- Resource Group
- Function App
- Storage Account
- Application Insights
- Configuration values

## 🔧 Local Development

### Run Function Locally
```bash
cd src
dotnet restore
func start
```

### Deploy Infrastructure Locally
```bash
cd infrastructure
az login
pulumi login
pulumi stack select dev
pulumi up
```

### Local Testing Script
```bash
# Use the provided PowerShell script
.\deploy-local.ps1
```

## ⚙️ Configuration Management

The project demonstrates three configuration patterns:

1. **Direct IConfiguration Access**
```csharp
var setting = _configuration["MySetting"];
```

2. **Strongly-Typed Configuration**
```csharp
var appSettings = _configuration.GetSection("AppSettings").Get<AppSettings>();
```

3. **Options Pattern with Fallback**
```csharp
var maxRetries = _configuration.GetValue<int>("AppSettings:MaxRetries", 3);
```

## 🏗️ Infrastructure Resources

Each environment deploys:
- **Resource Group**: Container for all resources
- **Storage Account**: Required for Azure Functions runtime
- **Function App**: Serverless compute with consumption plan
- **Application Insights**: Monitoring and logging
- **Configuration**: Environment-specific app settings

## 🔄 CI/CD Pipeline

### Build & Deploy Workflow
1. **Build & Test**: Compile function code and run tests
2. **Infrastructure**: Deploy/update Azure resources with Pulumi
3. **Function Deploy**: Deploy function code to Azure
4. **Verification**: Health check and endpoint testing

### Destroy Workflow
Safe infrastructure cleanup requiring `DESTROY` confirmation:
```bash
# GitHub Actions -> Destroy Infrastructure
# Type: "DESTROY" to confirm
```

## 📊 Monitoring

- **Application Insights**: Automatic telemetry and logging
- **Health Endpoint**: `/api/ConfigHealth` for monitoring
- **GitHub Actions**: Build and deployment status badges

## 🛡️ Security

- **Azure Managed Identity**: For secure resource access
- **GitHub Secrets**: Encrypted credential storage
- **Principle of Least Privilege**: Minimal required permissions
- **Environment Isolation**: Separate resources per environment

## 📚 Learn More

- [Detailed Setup Guide](DEPLOYMENT.md)
- [Azure Functions Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/)
- [Pulumi Azure Native Provider](https://www.pulumi.com/registry/packages/azure-native/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test locally
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Issues**: [GitHub Issues](https://github.com/tonyjoanes/azure-function-pulumi/issues)
- **Discussions**: [GitHub Discussions](https://github.com/tonyjoanes/azure-function-pulumi/discussions)
- **Documentation**: Check the [DEPLOYMENT.md](DEPLOYMENT.md) guide

---

⭐ **Star this repository** if you find it helpful! 