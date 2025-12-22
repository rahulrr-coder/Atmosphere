import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vitest/config' 
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      // Maps '@' to the 'src' directory
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  test: {
    globals: true,
    environment: 'jsdom',
    // Ensures all .spec files are included
    include: ['src/**/*.{test,spec}.{js,ts,jsx,tsx}']
  }
})