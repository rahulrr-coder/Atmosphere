<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
// Import Child Components
import HomeView from '../components/HomeView.vue'
import CityWeather from '../components/CityWeather.vue'

// Import CSS
import '../assets/css/dashboard.css'

// --- State ---
const router = useRouter()
const username = ref(localStorage.getItem('userName') || 'Guest')
const userEmail = ref(localStorage.getItem('userEmail') || '') 
const homeCity = ref(localStorage.getItem('homeCity') || '') 
const emailActive = ref(localStorage.getItem('emailActive') === 'true') 
const viewState = ref<'HOME' | 'CITY'>('HOME')

const weather = ref<any>(null)
const aiData = ref<any>(null)
const favorites = ref<string[]>([])

// Search State
const cityInput = ref('')
const searchResults = ref<any[]>([])
const showSuggestions = ref(false)

const loading = ref(false)
const aiLoading = ref(false)
const showProfile = ref(false)

// --- ACTIONS ---
const loadFavorites = async () => {
  try {
    const res = await axios.get(`http://localhost:5160/api/Favorites/${username.value}`)
    favorites.value = res.data;
  } catch { favorites.value = [] }
}

const goHome = () => {
  viewState.value = 'HOME';
  weather.value = null;
  cityInput.value = '';
  searchResults.value = [];
  showSuggestions.value = false;
}

const onSearchInput = () => {
  if (cityInput.value.length < 3) { 
    searchResults.value = []; 
    showSuggestions.value = false;
    return; 
  }
  setTimeout(async () => {
    try {
      const res = await axios.get(`http://localhost:5160/api/Weather/search?query=${cityInput.value}`);
      searchResults.value = res.data;
      showSuggestions.value = true;
    } catch { }
  }, 300);
}

// 🟢 FETCH WEATHER (With Frontend Mocking)
const fetchWeather = async (city: string) => {
  if (!city) return;
  
  // 1. Clear Search UI immediately to prevent sticking
  showSuggestions.value = false;
  searchResults.value = [];
  
  loading.value = true;
  viewState.value = 'CITY';
  weather.value = null;
  aiData.value = null;
  cityInput.value = ''; 

  try {
    const res = await axios.get(`http://localhost:5160/api/Weather/${city}`);
    
    // 🟢 MOCK INJECTION: Add the missing fields here since backend doesn't send them
    const mockUV = Math.floor(Math.random() * 6); // Random UV 0-5
    const mockVis = (Math.random() * (10 - 5) + 5).toFixed(1); // Random Visibility 5-10km
    
    weather.value = {
        ...res.data,
        country: res.data.country || "",
        // Inject Missing Data
        uvIndex: mockUV,
        visibility: mockVis, 
        sunrise: "06:20 AM",
        sunset: "06:45 PM",
        dayLength: "12h 25m"
    };

    aiLoading.value = true;
    try {
        const aiRes = await axios.get(`http://localhost:5160/api/Weather/advice?city=${city}`);
        let raw = aiRes.data.advice.replace(/```json|```/g, '').trim();
        if (!raw.startsWith('{')) {
           const start = raw.indexOf('{');
           const end = raw.lastIndexOf('}');
           if (start >= 0 && end > start) raw = raw.substring(start, end - start + 1);
        }
        aiData.value = JSON.parse(raw);
    } catch { 
      aiData.value = { summary: `Enjoy the atmosphere in ${city}.`, outfit: "Wear something comfortable.", safety: "No specific hazards." }; 
    }
    aiLoading.value = false;

  } catch { 
    alert("City not found");
    viewState.value = 'HOME';
  } 
  finally { loading.value = false; }
}

const toggleEmailSub = async () => {
  const newState = !emailActive.value;
  emailActive.value = newState;
  localStorage.setItem('emailActive', String(newState));
  try {
      await axios.put(`http://localhost:5160/api/auth/update-subscription`, {
          username: username.value, isSubscribed: newState, city: homeCity.value
      });
  } catch { emailActive.value = !newState; }
}

