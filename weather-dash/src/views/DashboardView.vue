<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

// 1. Define Types (Interfaces)
interface WeatherData {
  city: string;
  temperature: number;
  weather: string;
  aqi: number;
}

interface AiResponse {
  advice: string;
}

const router = useRouter()

// 2. Typed Refs
const weather = ref<WeatherData | null>(null)
const aiAdvice = ref<string>('')
const isAiLoading = ref<boolean>(false)
const cityInput = ref<string>('')
const loading = ref<boolean>(false)
const error = ref<string>('')
const isSubscribed = ref<boolean>(false)
const username = ref<string>(localStorage.getItem('userName') || 'Friend')

onMounted(() => {
  const savedCity = localStorage.getItem('userCity')
  if (savedCity) {
    cityInput.value = savedCity
    fetchWeather(savedCity)
  }
  isSubscribed.value = localStorage.getItem('isSubscribed') === 'true'
})

const fetchWeather = async (city: string) => {
  if (!city) return
  loading.value = true
  error.value = ''
  weather.value = null
  aiAdvice.value = ''

  try {
    // Generics <WeatherData> tell axios what the result looks like
    const res = await axios.get<WeatherData>(`http://localhost:5160/api/Weather/${city}`)
    weather.value = res.data
    
    // Fetch AI
    fetchAiAdvice(city)
  } catch (err) {
    error.value = "Could not find that city."
  } finally {
    loading.value = false
  }
}

const fetchAiAdvice = async (city: string) => {
  isAiLoading.value = true
  try {
    // Generics <AiResponse>
    const res = await axios.get<AiResponse>(`http://localhost:5160/api/Weather/advice?city=${city}`)
    aiAdvice.value = res.data.advice
  } catch (e) {
    aiAdvice.value = "AI is offline right now."
  } finally {
    isAiLoading.value = false
  }
}

const toggleSubscription = () => {
  isSubscribed.value = !isSubscribed.value
  localStorage.setItem('isSubscribed', String(isSubscribed.value))
  alert(isSubscribed.value ? "Emails enabled! 📧" : "Emails disabled 🔕")
}

const logout = () => {
  localStorage.clear()
  router.push('/')
}
</script>

<template>
  <div class="dashboard-wrapper">
    <nav>
      <div class="logo">☀️ WeatherApp</div>
      <div class="nav-actions">
        <button @click="toggleSubscription" class="icon-btn" :class="{ active: isSubscribed }" title="Toggle Emails">
          {{ isSubscribed ? '🔔' : '🔕' }}
        </button>
        <button @click="logout" class="logout-link">Log Out</button>
      </div>
    </nav>

    <main>
      <div class="greeting">
        <h1>Hello, {{ username }}!</h1>
        <p>Here is your daily scoop.</p>
      </div>

      <div class="search-bar">
        <input 
          v-model="cityInput" 
          @keyup.enter="fetchWeather(cityInput)" 
          placeholder="Search city..." 
        />
        <button @click="fetchWeather(cityInput)" :disabled="loading">
          {{ loading ? '...' : '🔍' }}
        </button>
      </div>

      <div v-if="error" class="error-banner">{{ error }}</div>

      <div v-if="weather" class="weather-card fade-in">
        <div class="card-top">
          <div>
            <span class="temp">{{ weather.temperature }}°</span>
            <span class="condition">{{ weather.weather }}</span>
          </div>
          <div class="aqi-box" :class="'aqi-' + weather.aqi">
            <span>AQI</span>
            <strong>{{ weather.aqi }}</strong>
          </div>
        </div>

        <div class="ai-section">
          <div v-if="isAiLoading" class="skeleton-text">Analyzing weather data...</div>
          <div v-else class="ai-bubble">
            <span class="emoji">💡</span>
            <p>"{{ aiAdvice }}"</p>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped>
/* (Same CSS as provided before) */
.dashboard-wrapper { max-width: 600px; margin: 0 auto; padding: 20px; font-family: 'Inter', sans-serif; color: #2d3748; }
nav { display: flex; justify-content: space-between; align-items: center; margin-bottom: 40px; }
.logo { font-weight: 800; font-size: 1.2rem; color: #4a5568; }
.logout-link { background: none; border: none; color: #718096; cursor: pointer; font-weight: 600; }
.logout-link:hover { color: #e53e3e; }
.greeting h1 { font-size: 2rem; margin: 0; color: #1a202c; }
.greeting p { margin-top: 5px; color: #718096; }
.search-bar { display: flex; gap: 10px; margin: 30px 0; }
.search-bar input { flex: 1; padding: 15px; border-radius: 12px; border: 2px solid #edf2f7; font-size: 1rem; outline: none; }
.search-bar input:focus { border-color: #667eea; }
.search-bar button { padding: 0 20px; border-radius: 12px; border: none; background: #667eea; color: white; cursor: pointer; font-size: 1.2rem; }
.weather-card { background: white; padding: 30px; border-radius: 24px; box-shadow: 0 20px 40px rgba(0,0,0,0.05); }
.card-top { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 30px; }
.temp { font-size: 4rem; font-weight: 800; line-height: 1; display: block; letter-spacing: -2px; }
.condition { font-size: 1.2rem; color: #718096; font-weight: 500; }
.aqi-box { text-align: center; padding: 8px 12px; border-radius: 12px; color: white; min-width: 60px; }
.aqi-box span { display: block; font-size: 0.7rem; opacity: 0.9; text-transform: uppercase; }
.aqi-box strong { font-size: 1.2rem; }
.aqi-1, .aqi-2 { background: #48bb78; }
.aqi-3 { background: #ecc94b; color: #744210; }
.aqi-4, .aqi-5 { background: #f56565; }
.ai-section { margin-top: 20px; }
.ai-bubble { background: #ebf4ff; padding: 20px; border-radius: 16px; display: flex; gap: 15px; align-items: flex-start; }
.ai-bubble p { margin: 0; line-height: 1.5; color: #4a5568; font-style: italic; }
.emoji { font-size: 1.5rem; }
.skeleton-text { color: #a0aec0; font-size: 0.9rem; animation: pulse 1.5s infinite; }
@keyframes pulse { 0% { opacity: 0.6; } 50% { opacity: 1; } 100% { opacity: 0.6; } }
.fade-in { animation: fadeIn 0.5s ease-out; }
@keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
.error-banner { background: #fed7d7; color: #c53030; padding: 15px; border-radius: 12px; margin-bottom: 20px; text-align: center; }
.icon-btn { background: none; border: none; font-size: 1.4rem; cursor: pointer; margin-right: 15px; transition: transform 0.2s; }
.icon-btn:hover { transform: scale(1.1); }
</style>