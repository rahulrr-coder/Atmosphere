/// <reference types="vite/client" />

// This tells TypeScript how to handle .vue imports
declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}