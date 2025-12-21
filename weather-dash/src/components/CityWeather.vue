<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  weather: any;
  aiData: any;
  loading: boolean;
  aiLoading: boolean;
  isHome: boolean;
  isFav: boolean;
}>();

const emit = defineEmits(['toggleFav', 'setHome']);

const weatherClass = computed(() => {
  if (!props.weather) return 'bg-default';
  const cond = props.weather.currentCondition.toLowerCase();
  if (cond.includes('clear') || cond.includes('sun')) return 'bg-sunny';
  if (cond.includes('cloud')) return 'bg-cloudy';
  if (cond.includes('rain') || cond.includes('drizzle')) return 'bg-rainy';
  if (cond.includes('mist') || cond.includes('haze')) return 'bg-misty';
  return 'bg-default';
});
</script>

<template>
  <div class="city-view fade-in">
    <div v-if="props.loading" class="loader"><div class="spinner"></div></div>

    <div v-else-if="props.weather" class="floating-card">
      <div class="detail-grid">
        
        <section class="left-panel" :class="weatherClass">
          <div class="weather-main-content">
            <div class="weather-header">
              <div class="title-group">
                <h1 class="city-name">{{ props.weather.city }}</h1>
                <p class="country-name">{{ props.weather.country }}</p>
              </div>
              <div class="actions">
                <button @click="emit('toggleFav')" class="icon-btn" :class="{ loved: props.isFav }">
                  {{ props.isFav ? '♥' : '♡' }}
                </button>
                <button @click="emit('setHome')" class="home-pill" :class="{active: props.isHome}" :disabled="props.isHome">
                  {{ props.isHome ? 'Home' : 'Set Home' }}
                </button>
              </div>
            </div>

            <div class="temp-block">
              <span class="val">{{ Math.round(props.weather.currentTemp) }}</span>
              <span class="unit">°</span>
            </div>
            
            <div class="condition-row">
              <span class="condition">{{ props.weather.currentCondition }}</span>
              <span class="high-low">H: {{ Math.round(props.weather.maxTemp) }}°  L: {{ Math.round(props.weather.minTemp) }}°</span>
            </div>

            <div class="metrics-grid">
              <div class="m-item"><label>HUMIDITY</label><span>{{ props.weather.humidity }}%</span></div>
              <div class="m-item"><label>WIND</label><span>{{ Math.round(props.weather.windSpeed) }} km/h</span></div>
              <div class="m-item"><label>AQI</label><span>{{ props.weather.aqi }}</span></div>
              
              <div class="m-item"><label>UV INDEX</label><span>{{ props.weather.uvIndex }}</span></div>
              <div class="m-item"><label>VISIBILITY</label><span>{{ props.weather.visibility }} km</span></div>
              <div class="m-item"><label>SUNRISE</label><span class="small-val">{{ props.weather.sunrise }}</span></div>
              <div class="m-item"><label>DAYLIGHT</label><span class="small-val">{{ props.weather.dayLength }}</span></div>
            </div>
          </div>

          <div class="day-cycle" v-if="props.weather.dayParts && props.weather.dayParts.length >= 3">
            <div class="cycle-item">
              <span class="c-label">Morning</span><span class="c-val">{{ Math.round(props.weather.dayParts[0].temp) }}°</span>
            </div>
            <div class="cycle-divider"></div>
            <div class="cycle-item">
              <span class="c-label">Afternoon</span><span class="c-val">{{ Math.round(props.weather.dayParts[1].temp) }}°</span>
            </div>
            <div class="cycle-divider"></div>
            <div class="cycle-item">
              <span class="c-label">Evening</span><span class="c-val">{{ Math.round(props.weather.dayParts[2].temp) }}°</span>
            </div>
          </div>
        </section>

        <section class="right-panel">
          <div class="panel-inner">
            <div class="brief-header">DAILY BRIEFING</div>
            <div v-if="props.aiLoading" class="skeleton"><div class="line"></div><div class="line"></div></div>
            <div v-else>
              <p class="ai-summary">"{{ props.aiData?.summary }}"</p>
              <div class="advisories">
                <div class="advice-block">
                  <span class="cat-label">OUTFIT</span><p class="adv-text">{{ props.aiData?.outfit }}</p>
                </div>
                <div class="advice-block">
                  <span class="cat-label">ADVISORY</span><p class="adv-text">{{ props.aiData?.safety }}</p>
                </div>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>