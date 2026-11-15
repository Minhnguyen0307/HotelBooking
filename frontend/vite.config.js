import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': 'http://127.0.0.1:5190',
      '/images': 'http://127.0.0.1:5190',
      '/uploads': 'http://127.0.0.1:5190'
    }
  }
});
