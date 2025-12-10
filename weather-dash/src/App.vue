<script setup lang="ts">
import { ref, onMounted } from 'vue'

// Interfaces
interface WeatherData {
  city: string;
  temperature: number;
  weather: string;
  precipitation: number;
}

interface FavoriteCity {
  id: number;
  name: string;
}

// State
const weather = ref<WeatherData | null>(null)
const favorites = ref<FavoriteCity[]>([])
const cityInput = ref('')
const loading = ref(false)
const error = ref('')

// Load favorites on startup
onMounted(() => refreshFavorites())

const fetchWeather = async (city: string) => {
  if (!city) return;
  loading.value = true;
  error.value = '';
  weather.value = null;

  try {
    const response = await fetch(`http://localhost:5160/Weather/${city}`)
    if (!response.ok) throw new Error("City not found")
    weather.value = await response.json()
  } catch (err) {
    error.value = (err as Error).message
  } finally {
    loading.value = false
  }
}

const refreshFavorites = async () => {
  try {
    const res = await fetch('http://localhost:5160/Favorites')
    if (res.ok) favorites.value = await res.json()
  } catch (e) { console.error("DB Error", e) }
}

const saveCity = async () => {
  if (!weather.value) return;
  
  await fetch('http://localhost:5160/Favorites', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: weather.value.city })
  })
  await refreshFavorites()
}

const deleteCity = async (id: number) => {
  await fetch(`http://localhost:5160/Favorites/${id}`, { method: 'DELETE' })
  await refreshFavorites()
}
</script>

<template>
  <div class="container">
    <h1>☁️ Weather App</h1>
    
    <div class="search-box">
      <input v-model="cityInput" @keyup.enter="fetchWeather(cityInput)" placeholder="Enter City" />
      <button @click="fetchWeather(cityInput)" :disabled="loading">Search</button>
    </div>

    <p v-if="error" class="error">{{ error }}</p>

    <div v-if="weather" class="card">
      <h2>{{ weather.city }}</h2>
      <div class="temp">{{ weather.temperature }}°C</div>
      <p>{{ weather.weather }}</p>
      <button class="save-btn" @click="saveCity">❤️ Save to Favorites</button>
    </div>

    <div v-if="favorites.length > 0" class="favorites">
      <h3>Your Saved Cities</h3>
      <ul>
        <li v-for="fav in favorites" :key="fav.id">
          <span @click="fetchWeather(fav.name)" class="fav-name">{{ fav.name }}</span>
          <button @click="deleteCity(fav.id)" class="delete-btn">×</button>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.container { max-width: 400px; margin: 0 auto; text-align: center; font-family: sans-serif; }
.search-box { display: flex; gap: 10px; justify-content: center; margin-bottom: 20px; }
input { padding: 8px; border-radius: 4px; border: 1px solid #ccc; }
button { padding: 8px 15px; cursor: pointer; }
.card { background: #e5e5e5; padding: 20px; border-radius: 10px; margin: 20px 0; }
.temp { font-size: 2.5rem; font-weight: bold; margin: 10px 0; }
.save-btn { background-color: #4CAF50; color: white; border: none; border-radius: 5px; margin-top: 10px; cursor: pointer; }
.favorites ul { list-style: none; padding: 0; }
.favorites li { display: flex; justify-content: space-between; padding: 10px; border-bottom: 1px solid #eee; }
.fav-name { cursor: pointer; color: #007bff; text-decoration: underline; }
.delete-btn { background: #ff4444; color: white; border: none; border-radius: 50%; width: 24px; height: 24px; cursor: pointer; }
.error { color: red; }
</style>