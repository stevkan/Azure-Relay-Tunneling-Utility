# Azure Relay Tunneling Utility

**Version: 1.5.1**

An HTTP tunneling utility based on Azure Relay. Forward HTTP traffic from Azure to your local machine for debugging and development.

## 🎯 What Does This Do?

This utility creates a secure tunnel between Azure and your local machine, allowing you to:
- Expose local web servers, APIs, or HTTP services through Azure endpoints
- Debug bots and agents locally while receiving real traffic from Azure-hosted channels
- Test with real ChannelData from channels like WebChat, Teams, Skype
- Develop and test without deploying to Azure
- Access localhost services from anywhere via Azure Relay

## 📦 Choose Your Project

This repository contains two implementations using different Azure Relay technologies:

### [RelayTunnelUsingHybridConnection](Src/RelayTunnelUsingHybridConnection/README.md) ✅ **Recommended**
**Technology:** Azure Relay Hybrid Connections (.NET 8)  
**Best For:** Modern development, production use, dynamic resource management

**Protocol Support:**
- ✅ HTTP/REST (request/response patterns)
- ✅ WebSocket connections

**Key Features:**
- ✅ Modern .NET 8 implementation
- ✅ Dynamic resource creation - hybrid connections appear/disappear automatically
- ✅ Multiple authentication options (Azure CLI, Service Principal, Managed Identity)
- ✅ Multi-relay support in single application
- ✅ Actively maintained

**[📖 Full Documentation →](Src/RelayTunnelUsingHybridConnection/README.md)**

---

### [RelayTunnelUsingWCF](Src/RelayTunnelUsingWCF/README.md)
**Technology:** WCF Relay (.NET Framework 4.8)  
**Best For:** Legacy systems, existing WCF infrastructure

**Protocol Support:**
- ✅ HTTP/REST (request/response patterns)
- ❌ WebSocket connections **NOT supported**

**Key Features:**
- ✅ WCF Relay endpoints
- ✅ Dynamic relay registration (appears when running)
- ✅ .NET Framework 4.8
- ⚠️ **Security Warning:** Uses deprecated Azure libraries with no ongoing security updates

**⚠️ Not recommended for production use due to deprecated dependencies**

**[📖 Full Documentation →](Src/RelayTunnelUsingWCF/README.md)**

---

## 🚀 Quick Start

1. **Choose your project** (Hybrid Connection recommended for new projects)
2. **Follow the project-specific README** for detailed setup instructions
3. **Configure your bot's messaging endpoint** in Azure to point to your relay
4. **Run the utility** and test your bot locally

## 📊 Quick Comparison

| Feature | Hybrid Connection | WCF Relay |
|---------|------------------|-----------|
| **.NET Version** | .NET 8 | .NET Framework 4.8 |
| **HTTP/REST Support** | ✅ Yes | ✅ Yes |
| **WebSocket Support** | ✅ Yes | ❌ No |
| **Production Ready** | ✅ Yes | ⚠️ **No - Security Risk** |
| **Security Updates** | ✅ Active support | ❌ Deprecated libraries |
| **Authentication** | Azure CLI, Service Principal, Managed Identity | SAS key only |
| **Dynamic Resources** | ✅ ARM template automation | ✅ Runtime registration |

**Recommendation:** Use RelayTunnelUsingHybridConnection for all new projects and production deployments.

📚 **[View Detailed Comparison →](docs/COMPARISON.md)**

## 📖 Documentation

### Project Setup Guides
- **[Hybrid Connection Setup](Src/RelayTunnelUsingHybridConnection/README.md)** - Complete guide for modern implementation
- **[WCF Relay Setup](Src/RelayTunnelUsingWCF/README.md)** - Complete guide for legacy implementation

### Additional Resources
- **[Technical Comparison](docs/COMPARISON.md)** - Detailed WCF vs Hybrid Connection comparison and migration guide
- **[Authentication Guide](docs/AUTHENTICATION.md)** - Azure authentication setup for dynamic resources
- **[Troubleshooting Guide](docs/TROUBLESHOOTING.md)** - Common issues and solutions
- **[ARM Automation Details](Src/RelayTunnelUsingHybridConnection/README_ARM_AUTOMATION.md)** - Technical implementation details

## 🙏 Acknowledgments

This project is a rewrite inspired by the original work that [Gabriel Gonzalez (gabog)](https://github.com/gabog) created in his project [AzureServiceBusBotRelay](https://github.com/gabog/AzureServiceBusBotRelay).

Part of this code is also based on the work that [Pedro Felix](https://github.com/pmhsfelix) did in his project [WebApi.Explorations.ServiceBusRelayHost](https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost).

## 📝 License

See [LICENSE](LICENSE) file for details.
