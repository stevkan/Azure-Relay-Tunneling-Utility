import { app, BrowserWindow, ipcMain } from 'electron';
import path from 'node:path';
import started from 'electron-squirrel-startup';
import { ConfigService } from '@shared/services/ConfigService';
import { AppConfig } from '@shared/types/Configuration';
import { TunnelManager } from './TunnelManager';

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (started) {
  app.quit();
}

const configService = new ConfigService();
const tunnelManager = new TunnelManager(configService);

const createWindow = () => {
  // Create the browser window.
  const mainWindow = new BrowserWindow({
    width: 1000,
    height: 800,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
    },
  });

  // and load the index.html of the app.
  if (MAIN_WINDOW_VITE_DEV_SERVER_URL) {
    mainWindow.loadURL(MAIN_WINDOW_VITE_DEV_SERVER_URL);
  } else {
    mainWindow.loadFile(
      path.join(__dirname, `../renderer/${MAIN_WINDOW_VITE_NAME}/index.html`),
    );
  }

  // Open the DevTools.
  mainWindow.webContents.openDevTools();
};

// IPC Handlers
ipcMain.handle('get-config', () => {
  return configService.loadConfig();
});

ipcMain.handle('save-config', (event, config: AppConfig) => {
  configService.saveConfig(config);
  return true;
});

ipcMain.handle('encrypt-key', (event, key: string) => {
  try {
    return configService.encryptKey(key);
  } catch (e: any) {
    console.error("Encryption failed:", e);
    throw e;
  }
});

ipcMain.handle('decrypt-key', (event, encryptedKey: string) => {
  try {
    return configService.decryptKey(encryptedKey);
  } catch (e: any) {
    console.error("Decryption failed:", e);
    throw e;
  }
});

ipcMain.handle('start-tunnel', async (event, id: string) => {
  await tunnelManager.startTunnel(id);
  return true;
});

ipcMain.handle('stop-tunnel', async (event, id: string) => {
  await tunnelManager.stopTunnel(id);
  return true;
});

ipcMain.handle('get-tunnel-status', (event, id: string) => {
  return tunnelManager.getStatus(id);
});

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', createWindow);

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});

// In this file you can include the rest of your app's specific main process
// code. You can also put them in separate files and import them here.
