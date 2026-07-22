import { defineStore } from 'pinia'
import { ref } from 'vue'
import axios from 'axios'

// Decode JWT payload (base64url)
function decodeJwt(token: string): any {
  try {
    const base64Url = token.split('.')[1]
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    return JSON.parse(jsonPayload)
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('jwt_token'))
  const user = ref<{ username: string; userId: string } | null>(null)

  // Initialize user from stored token
  if (token.value) {
    const payload = decodeJwt(token.value)
    if (payload) {
      user.value = {
        username: payload['username'] || payload['name'] || payload.sub || '',
        userId: payload['sub'] || ''
      }
    }
    axios.defaults.headers.common['Authorization'] = `Bearer ${token.value}`
  }

  const isAuthenticated = () => !!token.value

  const setToken = (newToken: string) => {
    token.value = newToken
    localStorage.setItem('jwt_token', newToken)
    axios.defaults.headers.common['Authorization'] = `Bearer ${newToken}`

    // Decode and set user info
    const payload = decodeJwt(newToken)
    if (payload) {
      user.value = {
        username: payload['username'] || payload['name'] || payload.sub || '',
        userId: payload['sub'] || ''
      }
    }
  }

  const logout = () => {
    token.value = null
    user.value = null
    localStorage.removeItem('jwt_token')
    delete axios.defaults.headers.common['Authorization']
  }

  return { token, user, isAuthenticated, setToken, logout }
})