const setAsHome = async () => {
  if (!weather.value) return;
  const city = weather.value.city;
  homeCity.value = city;
  localStorage.setItem('homeCity', city);
  try {
      await axios.put(`http://localhost:5160/api/auth/update-subscription`, {
          username: username.value, isSubscribed: emailActive.value, city: city
      });
      loadFavorites();
  } catch {}
}

const removeSanctuary = async () => {
  if(!confirm("Remove from Sanctuary?")) return;
  homeCity.value = '';
  emailActive.value = false;
  localStorage.setItem('homeCity', '');
  localStorage.setItem('emailActive', 'false');
  try {
    await axios.put(`http://localhost:5160/api/auth/update-subscription`, {
        username: username.value, isSubscribed: false, city: ""
    });
  } catch {}
}

const toggleFav = async () => {
  if (!weather.value) return;
  const city = weather.value.city;
  const current = favorites.value.includes(city);
  try {
    if (current) {
      await axios.delete(`http://localhost:5160/api/Favorites/${username.value}/${city}`);
      favorites.value = favorites.value.filter(c => c !== city);
    } else {
      await axios.post(`http://localhost:5160/api/Favorites`, { username: username.value, cityName: city });
      favorites.value.push(city);
    }
  } catch {}
}

const removeDirectly = async (city: string) => {
  if(confirm(`Remove ${city}?`)) {
    await axios.delete(`http://localhost:5160/api/Favorites/${username.value}/${city}`);
    favorites.value = favorites.value.filter(c => c !== city);
  }
}

const logout = () => { localStorage.clear(); router.push('/'); }
const isHome = computed(() => weather.value?.city === homeCity.value);
const isFav = computed(() => weather.value ? favorites.value.includes(weather.value.city) : false);
const otherFavorites = computed(() => favorites.value.filter(c => c !== homeCity.value));

onMounted(() => loadFavorites())
</script>

<template>
  <div class="luxury-app">
    <nav class="navbar">
      <div class="logo" @click="goHome">Atmosphere<span class="dot">.</span></div>
      
      <div v-if="viewState === 'CITY'" class="nav-central-search fade-in">
        <input 
          v-model="cityInput" 
          @input="onSearchInput"
          @keyup.enter="fetchWeather(cityInput)" 
          placeholder="Search another city..." 
        />
        <ul v-if="showSuggestions && searchResults.length > 0" class="nav-suggestions">
           <li v-for="res in searchResults" :key="res.name" @click="fetchWeather(res.name)">
             {{ res.name }} <span class="sub-nav">{{ res.country }}</span>
           </li>
        </ul>
      </div>

      <div class="profile-trigger" @click="showProfile = !showProfile">
        <span class="initial">{{ username.charAt(0).toUpperCase() }}</span>
        <div v-if="showProfile" class="dropdown fade-in">
          <div class="meta">
            <span class="u-name">{{ username }}</span>
            <span class="u-email">{{ userEmail }}</span>
          </div>
          <div class="divider"></div>
          <button @click="logout">Log Out</button>
        </div>
      </div>
    </nav>

    <main class="main-body">
      <HomeView 
        v-if="viewState === 'HOME'"
        :homeCity="homeCity"
        :emailActive="emailActive"
        :favorites="otherFavorites"
        :cityInput="cityInput"
        :searchResults="searchResults"
        :showSuggestions="showSuggestions"
        @update:cityInput="cityInput = $event"
        @search="onSearchInput"
        @fetch="fetchWeather"
        @toggleEmail="toggleEmailSub"
        @removeSanctuary="removeSanctuary"
        @removeFavorite="removeDirectly"
      />

      <CityWeather 
        v-if="viewState === 'CITY'"
        :weather="weather"
        :aiData="aiData"
        :loading="loading"
        :aiLoading="aiLoading"
        :isHome="isHome"
        :isFav="isFav"
        @toggleFav="toggleFav"
        @setHome="setAsHome"
      />
    </main>
  </div>
</template>