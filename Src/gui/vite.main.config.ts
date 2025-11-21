import { defineConfig } from 'vite';
import path from 'path';

// https://vitejs.dev/config
export default defineConfig({
  resolve: {
    alias: {
      '@shared': path.resolve(__dirname, '../ts/RelayTunnelUsingHybridConnection/src'),
    },
  },
});
