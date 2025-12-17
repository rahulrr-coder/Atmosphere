<script setup lang="ts">
import { ref, onMounted, computed, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

// Types
interface DayPart { partName: string; temp: number; condition: string; }
interface WeatherData { 
  city: string; 
  country?: string; 
  currentTemp: number; 
  currentCondition: string; 
  humidity: number; 
  windSpeed: number; 
  aqi: number; 
  dayParts: DayPart[]; 
}
interface AiAdvice { summary: string; outfit: string; safety: string; }
interface CityResult { name: string; country: string; }

// State
const router = useRouter()
const username = ref(localStorage.getItem('userName') || 'Guest')
const userEmail = ref(localStorage.getItem('userEmail') || '') 
const homeCity = ref(localStorage.getItem('homeCity') || '') 
const viewState = ref<'HOME' | 'CITY'>('HOME')

const weather = ref<WeatherData | null>(null)
const aiData = ref<AiAdvice | null>(null)
const favorites = ref<string[]>([])

// Search
const cityInput = ref('')
const searchResults = ref<CityResult[]>([])
const showSuggestions = ref(false)

const loading = ref(false)
const aiLoading = ref(false)
const showProfile = ref(false)

// Computed
const weatherClass = computed(() => {
  if (!weather.value) return 'bg-default';
  const cond = weather.value.currentCondition.toLowerCase();
  if (cond.includes('clear') || cond.includes('sun')) return 'bg-sunny';
  if (cond.includes('cloud')) return 'bg-cloudy';
  if (cond.includes('rain') || cond.includes('drizzle')) return 'bg-rainy';
  return 'bg-default';
});

const isHome = computed(() => weather.value?.city === homeCity.value);
const otherFavorites = computed(() => favorites.value.filter(c => c !== homeCity.value));

// Actions
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
}

const onSearchInput = () => {
  if (cityInput.value.length < 3) { searchResults.value = []; return; }
  setTimeout(async () => {
    try {
      const res = await axios.get<CityResult[]>(`http://localhost:5160/api/Weather/search?query=${cityInput.value}`);
      searchResults.value = res.data;
      showSuggestions.value = true;
    } catch { }
  }, 300);
}

const fetchWeather = async (city: string) => {
  if (!city) return;
  loading.value = true;
  viewState.value = 'CITY';
  showSuggestions.value = false;
  weather.value = null;
  aiData.value = null;
  cityInput.value = ''; 

  try {
    const res = await axios.get<WeatherData>(`http://localhost:5160/api/Weather/${city}`);
    weather.value = res.data;
    if(!weather.value.country) weather.value.country = ""; 

    aiLoading.value = true;
    try {
        const aiRes = await axios.get(`http://localhost:5160/api/Weather/advice?city=${city}`);
        const raw = aiRes.data.advice.replace(/```json|```/g, '').trim();
        aiData.value = JSON.parse(raw);
    } catch { 
      aiData.value = { 
        summary: `Enjoy the atmosphere in ${city}.`, 
        outfit: "-", 
        safety: "-" 
      }; 
    }
    aiLoading.value = false;

  } catch { 
    alert("City not found");
    viewState.value = 'HOME';
  } 
  finally { loading.value = false; }
}

