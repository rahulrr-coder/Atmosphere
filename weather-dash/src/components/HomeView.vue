<script setup lang="ts">
import { computed } from 'vue';

interface Props {
  homeCity: string;
  emailActive: boolean;
  favorites: string[];
  cityInput: string;
  searchResults: any[];
  showSuggestions: boolean;
}

const props = defineProps<Props>();
const emit = defineEmits([
  'update:cityInput', 
  'search', 
  'fetch', 
  'toggleEmail', 
  'removeSanctuary', 
  'removeFavorite'
]);

const inputVal = computed({
  get: () => props.cityInput,
  set: (val) => emit('update:cityInput', val)
});

</script>

<template>
  <div class="home-view fade-in">
    <div class="hero">
      <h1>Explore the air around you.</h1>
      
      <div class="cloud-search">
        <input 
          v-model="inputVal" 
          @input="emit('search')" 
          @keyup.enter="emit('fetch', inputVal)" 
          placeholder="Search city (e.g. Paris)..." 
        />
        <button class="arrow-btn" @click="emit('fetch', inputVal)">→</button>
        <ul v-if="props.showSuggestions && props.searchResults.length > 0" class="suggestions-list">
          <li v-for="res in props.searchResults" :key="res.name" @click="emit('fetch', res.name)">
            {{ res.name }} <span class="sub">{{ res.country }}</span>
          </li>
        </ul>
      </div>
    </div>

    <div v-if="props.homeCity" class="section-block">
      <div class="section-header">
        <p class="section-label">MY SANCTUARY</p>
        <button class="text-btn" @click="emit('removeSanctuary')">Remove</button>
      </div>
      
      <div class="home-card">
        <div class="info" @click="emit('fetch', props.homeCity)">
          <h3>{{ props.homeCity }}</h3>
          <p class="sub-text">Primary Location</p>
        </div>
        
        <div class="controls">
          <button 
            class="email-toggle-btn" 
            :class="{ active: props.emailActive }"
            @click="emit('toggleEmail')"
            :title="props.emailActive ? 'Emails are ON' : 'Emails are OFF'"
          >
            <svg v-if="props.emailActive" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path><path d="M13.73 21a2 2 0 0 1-3.46 0"></path></svg>
            <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path><path d="M13.73 21a2 2 0 0 1-3.46 0"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
            <span>{{ props.emailActive ? 'ON' : 'OFF' }}</span>
          </button>
        </div>
      </div>
    </div>
    
    <div class="section-block">
      <p class="section-label">COLLECTION</p>
      <div v-if="props.favorites.length > 0" class="fav-grid">
        <div v-for="fav in props.favorites" :key="fav" class="fav-card">
          <div class="card-content" @click="emit('fetch', fav)">
            <h3>{{ fav }}</h3>
            <span class="view-link">View</span>
          </div>
          <button class="remove-btn" @click.stop="emit('removeFavorite', fav)">✕</button>
        </div>
      </div>
      <div v-else-if="!props.homeCity" class="empty-state">
        <p>Your collection is empty.</p>
      </div>
    </div>
  </div>
</template>