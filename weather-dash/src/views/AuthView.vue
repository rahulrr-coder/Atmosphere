<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

const router = useRouter()

// State
const isLogin = ref(true)
const isLoading = ref(false)
const errorMessage = ref('')

// Form Fields
const username = ref('')
const email = ref('')
const password = ref('')

const toggleMode = () => {
  isLogin.value = !isLogin.value
  errorMessage.value = ''
  if (isLogin.value) email.value = ''
}

const handleSubmit = async () => {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const payload = {
      username: username.value,
      email: email.value,
      password: password.value,
      isSubscribed: false,
      city: ""
    }

    const baseUrl = 'http://localhost:5160/api/auth'
    let emailToSave = email.value; // Default to what was typed (Registration)

    if (isLogin.value) {
      // LOGIN: Backend returns the full user object, including email
      const res = await axios.post(`${baseUrl}/login`, payload)
      
      // 🟢 CRITICAL FIX: Grab email from backend response
      if (res.data.email) {
        emailToSave = res.data.email;
      }
    } else {
      // REGISTER
      await axios.post(`${baseUrl}/register`, payload)
    }

    // Save to LocalStorage
    localStorage.setItem('userName', username.value)
    if (emailToSave) {
      localStorage.setItem('userEmail', emailToSave) // Now the dashboard will see the real email
    }

    router.push('/dashboard')

  } catch (err: any) {
    console.error(err)
    errorMessage.value = err.response?.data || "Authentication failed. Please check details."
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="auth-wrapper">
    <component is="style">
      @import url('https://fonts.googleapis.com/css2?family=Lato:wght@300;400;700&family=Playfair+Display:ital,wght@0,400;0,600;1,400&display=swap');
    </component>

    <div class="auth-card fade-in">
      <div class="header">
        <h1>{{ isLogin ? 'Welcome Back' : 'Join Atmosphere' }}</h1>
        <p>{{ isLogin ? 'Sign in to your dashboard' : 'Experience the forecast reimagined' }}</p>
      </div>

      <form @submit.prevent="handleSubmit">
        <div class="input-group">
          <label>USERNAME</label>
          <input v-model="username" type="text" required />
        </div>

        <div v-if="!isLogin" class="input-group slide-in">
          <label>EMAIL</label>
          <input v-model="email" type="email" required />
        </div>

        <div class="input-group">
          <label>PASSWORD</label>
          <input v-model="password" type="password" required minlength="6" />
        </div>

        <button type="submit" class="submit-btn" :disabled="isLoading">
          {{ isLoading ? 'Processing...' : (isLogin ? 'Enter' : 'Create Account') }}
        </button>

        <p v-if="errorMessage" class="error-text">{{ errorMessage }}</p>
      </form>

      <div class="footer">
        <span @click="toggleMode" class="toggle-link">
          {{ isLogin ? "Don't have an account? Sign Up" : "Already have an account? Log In" }}
        </span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.auth-wrapper {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #f8f8f8;
  font-family: 'Lato', sans-serif;
  color: #333;
}

.auth-card {
  background: white;
  width: 100%;
  max-width: 450px;
  padding: 60px 50px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.04);
  border: 1px solid #eaeaea;
}

.header { text-align: center; margin-bottom: 50px; }
.header h1 { font-family: 'Playfair Display', serif; font-size: 2.2rem; margin: 0; color: #1e1e1e; }
.header p { color: #888; margin-top: 10px; font-weight: 300; }

.input-group { margin-bottom: 25px; }
.input-group label { display: block; font-size: 0.7rem; letter-spacing: 2px; font-weight: 700; color: #aaa; margin-bottom: 10px; }
.input-group input { 
  width: 100%; padding: 15px 0; border: none; border-bottom: 1px solid #ddd; 
  outline: none; font-size: 1.1rem; font-family: 'Lato', sans-serif; color: #333; transition: border 0.3s;
}
.input-group input:focus { border-bottom-color: #333; }

.submit-btn {
  width: 100%; padding: 18px; background: #1e1e1e; color: white; border: none;
  font-size: 0.9rem; letter-spacing: 1px; text-transform: uppercase; cursor: pointer;
  margin-top: 20px; transition: background 0.3s;
}
.submit-btn:hover { background: #000; }
.submit-btn:disabled { opacity: 0.7; }

.footer { margin-top: 30px; text-align: center; font-size: 0.9rem; }
.toggle-link { cursor: pointer; color: #666; transition: color 0.2s; border-bottom: 1px solid transparent; }
.toggle-link:hover { color: #000; border-bottom-color: #000; }

.error-text { color: #d32f2f; text-align: center; margin-top: 20px; font-size: 0.9rem; }

/* Animations */
.fade-in { animation: fadeIn 0.8s ease-out; }
@keyframes fadeIn { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }
</style>