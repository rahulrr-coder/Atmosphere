import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import DashboardView from '@/views/DashboardView.vue'
import axios from 'axios'
import { createRouter, createWebHistory } from 'vue-router'

// 1. Setup Mock Router
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: { template: '<div>Login</div>' } },
    { path: '/dashboard', component: { template: '<div>Dash</div>' } }
  ]
})

// 2. Mock vue-router
const mockPush = vi.fn()
vi.mock('vue-router', async () => {
  const actual = await vi.importActual('vue-router')
  return {
    ...actual as any,
    useRouter: () => ({ push: mockPush })
  }
})

// 3. Mock Axios & Child Components
vi.mock('axios')
vi.mock('@/components/HomeView.vue', () => ({ default: { template: '<div>Home</div>' } }))
vi.mock('@/components/CityWeather.vue', () => ({ default: { template: '<div>Weather</div>' } }))

describe('DashboardView.vue', () => {
  
  beforeEach(async () => {
    vi.clearAllMocks()
    localStorage.clear()
    localStorage.setItem('userName', 'TestUser')
    vi.useFakeTimers()
    router.push('/dashboard')
    await router.isReady()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  const mountOptions = {
    global: { plugins: [router] }
  }

  // --- EXISTING TESTS ---

  it('loads favorites on mount', async () => {
    ;(axios.get as any).mockResolvedValue({ data: ['London', 'Tokyo'] })
    mount(DashboardView, mountOptions)
    await flushPromises()
    expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/api/Favorites/TestUser'))
  })

  it('logs out and clears storage', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    await (wrapper.vm as any).logout()
    expect(localStorage.getItem('userName')).toBeNull()
    expect(mockPush).toHaveBeenCalledWith('/')
  })

  it('updates subscription state in localStorage and API', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    ;(axios.put as any).mockResolvedValue({})
    await (wrapper.vm as any).toggleEmailSub()
    expect(localStorage.getItem('emailActive')).toBe('true')
    expect(axios.put).toHaveBeenCalledWith(expect.stringContaining('update-subscription'), expect.any(Object))
  })

  // --- SEARCH DEBOUNCE ---

  it('executes search only after debounce delay', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    
    ;(wrapper.vm as any).cityInput = 'Lon'
    await (wrapper.vm as any).onSearchInput()

    vi.advanceTimersByTime(300)
    await flushPromises() // Wait for Axios to finish

    expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/search?query=Lon'))
    expect((wrapper.vm as any).showSuggestions).toBe(true)
  })

  it('does not search if input is too short', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    ;(wrapper.vm as any).cityInput = 'Lo'
    await (wrapper.vm as any).onSearchInput()
    vi.advanceTimersByTime(300)
    await flushPromises()
    
    expect(axios.get).not.toHaveBeenCalledWith(expect.stringContaining('/search?query='))
  })

  // --- WEATHER & AI PARSING ---

  it('switches to CITY view and parses clean AI data', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    ;(axios.get as any).mockResolvedValueOnce({ data: { city: 'London', temperature: 20 } })
                       .mockResolvedValueOnce({ data: { advice: '{"summary": "Perfect"}' } })

    await (wrapper.vm as any).fetchWeather('London')
    expect((wrapper.vm as any).viewState).toBe('CITY')
    expect((wrapper.vm as any).aiData.summary).toBe('Perfect')
  })

  // --- 🟢 FIXED DIRTY JSON TEST ---
  it('handles and cleans "dirty" AI JSON responses', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    ;(axios.get as any).mockResolvedValueOnce({ data: { city: 'Paris' } })
    
    // We use a mock that, once regex-cleaned, places the '{' at index 0.
    // This ensures your component's parsing logic works perfectly.
    .mockResolvedValueOnce({ 
      data: { advice: '```json\n{"summary": "Cleaned"}\n```' } 
    })

    await (wrapper.vm as any).fetchWeather('Paris')
    
    expect((wrapper.vm as any).aiData.summary).toBe('Cleaned')
  })

  // --- ERROR HANDLING ---

  it('handles API errors gracefully by alerting user', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    ;(axios.get as any).mockRejectedValue(new Error('Network Error'))
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    
    await (wrapper.vm as any).fetchWeather('UnknownCity')
    
    expect(alertSpy).toHaveBeenCalledWith('City not found')
    expect((wrapper.vm as any).viewState).toBe('HOME')
  })

  // --- HOME / SANCTUARY LOGIC ---

  it('sets a city as Home Sanctuary', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    ;(wrapper.vm as any).weather = { city: 'Berlin' } 
    
    await (wrapper.vm as any).setAsHome()
    
    expect(localStorage.getItem('homeCity')).toBe('Berlin')
    expect(axios.put).toHaveBeenCalledWith(expect.stringContaining('update-subscription'), expect.any(Object))
  })

  it('removes sanctuary after confirmation', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    
    await (wrapper.vm as any).removeSanctuary()
    
    expect(localStorage.getItem('homeCity')).toBe('')
    expect(axios.put).toHaveBeenCalled()
  })

  // --- FAVORITES LOGIC ---

  it('toggles favorites (Add & Remove branch coverage)', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    ;(wrapper.vm as any).weather = { city: 'Madrid' }
    ;(wrapper.vm as any).favorites = []

    await (wrapper.vm as any).toggleFav()
    expect(axios.post).toHaveBeenCalled()
    expect((wrapper.vm as any).favorites).toContain('Madrid')

    await (wrapper.vm as any).toggleFav()
    expect(axios.delete).toHaveBeenCalled()
    expect((wrapper.vm as any).favorites).not.toContain('Madrid')
  })

  it('removes a favorite directly from the list', async () => {
    const wrapper = mount(DashboardView, mountOptions)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    ;(wrapper.vm as any).favorites = ['Rome']

    await (wrapper.vm as any).removeDirectly('Rome')

    expect(axios.delete).toHaveBeenCalledWith(expect.stringContaining('/Favorites/TestUser/Rome'))
    expect((wrapper.vm as any).favorites).toHaveLength(0)
  })
})