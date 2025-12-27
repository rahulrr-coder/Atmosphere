import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import HomeView from '@/components/HomeView.vue'

describe('HomeView.vue', () => {
  const defaultProps = {
    homeCity: '',
    emailActive: false,
    favorites: [] as string[],
    cityInput: '',
    searchResults: [] as any[],
    showSuggestions: false
  }

  it('renders the hero message correctly', () => {
    const wrapper = mount(HomeView, { props: defaultProps })
    expect(wrapper.find('h1').text()).toBe('Explore the air around you.')
  })

  it('emits "search" and updates model when typing', async () => {
    const wrapper = mount(HomeView, { props: defaultProps })
    const input = wrapper.find('input')
    await input.setValue('London')
    expect(wrapper.emitted('update:cityInput')?.[0]).toEqual(['London'])
    expect(wrapper.emitted('search')).toBeTruthy()
  })

  it('emits "fetch" when Enter key is pressed', async () => {
    const wrapper = mount(HomeView, { props: { ...defaultProps, cityInput: 'Berlin' } })
    const input = wrapper.find('input')
    await input.trigger('keyup.enter')
    expect(wrapper.emitted('fetch')?.[0]).toEqual(['Berlin'])
  })

  it('renders "My Sanctuary" section only when homeCity exists', async () => {
    const wrapper = mount(HomeView, { 
      props: { ...defaultProps, homeCity: 'New York' } 
    })
    expect(wrapper.find('.section-label').text()).toBe('MY SANCTUARY')
    expect(wrapper.find('h3').text()).toBe('New York')
    await wrapper.find('.text-btn').trigger('click')
    expect(wrapper.emitted('removeSanctuary')).toBeTruthy()
  })

  it('toggles email subscription icon/text based on prop', async () => {
    const wrapperActive = mount(HomeView, { 
      props: { ...defaultProps, homeCity: 'NYC', emailActive: true } 
    })
    expect(wrapperActive.find('.email-toggle-btn').text()).toContain('ON')

    const wrapperInactive = mount(HomeView, { 
      props: { ...defaultProps, homeCity: 'NYC', emailActive: false } 
    })
    expect(wrapperInactive.find('.email-toggle-btn').text()).toContain('OFF')
    await wrapperInactive.find('.email-toggle-btn').trigger('click')
    expect(wrapperInactive.emitted('toggleEmail')).toBeTruthy()
  })

  it('renders favorites list and handles clicks', async () => {
    const favorites = ['Paris', 'Tokyo']
    const wrapper = mount(HomeView, { 
      props: { ...defaultProps, favorites } 
    })
    
    const cards = wrapper.findAll('.fav-card')
    expect(cards.length).toBe(2)

    // Using (cards[0] as any) bypasses the TypeScript check that is causing your error
    expect((cards[0] as any).text()).toContain('Paris')
    
    await (cards[0] as any).find('.card-content').trigger('click')
    expect(wrapper.emitted('fetch')?.[0]).toEqual(['Paris'])
  })

  it('emits "removeFavorite" when the X button is clicked', async () => {
    const favorites = ['Paris']
    const wrapper = mount(HomeView, { 
      props: { ...defaultProps, favorites } 
    })
    const removeBtn = wrapper.find('.remove-btn')
    await removeBtn.trigger('click')
    expect(wrapper.emitted('removeFavorite')?.[0]).toEqual(['Paris'])
  })

  it('shows suggestions list when enabled', async () => {
    const searchResults = [{ name: 'Rome', country: 'Italy' }]
    const wrapper = mount(HomeView, { 
      props: { ...defaultProps, showSuggestions: true, searchResults } 
    })
    
    const items = wrapper.findAll('.suggestions-list li')
    expect(items.length).toBe(1)

    // Explicitly bypass the undefined check with 'any'
    const firstItem = items[0] as any
    expect(firstItem.text()).toContain('Rome')
    
    await firstItem.trigger('click')
    expect(wrapper.emitted('fetch')?.[0]).toEqual(['Rome'])
  })
})