const toggleEmailSub = async (city: string, currentState: boolean) => {
  try {
      await axios.put(`http://localhost:5160/api/auth/update-subscription`, {
          username: username.value, isSubscribed: currentState, city: city
      });
      if (currentState) {
        homeCity.value = city;
        localStorage.setItem('homeCity', city);
      } else {
        if(confirm("Turn off daily emails?")) {
           homeCity.value = '';
           localStorage.setItem('homeCity', '');
        } else {
           loadFavorites(); return; 
        }
      }
      loadFavorites();
  } catch { alert("Update failed"); }
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

const setAsHome = async () => {
  if (!weather.value) return;
  if(confirm(`Make ${weather.value.city} your Sanctuary (Home City)?`)) {
    toggleEmailSub(weather.value.city, true);
  }
}

const removeDirectly = async (city: string) => {
  if(confirm(`Remove ${city} from favorites?`)) {
    await axios.delete(`http://localhost:5160/api/Favorites/${username.value}/${city}`);
    favorites.value = favorites.value.filter(c => c !== city);
  }
}

const logout = () => { localStorage.clear(); router.push('/'); }
const isFav = () => weather.value ? favorites.value.includes(weather.value.city) : false;

onMounted(() => loadFavorites())
</script>

<template>
  <div class="luxury-app">
    <component is="style">
      @import url('https://fonts.googleapis.com/css2?family=Lato:wght@300;400;700&family=Playfair+Display:ital,wght@0,400;0,600;0,700;1,400&display=swap');
    </component>

    <nav class="navbar">
      <div class="logo" @click="goHome">Atmosphere<span class="dot">.</span></div>
      
      <div v-if="viewState === 'CITY'" class="nav-search-container">
        <input 
          v-model="cityInput" 
          @input="onSearchInput"
          @keyup.enter="fetchWeather(cityInput)" 
          placeholder="Search another city..." 
        />
        <ul v-if="showSuggestions && searchResults.length > 0" class="nav-suggestions">
           <li v-for="res in searchResults" :key="res.name" @click="fetchWeather(res.name)">
             {{ res.name }} <span class="sub">{{ res.country }}</span>
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

      <div v-if="viewState === 'HOME'" class="home-view fade-in">
        
        <div class="hero">
          <h1>Explore the air around you.</h1>
          
          <div class="cloud-search">
            <input 
              v-model="cityInput" 
              @input="onSearchInput" 
              @keyup.enter="fetchWeather(cityInput)" 
              placeholder="Search city (e.g. Paris)..." 
            />
            <button class="arrow-btn" @click="fetchWeather(cityInput)">→</button>
            
            <ul v-if="showSuggestions && searchResults.length > 0" class="suggestions-list">
              <li v-for="res in searchResults" :key="res.name" @click="fetchWeather(res.name)">
                {{ res.name }} <span class="sub">{{ res.country }}</span>
              </li>
            </ul>
          </div>
        </div>

        <div v-if="homeCity" class="section-block">
          <p class="section-label">MY SANCTUARY</p>
          <div class="home-card">
            <div class="info" @click="fetchWeather(homeCity)">
              <h3>{{ homeCity }}</h3>
              <p class="sub-text">You are receiving daily briefings for this city.</p>
            </div>
            <div class="controls">
              <label class="switch">
                <input type="checkbox" checked @change="(e) => toggleEmailSub(homeCity, (e.target as HTMLInputElement).checked)">
                <span class="slider round"></span>
              </label>
            </div>
          </div>
        </div>
        
        <div class="section-block">
          <p class="section-label">COLLECTION</p>
          
          <div v-if="otherFavorites.length > 0" class="fav-grid">
            <div v-for="fav in otherFavorites" :key="fav" class="fav-card">
              <div class="card-content" @click="fetchWeather(fav)">
                <h3>{{ fav }}</h3>
                <span class="view-link">View Report</span>
              </div>
              <button class="remove-btn" @click.stop="removeDirectly(fav)" title="Remove">✕</button>
            </div>
          </div>
          
          <div v-else-if="!homeCity" class="empty-state">
            <p>Your collection is empty.</p>
          </div>
        </div>

      </div>

      <div v-if="viewState === 'CITY'" class="city-view fade-in">
        
        <div v-if="loading" class="loader"><div class="spinner"></div></div>

        <div v-else-if="weather" class="floating-card">
          <div class="detail-grid">
            
            <section class="left-panel" :class="weatherClass">
              <div class="weather-header">
                <div class="title-group">
                  <h1 class="city-name">{{ weather.city }}</h1>
                  <p class="country-name">{{ weather.country }}</p>
                </div>
                
                <div class="actions">
                  <button @click="toggleFav" class="icon-btn" :class="{ loved: isFav() }" title="Save to Collection">
                    {{ isFav() ? '♥' : '♡' }}
                  </button>
                  
                  <button 
                    @click="setAsHome" 
                    class="home-pill" 
                    :class="{active: isHome}"
                    :disabled="isHome"
                    :title="isHome ? 'This is your Home City' : 'Set as Home City'"
                  >
                    {{ isHome ? 'Home' : 'Set Home' }}
                  </button>
                </div>
              </div>

              <div class="temp-block">
                <span class="val">{{ Math.round(weather.currentTemp) }}</span>
                <span class="unit">°</span>
              </div>
              <div class="condition">{{ weather.currentCondition }}</div>

              <div class="metrics">
                <div class="m-item"><label>HUMIDITY</label><span>{{ weather.humidity }}%</span></div>
                <div class="m-item"><label>WIND</label><span>{{ Math.round(weather.windSpeed) }} km/h</span></div>
                <div class="m-item"><label>AQI</label><span>{{ weather.aqi }}</span></div>
              </div>
            </section>

            <section class="right-panel">
              <div class="panel-inner">
                <div class="brief-header">DAILY BRIEFING</div>
                
                <div v-if="aiLoading" class="skeleton">
                  <div class="line l1"></div><div class="line l2"></div><div class="line l3"></div>
                </div>
                
                <div v-else>
                  <p class="ai-summary">"{{ aiData?.summary }}"</p>
                  
                  <div class="advisories">
                    <div class="advice-block">
                      <span class="cat-label">OUTFIT</span>
                      <p class="adv-text">{{ aiData?.outfit }}</p>
                    </div>

                    <div class="advice-block">
                      <span class="cat-label">ADVISORY</span>
                      <p class="adv-text">{{ aiData?.safety }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </section>
          </div>
        </div>
      </div>

    </main>
  </div>
</template>

<style scoped>
/* RESET & FONTS */
.luxury-app { min-height: 100vh; display: flex; flex-direction: column; background: linear-gradient(180deg, #fdfbf7 0%, #ffffff 100%); color: #1a1a1a; font-family: 'Lato', sans-serif; overflow-x: hidden; }
h1, h2, h3, .city-name, .temp-block { font-family: 'Playfair Display', serif; }

/* NAVBAR */
.navbar { display: flex; justify-content: space-between; align-items: center; padding: 20px 40px; background: white; z-index: 50; height: 80px; position: sticky; top: 0; box-shadow: 0 2px 20px rgba(0,0,0,0.02); }
.logo { font-size: 1.5rem; font-weight: 700; cursor: pointer; letter-spacing: -0.5px; }
.logo .dot { color: #d4a373; }

/* NAV SEARCH (Centered) */
.nav-search-container { position: absolute; left: 50%; transform: translateX(-50%); width: 400px; }
.nav-search-container input { width: 100%; padding: 10px 20px; border: 1px solid #eee; border-radius: 20px; outline: none; background: #f9f9f9; font-family: 'Lato'; transition: all 0.2s; }
.nav-search-container input:focus { background: white; border-color: #ccc; box-shadow: 0 4px 12px rgba(0,0,0,0.05); }
.nav-suggestions { position: absolute; top: 110%; width: 100%; background: white; list-style: none; padding: 0; box-shadow: 0 10px 20px rgba(0,0,0,0.08); border-radius: 8px; overflow: hidden; z-index: 60; }
.nav-suggestions li { padding: 10px 15px; cursor: pointer; border-bottom: 1px solid #f5f5f5; font-size: 0.9rem; }
.nav-suggestions li:hover { background: #f9f9f9; }

.initial { width: 40px; height: 40px; background: #1a1a1a; color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-family: 'Playfair Display'; cursor: pointer; }
.dropdown { position: absolute; right: 0; top: 60px; background: white; border: 1px solid #eee; box-shadow: 0 10px 30px rgba(0,0,0,0.08); width: 200px; padding: 20px; z-index: 100; border-radius: 12px; }
.u-name { font-weight: bold; display: block; }
.u-email { font-size: 0.8rem; color: #888; display: block; margin-bottom: 10px; }
.divider { height: 1px; background: #eee; margin-bottom: 10px; }
.dropdown button { background: none; border: none; color: #d32f2f; cursor: pointer; width: 100%; text-align: left; padding-top: 10px; }
.profile-trigger { position: relative; }

/* HOME VIEW */
.home-view { flex: 1; display: flex; flex-direction: column; align-items: center; padding: 60px 20px; max-width: 900px; margin: 0 auto; width: 100%; }
.hero h1 { font-size: 3.8rem; color: #1a1a1a; margin-bottom: 50px; text-align: center; font-weight: 500; letter-spacing: -1px; }

/* CLOUD SEARCH (Home) */
.cloud-search { 
  position: relative; width: 100%; max-width: 650px; margin-bottom: 80px; 
  background: white; border-radius: 50px; 
  box-shadow: 0 20px 50px rgba(0,0,0,0.06); 
  display: flex; align-items: center; padding: 10px 10px 10px 30px;
  transition: transform 0.3s ease, box-shadow 0.3s ease;
}
.cloud-search:focus-within { transform: translateY(-2px); box-shadow: 0 25px 60px rgba(0,0,0,0.08); }
.cloud-search input { flex: 1; border: none; font-size: 1.2rem; outline: none; font-family: 'Lato'; background: transparent; color: #333; }
.arrow-btn { background: #1a1a1a; color: white; border: none; width: 50px; height: 50px; border-radius: 50%; font-size: 1.5rem; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: background 0.2s; }
.arrow-btn:hover { background: #333; }
.suggestions-list { position: absolute; top: 110%; left: 20px; right: 20px; background: white; list-style: none; padding: 10px 0; margin-top: 5px; box-shadow: 0 15px 40px rgba(0,0,0,0.08); border-radius: 20px; z-index: 50; text-align: left; }
.suggestions-list li { padding: 15px 25px; cursor: pointer; transition: background 0.1s; }
.suggestions-list li:hover { background: #f7f7f7; }
.sub { color: #999; font-size: 0.85rem; margin-left: 5px; font-style: italic; }

/* SECTIONS */
.section-block { width: 100%; margin-bottom: 60px; }
.section-label { font-size: 0.75rem; letter-spacing: 2px; color: #aaa; margin-bottom: 25px; font-weight: 700; text-transform: uppercase; }

/* Home Card */
.home-card { background: #fffcf5; border: 1px solid #eaddcf; padding: 30px 40px; border-radius: 20px; display: flex; justify-content: space-between; align-items: center; cursor: pointer; transition: all 0.2s; }
.home-card:hover { transform: translateY(-3px); box-shadow: 0 10px 30px rgba(0,0,0,0.03); }
.home-card h3 { margin: 0; font-size: 2rem; color: #1a1a1a; }
.sub-text { color: #888; font-size: 0.9rem; margin-top: 5px; }

/* Fav Grid */
.fav-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 20px; }
.fav-card { background: white; border: 1px solid #f0f0f0; padding: 25px; border-radius: 16px; position: relative; cursor: pointer; transition: all 0.3s; display: flex; justify-content: space-between; align-items: center; box-shadow: 0 5px 15px rgba(0,0,0,0.02); }
.fav-card:hover { transform: translateY(-5px); box-shadow: 0 15px 30px rgba(0,0,0,0.05); border-color: #1a1a1a; }
.card-content h3 { margin: 0 0 5px 0; font-size: 1.4rem; font-family: 'Playfair Display'; }
.view-link { font-size: 0.75rem; color: #bbb; text-transform: uppercase; letter-spacing: 1px; }
.remove-btn { background: none; border: none; color: #eee; font-size: 1.2rem; cursor: pointer; padding: 8px; transition: color 0.2s; border-radius: 50%; }
.remove-btn:hover { color: #ef4444; background: #fff5f5; }

/* CITY VIEW */
.city-view { flex: 1; display: flex; align-items: center; justify-content: center; padding: 40px; background: #f8f9fa; }
.floating-card { width: 100%; max-width: 1100px; background: white; border-radius: 50px; overflow: hidden; box-shadow: 0 30px 80px rgba(0,0,0,0.08); min-height: 650px; display: flex; }
.detail-grid { display: grid; grid-template-columns: 1.3fr 1fr; width: 100%; }

.left-panel { padding: 60px; display: flex; flex-direction: column; justify-content: center; color: #1a1a1a; transition: background 0.5s; position: relative; }

/* Header Layout: Name wraps, Actions stay put */
.weather-header { 
  display: flex; 
  justify-content: space-between; 
  align-items: flex-start; /* Important: Align to top */
  margin-bottom: 40px; 
  gap: 20px; 
}
.title-group { 
  flex: 1; 
  min-width: 0; /* Allows text wrap */
  padding-right: 10px; /* Space between name and buttons */
}
.city-name { 
  font-size: 3.5rem; 
  margin: 0; 
  line-height: 1.1; 
  word-wrap: break-word; /* Handle long names like Thiruvananthapuram */
}
.country-name { font-size: 1.2rem; color: inherit; opacity: 0.8; margin-top: 5px; font-family: 'Playfair Display'; font-style: italic; }

.actions { 
  display: flex; 
  align-items: center; 
  gap: 15px; 
  flex-shrink: 0; /* Prevents button squish */
}

/* Icon Button (Heart) */
.icon-btn { 
  font-size: 1.5rem; background: none; border: none; cursor: pointer; 
  opacity: 0.4; transition: all 0.2s; 
}
.icon-btn:hover { opacity: 0.8; transform: scale(1.1); }
.icon-btn.loved { opacity: 1; color: #e11d48; }

/* Set Home Pill (Redesigned) */
.home-pill { 
  padding: 8px 16px; 
  border: 1px solid #e5e5e5; 
  border-radius: 20px; 
  background: white; 
  font-size: 0.75rem; 
  font-weight: 700; 
  text-transform: uppercase; 
  cursor: pointer; 
  transition: all 0.2s; 
  white-space: nowrap;
}
.home-pill:hover { border-color: #1a1a1a; }
.home-pill.active { 
  background: #f0fdf4; 
  color: #15803d; 
  border-color: #bbf7d0; 
  cursor: default; 
}

.temp-block { font-size: 8rem; line-height: 1; margin: 20px 0; }
.unit { font-size: 3rem; vertical-align: top; }
.condition { font-size: 1.8rem; opacity: 0.7; margin-bottom: 60px; font-family: 'Playfair Display'; font-style: italic; }

.metrics { display: flex; gap: 50px; border-top: 1px solid rgba(0,0,0,0.1); padding-top: 40px; }
.m-item { display: flex; flex-direction: column; }
.m-item label { font-size: 0.7rem; letter-spacing: 1px; opacity: 0.6; margin-bottom: 5px; font-weight: 700; }
.m-item span { font-weight: bold; font-size: 1.1rem; }

.right-panel { background: #ffffff; padding: 60px; display: flex; flex-direction: column; justify-content: center; }
.panel-inner { max-width: 450px; margin: 0 auto; }
.brief-header { font-size: 0.8rem; letter-spacing: 2px; color: #d4a373; border-bottom: 2px solid #d4a373; display: inline-block; padding-bottom: 5px; margin-bottom: 40px; }
.ai-summary { font-family: 'Playfair Display'; font-size: 1.5rem; line-height: 1.6; color: #333; margin-bottom: 50px; }
.advisories { display: flex; flex-direction: column; gap: 30px; }
.advice-block { display: flex; flex-direction: column; gap: 8px; border-left: 2px solid #f0f0f0; padding-left: 20px; }
.cat-label { font-size: 0.7rem; letter-spacing: 1.5px; color: #aaa; text-transform: uppercase; font-weight: 700; }
.adv-text { font-size: 1rem; color: #1a1a1a; font-weight: 500; line-height: 1.5; margin: 0; font-family: 'Lato', sans-serif; }

/* TOGGLE SWITCH */
.switch { position: relative; display: inline-block; width: 50px; height: 28px; }
.switch input { opacity: 0; width: 0; height: 0; }
.slider { position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: #ccc; transition: .4s; border-radius: 34px; }
.slider:before { position: absolute; content: ""; height: 22px; width: 22px; left: 3px; bottom: 3px; background-color: white; transition: .4s; border-radius: 50%; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
input:checked + .slider { background-color: #10b981; }
input:checked + .slider:before { transform: translateX(22px); }

/* Backgrounds */
.bg-default { background: #fdfdfd; }
.bg-sunny { background: linear-gradient(135deg, #fffbf0 0%, #fff5e0 100%); }
.bg-cloudy { background: linear-gradient(135deg, #f3f4f6 0%, #e5e7eb 100%); }
.bg-rainy { background: linear-gradient(135deg, #e0f2fe 0%, #bae6fd 100%); }
.bg-misty { background: linear-gradient(135deg, #f5f5f4 0%, #d6d3d1 100%); }
.bg-stormy { background: linear-gradient(135deg, #cbd5e1 0%, #94a3b8 100%); }

.fade-in { animation: fadeIn 0.6s ease-out; }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
.loader { text-align: center; margin-top: 100px; color: #aaa; width: 100%; }
.spinner { width: 40px; height: 40px; border: 2px solid #eee; border-top-color: #1a1a1a; border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto 20px; }
@keyframes spin { to { transform: rotate(360deg); } }
.skeleton .line { height: 12px; background: #e0e0e0; margin-bottom: 15px; animation: pulse 1.5s infinite; }
.skeleton .l2 { width: 80%; } .skeleton .l3 { width: 60%; }
@keyframes pulse { 0% { opacity: 0.5; } 50% { opacity: 1; } 100% { opacity: 0.5; } }
</style>