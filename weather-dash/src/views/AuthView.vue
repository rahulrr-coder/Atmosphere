<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

const router = useRouter()

// State Types
const isLogin = ref<boolean>(true)
const isLoading = ref<boolean>(false)
const errorMessage = ref<string>('')

// Form Fields
const username = ref<string>('')
const email = ref<string>('')
const city = ref<string>('')

// Toggle mode
const toggleMode = () => {
  isLogin.value = !isLogin.value
  errorMessage.value = ''
}

const handleSubmit = async () => {
  isLoading.value = true
  errorMessage.value = ''

  try {
    // 1. Prepare Data
    const payload = {
      username: username.value,
      email: email.value,
      password: "DefaultPassword123!", 
      isSubscribed: true 
    }

    // 2. Call API (Only for Register in this MVP)
    if (!isLogin.value) {
      await axios.post('http://localhost:5160/api/auth/register', payload)
    }

    // 3. Save User Session
    localStorage.setItem('userName', username.value)
    localStorage.setItem('userCity', city.value || 'Coimbatore') 
    
    // 4. Redirect
    router.push('/dashboard')

  } catch (err: any) {
    console.error(err)
    // Handle specific backend error messages if available
    errorMessage.value = err.response?.data || "Unable to connect. Is the backend running?"
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="auth-container">
    <div class="auth-card">
      <div class="header">
        <h1>{{ isLogin ? 'Welcome Back' : 'Create Account' }}</h1>
        <p>{{ isLogin ? 'Enter your details to access your dashboard' : 'Join us for daily AI weather updates' }}</p>
      </div>

      <form @submit.prevent="handleSubmit">
        <div class="input-group">
          <label>Name</label>
          <input v-model="username" type="text" placeholder="e.g. Ninja" required />
        </div>

        <div v-if="!isLogin" class="fade-in">
          <div class="input-group">
            <label>Email</label>
            <input v-model="email" type="email" placeholder="user@example.com" required />
          </div>
          
          <div class="input-group">
            <label>Favorite City</label>
            <input v-model="city" type="text" placeholder="e.g. Coimbatore" required />
          </div>
        </div>

        <button type="submit" class="primary-btn" :disabled="isLoading">
          {{ isLoading ? 'Processing...' : (isLogin ? 'Sign In' : 'Get Started') }}
        </button>

        <p v-if="errorMessage" class="error-msg">{{ errorMessage }}</p>
      </form>

      <div class="footer">
        <p>
          {{ isLogin ? "Don't have an account?" : "Already have an account?" }}
          <span @click="toggleMode" class="link">
            {{ isLogin ? 'Sign Up' : 'Log In' }}
          </span>
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* (Same CSS as before - it works perfectly with TS) */
.auth-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  font-family: 'Inter', 'Segoe UI', sans-serif;
  padding: 20px;
}
.auth-card {
  background: white;
  width: 100%;
  max-width: 400px;
  padding: 40px;
  border-radius: 24px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.08);
}
.header { margin-bottom: 30px; text-align: center; }
.header h1 { margin: 0; font-size: 1.8rem; color: #2d3748; }
.header p { margin-top: 8px; color: #718096; font-size: 0.95rem; }
.input-group { margin-bottom: 20px; text-align: left; }
.input-group label { display: block; font-size: 0.85rem; font-weight: 600; color: #4a5568; margin-bottom: 8px; }
.input-group input {
  width: 100%;
  padding: 12px 16px;
  border: 2px solid #edf2f7;
  border-radius: 12px;
  font-size: 1rem;
  transition: all 0.2s;
  outline: none;
}
.input-group input:focus { border-color: #667eea; background: #fff; }
.primary-btn {
  width: 100%;
  padding: 14px;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 12px;
  font-weight: 600;
  font-size: 1rem;
  cursor: pointer;
  transition: background 0.2s;
  margin-top: 10px;
}
.primary-btn:hover { background: #5a67d8; }
.primary-btn:disabled { opacity: 0.7; cursor: not-allowed; }
.footer { margin-top: 25px; text-align: center; font-size: 0.9rem; color: #718096; }
.link { color: #667eea; font-weight: 600; cursor: pointer; margin-left: 5px; }
.link:hover { text-decoration: underline; }
.error-msg { color: #e53e3e; text-align: center; margin-top: 15px; font-size: 0.9rem; }
.fade-in { animation: fadeIn 0.3s ease-in; }
@keyframes fadeIn { from { opacity: 0; transform: translateY(-10px); } to { opacity: 1; transform: translateY(0); } }
</style>