import { createApp } from 'vue'
import App from './App.vue'
import router from './router' // Import the router we created


const app = createApp(App)

app.use(router)

app.mount('#app')