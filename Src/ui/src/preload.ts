// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts

import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('electronAPI', {
  getConfig: () => ipcRenderer.invoke('get-config'),
  saveConfig: (config: any) => ipcRenderer.invoke('save-config', config),
  encryptKey: (key: string) => ipcRenderer.invoke('encrypt-key', key),
  decryptKey: (encryptedKey: string) => ipcRenderer.invoke('decrypt-key', encryptedKey),
  startTunnel: (id: string) => ipcRenderer.invoke('start-tunnel', id),
  stopTunnel: (id: string) => ipcRenderer.invoke('stop-tunnel', id),
  getTunnelStatus: (id: string) => ipcRenderer.invoke('get-tunnel-status', id),
  deleteTunnel: (id: string) => ipcRenderer.invoke('delete-tunnel', id),
});
