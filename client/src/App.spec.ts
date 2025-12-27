import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import App from '@/App.vue'

// 1. Create a minimal router mock so RouterView has a context to render in
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { 
      path: '/', 
      component: { template: '<div id="mock-home">Home Page</div>' } 
    }
  ]
})

describe('App.vue', () => {
  it('renders the router view container', async () => {
    // Push to the initial route
    router.push('/')
    await router.isReady()

    const wrapper = mount(App, {
      global: {
        plugins: [router] // Injects the router into App
      }
    })

    // Verify that the App component exists and renders the current route's component
    expect(wrapper.exists()).toBe(true)
    expect(wrapper.html()).toContain('id="mock-home"')
  })
})