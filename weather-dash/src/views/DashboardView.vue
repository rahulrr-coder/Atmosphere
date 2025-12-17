<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

// Interfaces
interface DayPart { partName: string; temp: number; condition: string; }
interface WeatherData {
  city: string; currentTemp: number; currentCondition: string; description: string;
  humidity: number; windSpeed: number; aqi: number; maxTemp: number; minTemp: number;
  dayParts: DayPart[];
}
interface AiAdvice { headline: string; story: string; vibe: string; }

const router = useRouter()
const weather = ref<WeatherData | null>(null)
const aiData = ref<AiAdvice | null>(null)
const cityInput = ref('')
const loading = ref(false)
const aiLoading = ref(false)
const username = ref(localStorage.getItem('userName') || 'Friend')

// --- Translators ---
const getWind = (s: number) => s < 5 ? "Calm" : s < 20 ? "Breezy" : "Windy";
const getHum = (h: number) => h < 40 ? "Dry" : h < 70 ? "Comfy" : "Sticky";

const fetchWeather = async (city: string) => {
  if (!city) return;
  loading.value = true;
  weather.value = null; 
  aiData.value = null;

  try {
    const res = await axios.get<WeatherData>(`http://localhost:5160/api/Weather/${city}`);
    weather.value = res.data;
    
    // Call AI separately
    aiLoading.value = true;
    try {
        const aiRes = await axios.get(`http://localhost:5160/api/Weather/advice?city=${city}`);
        aiData.value = JSON.parse(aiRes.data.advice.replace(/```json|```/g, '').trim());
    } catch { /* AI Fail silent */ }
    aiLoading.value = false;
    
  } catch (e) { alert("City not found"); } 
  finally { loading.value = false; }
}

onMounted(() => {
  const savedCity = localStorage.getItem('userCity');
  if (savedCity) { cityInput.value = savedCity; fetchWeather(savedCity); }
})
</script>

<template>
  <div class="dashboard">
    <header>
      <h1>Hello, {{ username }}</h1>
      <button @click="router.push('/')" class="btn-text">Log Out</button>
    </header>

    <div class="search-box">
      <input v-model="cityInput" @keyup.enter="fetchWeather(cityInput)" placeholder="Search City..." />
    </div>

    <div v-if="weather" class="content slide-up">
      
      <div class="card ai-card">
        <div v-if="aiLoading" class="pulse">✨ AI is writing your forecast...</div>
        <div v-else>
          <h2>{{ aiData?.headline }}</h2>
          <p>"{{ aiData?.story }}"</p>
          <span class="badge">Mood: {{ aiData?.vibe }}</span>
        </div>
      </div>

      <div class="card weather-card">
        <div class="top-row">
          <div>
            <span class="big-temp">{{ Math.round(weather.currentTemp) }}°</span>
            <span class="desc">{{ weather.currentCondition }}</span>
          </div>
          <div class="details">
            <div>💧 {{ getHum(weather.humidity) }}</div>
            <div>💨 {{ getWind(weather.windSpeed) }}</div>
            <div :class="'aqi-'+weather.aqi">😷 AQI {{ weather.aqi }}</div>
          </div>
        </div>
        <div class="range">High: {{ Math.round(weather.maxTemp) }}° • Low: {{ Math.round(weather.minTemp) }}°</div>
      </div>

      <h3 class="label">Forecast</h3>
      <div class="grid-3">
        <div v-for="part in weather.dayParts" :key="part.partName" class="mini-card">
          <div class="part-name">{{ part.partName }}</div>
          <div class="part-temp">{{ Math.round(part.temp) }}°</div>
          <div class="part-cond">{{ part.condition }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Clean & Minimal UI */
.dashboard { max-width: 500px; margin: 0 auto; padding: 20px; font-family: 'Inter', sans-serif; color: #333; }
header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.btn-text { background: none; border: none; color: #666; cursor: pointer; font-weight: 600; }

.search-box input { width: 100%; padding: 14px; border-radius: 12px; border: 1px solid #eee; background: #f9f9f9; font-size: 1rem; margin-bottom: 25px; }

.card { background: white; padding: 20px; border-radius: 20px; box-shadow: 0 10px 30px rgba(0,0,0,0.05); margin-bottom: 15px; }

/* AI Card Gradients */
.ai-card { background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); color: white; }
.ai-card h2 { margin: 0 0 10px 0; font-size: 1.2rem; }
.ai-card p { opacity: 0.9; font-style: italic; margin-bottom: 15px; line-height: 1.5; }
.badge { background: rgba(255,255,255,0.2); padding: 5px 12px; border-radius: 20px; font-size: 0.85rem; font-weight: bold; }

/* Weather Card */
.top-row { display: flex; justify-content: space-between; align-items: center; }
.big-temp { font-size: 3.5rem; font-weight: 800; line-height: 1; }
.desc { display: block; font-size: 1.1rem; color: #666; margin-top: 5px; }
.details div { font-size: 0.9rem; margin-bottom: 5px; font-weight: 500; color: #555; }
.aqi-4, .aqi-5 { color: #ef4444; font-weight: bold; }
.range { margin-top: 15px; pt: 15px; border-top: 1px solid #eee; color: #888; font-size: 0.9rem; text-align: center; }

/* Grid */
.grid-3 { display: flex; gap: 10px; }
.mini-card { flex: 1; background: white; padding: 15px; border-radius: 16px; text-align: center; box-shadow: 0 5px 15px rgba(0,0,0,0.03); }
.part-name { font-size: 0.75rem; text-transform: uppercase; color: #999; margin-bottom: 5px; }
.part-temp { font-size: 1.4rem; font-weight: bold; margin-bottom: 5px; }
.part-cond { font-size: 0.8rem; color: #666; }

/* Animations */
.slide-up { animation: slideUp 0.6s cubic-bezier(0.16, 1, 0.3, 1); }
@keyframes slideUp { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }
.pulse { animation: pulse 1.5s infinite; opacity: 0.8; }
@keyframes pulse { 0% { opacity: 0.6; } 50% { opacity: 1; } 100% { opacity: 0.6; } }
</style>