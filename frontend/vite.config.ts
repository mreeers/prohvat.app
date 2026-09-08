import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:8081',
        changeOrigin: true
      },
      '/hubs': {
        target: 'http://localhost:8081',
        ws: true
      },
      '/s3': {
        target: 'http://localhost:9000',
        rewrite: (path) => path.replace(/^\/s3/, ''),
        changeOrigin: true
      }
    }
  }
})
