import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AuthView from '@/views/AuthView.vue'
import axios from 'axios'
import { createRouter, createWebHistory } from 'vue-router'

// 1. Setup a real Router instance to satisfy the "injection" warning
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: { template: '<div>Login</div>' } }, // 🟢 Added root path
    { path: '/dashboard', component: { template: '<div>Dash</div>' } }
  ]
})

// 2. Mock 'vue-router' to spy on the push method
const mockPush = vi.fn()
vi.mock('vue-router', async () => {
  const actual = await vi.importActual('vue-router')
  return {
    ...actual as any,
    useRouter: () => ({
      push: mockPush
    })
  }
})

// 3. Mock Axios
vi.mock('axios')

describe('AuthView.vue', () => {
  beforeEach(async () => {
    vi.clearAllMocks()
    localStorage.clear()
    // Ensure router is ready to prevent async warnings
    router.push('/')
    await router.isReady()
  })

  // Helper options to inject plugins
  const mountOptions = {
    global: {
      plugins: [router]
    }
  }

  it('renders login mode by default', () => {
    const wrapper = mount(AuthView, mountOptions)
    expect(wrapper.find('h1').text()).toBe('Welcome Back')
    expect(wrapper.find('button').text()).toBe('Enter')
  })

  it('toggles to registration mode', async () => {
    const wrapper = mount(AuthView, mountOptions)
    
    await wrapper.find('.toggle-link').trigger('click')
    
    expect(wrapper.find('h1').text()).toBe('Join Atmosphere')
    expect(wrapper.find('input[type="email"]').exists()).toBe(true)
  })

  it('successfully logs in and redirects to dashboard', async () => {
    const wrapper = mount(AuthView, mountOptions)
    
    // Mock successful login response
    ;(axios.post as any).mockResolvedValue({
      data: { email: 'test@atmosphere.com' }
    })

    await wrapper.find('input[type="text"]').setValue('testuser')
    await wrapper.find('input[type="password"]').setValue('password123')
    await wrapper.find('form').trigger('submit.prevent')

    expect(axios.post).toHaveBeenCalledWith(expect.stringContaining('/login'), expect.any(Object))
    
    // Check LocalStorage
    expect(localStorage.getItem('userName')).toBe('testuser')
    
    // Check redirection using our spy
    expect(mockPush).toHaveBeenCalledWith('/dashboard')
  })

  it('shows error message on API failure', async () => {
    const wrapper = mount(AuthView, mountOptions)

    ;(axios.post as any).mockRejectedValue({
      response: { data: 'Invalid Credentials' }
    })

    await wrapper.find('form').trigger('submit.prevent')
    
    // Wait for the DOM update
    // Using vi.waitFor ensures we catch the async state change
    await vi.waitFor(() => {
        expect(wrapper.find('.error-text').text()).toBe('Invalid Credentials')
    })
  })
})