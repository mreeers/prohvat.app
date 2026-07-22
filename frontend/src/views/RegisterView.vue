<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import axios from 'axios'

const name = ref('')
const email = ref('')
const password = ref('')
const errorMsg = ref('')
const router = useRouter()
const authStore = useAuthStore()

const register = async () => {
  try {
    errorMsg.value = ''
    const response = await axios.post('http://localhost:8081/api/auth/register', {
      name: name.value,
      email: email.value,
      password: password.value
    })
    
    authStore.setToken(response.data.token)
    router.push('/')
  } catch (err: any) {
    errorMsg.value = 'Error registering user. Email might already exist.'
  }
}
</script>

<template>
  <div class="auth-wrapper">
    <div class="auth-card glass-panel">
      <h2>Join the Ride</h2>
      <p class="subtitle">Create an account to see nearby rides</p>
      
      <form @submit.prevent="register" class="auth-form">
        <div class="input-group">
          <label>Name / Nickname</label>
          <input type="text" v-model="name" class="input-glass" required placeholder="EnduroRider99" />
        </div>

        <div class="input-group">
          <label>Email</label>
          <input type="email" v-model="email" class="input-glass" required placeholder="rider@prohvat.app" />
        </div>
        
        <div class="input-group">
          <label>Password</label>
          <input type="password" v-model="password" class="input-glass" required placeholder="••••••••" />
        </div>
        
        <div v-if="errorMsg" class="error">{{ errorMsg }}</div>
        
        <button type="submit" class="btn-primary full-width">Sign Up</button>
      </form>
      
      <p class="switch-mode">
        Already have an account? <router-link to="/auth/login">Sign In</router-link>
      </p>
    </div>
  </div>
</template>

<style scoped>
.auth-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: radial-gradient(circle at 50% 50%, rgba(255, 77, 0, 0.1) 0%, transparent 50%);
}

.auth-card {
  width: 100%;
  max-width: 400px;
  padding: 40px;
  text-align: center;
}

h2 {
  font-size: 2rem;
  margin-bottom: 8px;
}

.subtitle {
  color: var(--text-muted);
  margin-bottom: 32px;
}

.auth-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.input-group {
  text-align: left;
}

.input-group label {
  display: block;
  margin-bottom: 8px;
  font-weight: 500;
  font-size: 0.9rem;
}

.full-width {
  width: 100%;
  margin-top: 10px;
}

.error {
  color: #ff4d4d;
  font-size: 0.85rem;
}

.switch-mode {
  margin-top: 24px;
  font-size: 0.9rem;
  color: var(--text-muted);
}

.switch-mode a {
  color: var(--accent-primary);
  text-decoration: none;
  font-weight: 600;
}
</style>
