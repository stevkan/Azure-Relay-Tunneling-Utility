---
layout: default
title: Downloads
---

# Azure Relay Tunneling Utility - Downloads

---

## 🚀 Relay Tunnel Using Hybrid Connection (Recommended)

**Latest Version: v1.6.3** | [Documentation](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/dotnet/RelayTunnelUsingHybridConnection) | [Release Notes](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/tag/hybrid-v1.6.3)

**Technology:** Azure Relay Hybrid Connections (.NET 8)
**Status:** ✅ Actively maintained, production-ready

### Downloads

**Pre-built Binaries (Windows Only):**
- **[Windows Exe (x64)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/download/hybrid-v1.6.3/RelayTunnel-HybridConnection-DotNet-Installer.exe)**
- **[Windows (x64)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/download/hybrid-v1.6.2/RelayTunnel-HC-NET-v1.6.3-Win-x64.zip)**

### Linux & macOS Users
Official releases currently provide pre-built binaries for **Windows only**. Linux and macOS users can build from source.

👉 **[View Build Instructions for Linux/macOS](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/dotnet/RelayTunnelUsingHybridConnection#-publishing-executable)**

### Quick Start
1. Download the appropriate zip for your platform
2. Extract the contents
3. Rename `appsettings-template.json` to `appsettings.json`
4. Configure your Azure Relay settings
5. Run the executable

---

## 🚀 Node.js / TypeScript Version (Beta)

**Version: 0.9.0-beta.5** | [Documentation](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/ts/RelayTunnelUsingHybridConnection) | [Release Notes](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/tag/ts-v0.9.0-beta.5)

**Technology:** Node.js 20+ (Hybrid Connections)  
**Status:** 🧪 **Beta** - Known issues with DirectLine/Web Chat.

### Downloads

- **[Windows (x64)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/download/ts-v0.9.0-beta.5/RelayTunnel-HC-TS-v0.9.0-beta.5-Win-x64.zip)**

### Windows, Linux & macOS Users
Windows, Linux and macOS users must build from source.

👉 **[View Build Instructions](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/ts/RelayTunnelUsingHybridConnection#-publishing-executable)**

### Quick Start
1. Download the appropriate zip for your platform
2. Extract the contents
3. Rename `.env-template` to `.env`
4. Configure your Azure Relay settings
5. Run the executable

---

## 🚀 Relay Tunnel Using WCF (Legacy)

**Latest Version: v1.5.7** | [Documentation](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/dotnet/RelayTunnelUsingWCF) | [Release Notes](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/tag/wcf-v1.5.7)

**Technology:** WCF Relay (.NET Framework 4.8)  
**Status:** ⚠️ **Legacy - uses deprecated Azure libraries**

⚠️ **Security Warning:** This version uses deprecated Azure libraries with no ongoing security updates. Use the Hybrid Connection version for production environments.

### Downloads

- **[Windows (x86)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases/download/wcf-v1.5.7/RelayTunnel-WCF-NET-v1.5.7-Win-x86.zip)**

### Windows, Linux & macOS Users
Windows must build from source. Linux and macOS options are not supported by WCF.

👉 **[View Build Instructions for Windows](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/dotnet/RelayTunnelUsingWCF#-publishing-executable)**

### Quick Start
1. Download the zip
2. Extract the contents
3. Rename `appsettings-template.json` to `appsettings.json`
4. Configure your Azure Relay settings
5. Run the executable

---

## 📊 Comparison

| Feature | Hybrid Connection (.NET) | Hybrid Connection (TS) | WCF Relay |
|---------|--------------------------|------------------------|-----------|
| **Runtime** | .NET 8 | Node.js 20+ | .NET Framework 4.8 |
| **Platforms** | Windows (Release), Linux/macOS (Source) | Windows/Linux/macOS (Source) | Windows only |
| **HTTP/REST Support** | ✅ Yes | ✅ Yes | ✅ Yes |
| **WebSocket Support** | ✅ Yes | ✅ Yes | ❌ No |
| **Production Ready** | ✅ Yes | 🧪 Beta | ⚠️ No - Security Risk |
| **Security Updates** | ✅ Active support | ✅ Active support | ❌ Deprecated libraries |

---

## 📖 Documentation

- [Main README](https://github.com/stevkan/Azure-Relay-Tunneling-Utility)
- [Hybrid Connection Setup Guide (.NET)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/dotnet/RelayTunnelUsingHybridConnection)
- [Hybrid Connection Setup Guide (TypeScript)](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/ts/RelayTunnelUsingHybridConnection)
- [WCF Relay Setup Guide](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/Src/dotnet/RelayTunnelUsingWCF)
- [Troubleshooting](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/tree/main/docs/TROUBLESHOOTING.md)
- [All Releases](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/releases)

---

**Need help?** [Open an issue](https://github.com/stevkan/Azure-Relay-Tunneling-Utility/issues) on GitHub.
