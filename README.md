# Azure Relay Tunneling Utility

**Versions:** Hybrid Connection (.NET) v1.6.1 | Hybrid Connection (TypeScript) v0.9.0-beta.2 | WCF Relay v1.5.5

An HTTP tunneling utility based on Azure Relay. Forward HTTP traffic from Azure to your local machine for debugging and development.

## 🎯 What Does This Do?

This utility creates a secure tunnel between Azure and your local machine, allowing you to:
- Expose local web servers, APIs, or HTTP services through Azure endpoints
- Debug bots and agents locally while receiving real traffic from Azure-hosted channels
- Test with real ChannelData from channels like WebChat, Teams, Skype
- Develop and test without deploying to Azure
- Access localhost services from anywhere via Azure Relay

## 📦 Choose Your Project

This repository contains implementations in **.NET** and **TypeScript/Node.js** using different Azure Relay technologies:

### [RelayTunnelUsingHybridConnection](Src/dotnet/RelayTunnelUsingHybridConnection/README.md) ✅ **Recommended**
**Technology:** Azure Relay Hybrid Connections (.NET 8)  
**Best For:** Modern development, production use, dynamic resource management  
**Platform:** Cross-platform (Windows, Linux, macOS)

**Protocol Support:**
- ✅ HTTP/REST (request/response patterns)
- ✅ WebSocket connections

**Key Features:**
- ✅ Modern .NET 8 implementation
- ✅ Cross-platform support (Windows, Linux, macOS)
- ✅ Dynamic resource creation - hybrid connections appear/disappear automatically
- ✅ Multiple authentication options (Azure CLI, Service Principal, Managed Identity)
- ✅ Multi-relay support in single application
- ✅ Actively maintained

**[📖 Full Documentation →](Src/dotnet/RelayTunnelUsingHybridConnection/README.md)**

---

### [RelayTunnelUsingWCF](Src/dotnet/RelayTunnelUsingWCF/README.md)
**Technology:** WCF Relay (.NET Framework 4.8)  
**Best For:** Legacy systems, existing WCF infrastructure  
**Platform:** Windows only

**Protocol Support:**
- ✅ HTTP/REST (request/response patterns)
- ❌ WebSocket connections **NOT supported**

**Key Features:**
- ✅ WCF Relay endpoints
- ✅ Dynamic relay registration (appears when running)
- ✅ .NET Framework 4.8
- ⚠️ **Security Warning:** Uses deprecated Azure libraries with no ongoing security updates

**⚠️ Not recommended for production use due to deprecated dependencies**

**[📖 Full Documentation →](Src/dotnet/RelayTunnelUsingWCF/README.md)**

---

### [RelayTunnelUsingHybridConnection (TypeScript)](Src/ts/RelayTunnelUsingHybridConnection/README.md) ⚠️ **Beta - DirectLine Issues**
**Technology:** Azure Relay Hybrid Connections (TypeScript/Node.js)  
**Version:** v0.9.0-beta.2  
**Best For:** Node.js/TypeScript projects, cross-platform deployments (non-DirectLine scenarios)  
**Platform:** Cross-platform (Windows, Linux, macOS)

**Protocol Support:**
- ✅ HTTP/REST (request/response patterns)
- ⚠️ WebSocket connections (has issues with DirectLine/Web Chat)

**Key Features:**
- ✅ Node.js 20+ implementation
- ✅ Cross-platform support (Windows, Linux, macOS)
- ✅ Dynamic resource creation with Azure ARM
- ✅ Type-safe configuration with Zod
- ✅ Environment variable configuration
- ✅ CLI support with yargs
- ❌ **WCF Relay NOT supported** (no Node.js libraries exist)

**⚠️ Known Issues (Beta):**
- ⚠️ **DirectLine/Web Chat compatibility issues:**
  - Messages may fail (502 Bad Gateway) or be delayed by several seconds
  - Conversation update activities fail to be received
  - WebSocket mode produces 502 errors even on successful messages
  - **For DirectLine/Web Chat, use the .NET version instead**

**[📖 Full Documentation →](Src/ts/RelayTunnelUsingHybridConnection/README.md)**

---

## 🚀 Quick Start

1. **Choose your project** (Hybrid Connection recommended for new projects)
2. **Follow the project-specific README** for detailed setup instructions
3. **Configure your bot's messaging endpoint** in Azure to point to your relay
4. **Run the utility** and test your bot locally

## 📊 Quick Comparison

| Feature | Hybrid Connection (.NET) | Hybrid Connection (TypeScript) | WCF Relay (.NET) |
|---------|--------------------------|--------------------------------|------------------|
| **Platform** | .NET 8 | Node.js 20+ | .NET Framework 4.8 |
| **Version** | v1.6.1 | v0.9.0-beta.2 | v1.5.5 |
| **OS Support** | Windows, Linux, macOS | Windows, Linux, macOS | Windows only |
| **HTTP/REST** | ✅ Yes | ✅ Yes | ✅ Yes |
| **WebSocket** | ✅ Yes | ⚠️ Yes (DirectLine issues) | ❌ No |
| **DirectLine/Web Chat** | ✅ Fully supported | ⚠️ **Known issues (Beta)** | ❌ No WebSocket |
| **Production Ready** | ✅ Yes | ⚠️ **Beta (not for DirectLine)** | ⚠️ **No - Security Risk** |
| **Security Updates** | ✅ Active support | ✅ Active support | ❌ Deprecated libraries |
| **Authentication** | Azure CLI, SP, MI | Azure CLI, SP, MI | SAS key only |
| **Dynamic Resources** | ✅ ARM automation | ✅ ARM automation | ✅ Runtime registration |
| **Config Type** | JSON file | Environment variables | JSON file |

**Recommendation:** Use Hybrid Connection **.NET** for all new projects, especially with DirectLine/Web Chat. TypeScript version is beta and has DirectLine compatibility issues. WCF Relay is legacy only.

📚 **[View Detailed Comparison →](docs/COMPARISON.md)**

## 📖 Documentation

### Project Setup Guides
- **[Hybrid Connection Setup (.NET)](Src/dotnet/RelayTunnelUsingHybridConnection/README.md)** - Complete guide for .NET implementation
- **[Hybrid Connection Setup (TypeScript)](Src/ts/RelayTunnelUsingHybridConnection/README.md)** - Complete guide for Node.js/TypeScript implementation
- **[WCF Relay Setup (.NET)](Src/dotnet/RelayTunnelUsingWCF/README.md)** - Complete guide for legacy implementation

### Additional Resources
- **[Technical Comparison](docs/COMPARISON.md)** - Detailed WCF vs Hybrid Connection comparison and migration guide
- **[Authentication Guide](docs/AUTHENTICATION.md)** - Azure authentication setup for dynamic resources
- **[Troubleshooting Guide](docs/TROUBLESHOOTING.md)** - Common issues and solutions
- **[ARM Automation Details](Src/dotnet/RelayTunnelUsingHybridConnection/README_ARM_AUTOMATION.md)** - Technical implementation details

## 🙏 Acknowledgments

This project is a rewrite inspired by the original work that [Gabriel Gonzalez (gabog)](https://github.com/gabog) created in his project [AzureServiceBusBotRelay](https://github.com/gabog/AzureServiceBusBotRelay).

Part of this code is also based on the work that [Pedro Felix](https://github.com/pmhsfelix) did in his project [WebApi.Explorations.ServiceBusRelayHost](https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost).

## 📝 License

See [LICENSE](LICENSE) file for details.
