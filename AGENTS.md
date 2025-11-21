# Azure Relay Tunneling Utility - Shared GUI & Modernization Plan

This document tracks the progress of modernizing the Azure Relay Tunneling Utility suite by introducing a shared Electron-based GUI and a unified configuration system.

## Project Goals
1.  **Unified Configuration**: Move from `.env`/`appsettings.json` to a shared `%APPDATA%\AzureRelayTunnel\config.json` file.
2.  **Security**: Encrypt sensitive data (keys) in the config file using DPAPI (Windows), ensuring interoperability between Node.js and .NET.
3.  **Shared GUI**: Create a generic Electron "Launcher" application that can:
    - Manage configuration (add/edit/remove tunnels).
    - Launch tunnels using the embedded TypeScript logic.
    - Launch tunnels using the embedded .NET executables.
4.  **Standalone CLI**: Maintain headless CLI capability for all three existing tools (TS, .NET 8, .NET 4.8), adding interactive setup if config is missing.

## Architecture

### Directory Structure
- `Src/gui`: **[NEW]** Electron + React + TypeScript application.
- `Src/ts`: Existing TypeScript CLI/Library.
- `Src/dotnet`: Existing .NET solutions.

### Configuration Schema (`config.json`)
```json
{
  "version": 1,
  "tunnels": [
    {
      "id": "uuid",
      "name": "Production DB",
      "type": "typescript" | "dotnet-core" | "dotnet-wcf",
      "relayNamespace": "contoso-relay",
      "hybridConnectionName": "db-tunnel",
      "keyName": "RootManageSharedAccessKey",
      "encryptedKey": "BASE64_DPAPI_BLOB",
      "targetHost": "localhost",
      "targetPort": 1433
    }
  ]
}
```

## Roadmap & Status

### Phase 1: Foundation & Standards
- [ ] Define unified configuration schema.
- [ ] **TypeScript**: Implement `ConfigService` with `node-dpapi` for encryption/decryption.
- [ ] **.NET 8**: Implement `ConfigService` using `System.Security.Cryptography.ProtectedData`.
- [ ] **.NET 4.8**: Implement `ConfigService` using `System.Security.Cryptography.ProtectedData`.
- [ ] Verify DPAPI interoperability (encrypt in Node, decrypt in .NET, and vice-versa).

### Phase 2: TypeScript Refactoring
- [ ] Refactor `Src/ts` to export `DispatcherService` and `RelayResourceManager` as a library.
- [ ] Update `Src/ts/src/index.ts` (CLI) to use the new library and `ConfigService`.
- [ ] Add interactive setup mode to CLI (using `inquirer` or similar).
- [ ] Add `config edit` and `config show` commands.

### Phase 3: .NET Refactoring
- [ ] **RelayTunnelUsingHybridConnection (.NET 8)**:
    - [ ] Switch to `config.json` loading.
    - [ ] Add interactive setup mode.
    - [ ] Add CLI verbs (`config edit`, `config show`).
- [ ] **RelayTunnelUsingWCF (.NET 4.8)**:
    - [ ] Switch to `config.json` loading.
    - [ ] Add interactive setup mode.
    - [ ] Add CLI verbs.

### Phase 4: Electron GUI Implementation
- [ ] Scaffold `Src/gui` using Electron Forge + React + TypeScript.
- [ ] Implement shared UI components (Tunnel List, Config Form).
- [ ] Integrate `Src/ts` logic (import directly).
- [ ] Implement "Process Manager" to spawn .NET executables as child processes.
- [ ] Implement "Asset Bundling" to include compiled .NET binaries in the Electron distribution.

### Phase 5: Polish & Release
- [ ] End-to-End testing of all 3 tunnel types via GUI.
- [ ] End-to-End testing of CLI modes.
- [ ] Create build pipeline (GitHub Actions?) to build .NET apps -> copy to GUI assets -> build Electron app.

## Technical Notes
- **DPAPI Scope**: Must use `DataProtectionScope.CurrentUser` for interoperability and permissions.
- **Paths**: 
  - Config: `path.join(process.env.APPDATA, "AzureRelayTunnel", "config.json")`
- **Inter-Process Communication**: GUI will use Node's `child_process` to run .NET executables, passing config IDs or parameters as needed.

## Current Context
- Working Directory: `/d:/personal/AzureRelayTunnelingUtility`
- Repos:
    - `Src/ts`: TypeScript project
    - `Src/dotnet/RelayTunnelUsingHybridConnection`: .NET 8 project
    - `Src/dotnet/RelayTunnelUsingWCF`: .NET 4.8 project
