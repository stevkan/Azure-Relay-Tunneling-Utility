---
layout: default
title: Downloads
---

# Azure Relay Tunneling Utility - Downloads

---

## 🚀 Relay Tunnel Using Hybrid Connection (Recommended)

**Latest Version: v1.6.1** | [Documentation](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/RelayTunnelUsingHybridConnection) | [Release Notes](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/tag/hybrid-v1.6.1)

**Technology:** Azure Relay Hybrid Connections (.NET 8)  
**Status:** ✅ Actively maintained, production-ready

### Downloads

- **[Windows (x64)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/download/hybrid-v1.6.1/AzureRelayTunnelingUtility-HC-v1.6.1-Win-x64.zip)**
- **[Linux (x64)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/download/hybrid-v1.6.1/AzureRelayTunnelingUtility-HC-v1.6.1-Linux-x64.zip)**
- **[macOS (x64)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/download/hybrid-v1.6.1/AzureRelayTunnelingUtility-HC-v1.6.1-macOS-x64.zip)**

### Quick Start
1. Download the appropriate zip for your platform
2. Extract the contents
3. Rename `appsettings-template.json` to `appsettings.json`
4. Configure your Azure Relay settings
5. Run the executable

---

## ⚙️ Relay Tunnel Using WCF (Legacy)

**Latest Version: v1.5.5** | [Documentation](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/RelayTunnelUsingWCF) | [Release Notes](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/tag/wcf-v1.5.5)

**Technology:** WCF Relay (.NET Framework 4.8)  
**Status:** ⚠️ **Legacy - uses deprecated Azure libraries**

⚠️ **Security Warning:** This version uses deprecated Azure libraries with no ongoing security updates. Use the Hybrid Connection version for production environments.

### Downloads

- **[Windows (x86)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/download/wcf-v1.5.5/AzureRelayTunnelingUtility-WCF-v1.5.5-Win-x86.zip)**

### Quick Start
1. Download the zip
2. Extract the contents
3. Rename `appsettings-template.json` to `appsettings.json`
4. Configure your Azure Relay settings
5. Run the executable

---

## 📊 Comparison

| Feature | Hybrid Connection | WCF Relay |
|---------|------------------|-----------|
| **.NET Version** | .NET 8 | .NET Framework 4.8 |
| **Platforms** | Windows, Linux, macOS | Windows only |
| **HTTP/REST Support** | ✅ Yes | ✅ Yes |
| **WebSocket Support** | ✅ Yes | ❌ No |
| **Production Ready** | ✅ Yes | ⚠️ No - Security Risk |
| **Security Updates** | ✅ Active support | ❌ Deprecated libraries |

---

## 📖 Documentation

- [Main README](https://github.com/stevkan/Azure-Relay-Tunneling-Utility)
- [Hybrid Connection Setup Guide](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/RelayTunnelUsingHybridConnection)
- [WCF Relay Setup Guide](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/RelayTunnelUsingWCF)
- [Troubleshooting](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/docs/TROUBLESHOOTING.md)
- [All Releases](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases)

---

**Need help?** [Open an issue](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/issues) on GitHub.
