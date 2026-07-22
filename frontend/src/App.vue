<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useAuthStore } from './stores/auth'
import { useRouter } from 'vue-router'
import axios from 'axios'
import { signalRService } from './services/signalr'

const authStore = useAuthStore()
const router = useRouter()

const showNotifications = ref(false)
const notifications = ref<any[]>([])
const unreadCount = computed(() => notifications.value.filter(n => !n.isRead).length)

const handleLogout = () => {
  authStore.logout()
  router.push('/auth/login')
}

const fetchNotifications = async () => {
  if (!authStore.isAuthenticated()) return
  try {
    const res = await axios.get('http://localhost:8081/api/social/notifications', {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    notifications.value = res.data
  } catch (err) {
    console.error("Failed to load notifications")
  }
}

const handleReceiveNotification = (notification: any) => {
  notifications.value.unshift(notification)
}

const readNotification = async (notif: any) => {
  try {
    if (!notif.isRead) {
      await axios.put(`http://localhost:8081/api/social/notifications/${notif.id}/read`, {}, {
        headers: { 'Authorization': `Bearer ${authStore.token}` }
      })
      notif.isRead = true
    }
    showNotifications.value = false
    router.push(notif.link)
  } catch (err) {
    console.error(err)
  }
}

onMounted(() => {
  if (authStore.isAuthenticated()) {
    fetchNotifications()
    signalRService.startConnection()
  }
  signalRService.on('ReceiveNotification', handleReceiveNotification)
})

watch(() => authStore.token, (newToken) => {
  if (newToken) {
    fetchNotifications()
    signalRService.startConnection()
  } else {
    signalRService.stopConnection()
  }
})
</script>

<template>
  <div class="app-container">
    <nav class="navbar glass-panel">
      <div class="logo">
        <span class="prohvat">PROHVAT</span><span class="dot">.</span>APP
      </div>
      <div class="nav-links">
        <router-link to="/" class="nav-link">🗺️ Карта</router-link>
        <router-link to="/feed" class="nav-link">📜 Лента</router-link>
        <router-link to="/tuning" class="nav-link">🔧 Тюнинг</router-link>
        <template v-if="!authStore.isAuthenticated()">
          <router-link to="/auth/login" class="btn-primary" style="padding: 8px 16px; text-decoration: none;">Войти</router-link>
        </template>
        <template v-else>
          <router-link to="/garage" class="nav-link">🏍️ Гараж</router-link>
          
          <!-- Notifications Bell -->
          <div class="notif-wrapper" style="position:relative;">
            <button @click="showNotifications = !showNotifications" class="icon-btn">
              🔔
              <span v-if="unreadCount > 0" class="badge">{{ unreadCount }}</span>
            </button>
            <div v-if="showNotifications" class="notifications-dropdown glass-panel">
              <h4 style="margin: 0 0 10px 0; border-bottom: 1px solid rgba(255,255,255,0.1); padding-bottom: 5px;">Уведомления</h4>
              <div v-if="notifications.length === 0" style="color:#aaa; font-size:0.9em;">Нет новых уведомлений</div>
              <div v-for="n in notifications" :key="n.id" class="notif-item" :class="{ unread: !n.isRead }" @click="readNotification(n)">
                {{ n.message }}
              </div>
            </div>
          </div>

          <!-- Profile Link -->
          <router-link 
            v-if="authStore.user?.username" 
            :to="`/profile/${authStore.user.username}`" 
            class="profile-nav-link"
          >
            <div class="avatar-mini">{{ authStore.user.username.charAt(0).toUpperCase() }}</div>
            <span>@{{ authStore.user.username }}</span>
          </router-link>
          
          <button @click="handleLogout" class="btn-secondary logout-btn">Выйти</button>
        </template>
      </div>
    </nav>

    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<style scoped>
.app-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
}

.navbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 32px;
  position: fixed;
  top: 16px;
  left: 16px;
  right: 16px;
  z-index: 1000;
  border-radius: 12px;
}

.logo {
  font-family: var(--font-heading);
  font-weight: 800;
  font-size: 1.5rem;
  letter-spacing: -1px;
}

.prohvat {
  color: var(--text-primary);
}

.dot {
  color: var(--accent-primary);
}

.nav-links {
  display: flex;
  gap: 24px;
  align-items: center;
}

.nav-links a {
  color: var(--text-primary);
  text-decoration: none;
  font-weight: 600;
  transition: color 0.3s ease;
}

.nav-links a:hover {
  color: var(--accent-primary);
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 6px;
  color: rgba(255,255,255,0.8) !important;
  font-size: 0.9rem;
  padding: 6px 10px;
  border-radius: 8px;
  transition: all 0.2s ease;
}
.nav-link:hover {
  color: white !important;
  background: rgba(255,255,255,0.08);
}
.nav-link.router-link-active {
  color: var(--accent-primary) !important;
}

.icon-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 1.2rem;
  color: white;
  padding: 6px 10px;
  border-radius: 8px;
  position: relative;
  transition: background 0.2s;
}
.icon-btn:hover { background: rgba(255,255,255,0.1); }

.profile-nav-link {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 12px;
  background: rgba(255,255,255,0.08);
  border-radius: 20px;
  border: 1px solid rgba(255,255,255,0.15);
  text-decoration: none !important;
  color: white !important;
  font-size: 0.875rem;
  font-weight: 600;
  transition: all 0.2s;
}
.profile-nav-link:hover {
  background: rgba(255,255,255,0.15) !important;
  border-color: var(--accent-primary);
  color: white !important;
}

.avatar-mini {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent-primary), var(--accent-secondary));
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: 700;
  color: white;
  flex-shrink: 0;
}

.logout-btn {
  background: transparent;
  border: 1px solid rgba(255,255,255,0.2);
  color: rgba(255,255,255,0.7);
  padding: 6px 14px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.85rem;
  transition: all 0.2s;
}
.logout-btn:hover {
  background: rgba(255,77,0,0.2);
  border-color: var(--accent-primary);
  color: white;
}

.notif-wrapper { position: relative; }

.main-content {
  flex: 1;
  position: relative;
}

.badge {
  position: absolute;
  top: -5px;
  right: -5px;
  background: var(--primary-color);
  color: white;
  border-radius: 50%;
  padding: 2px 6px;
  font-size: 0.75rem;
  font-weight: bold;
}

.notifications-dropdown {
  position: absolute;
  top: 60px;
  right: 16px;
  width: 300px;
  max-height: 400px;
  overflow-y: auto;
  z-index: 2000;
  padding: 16px;
}

.notif-item {
  padding: 10px;
  border-radius: 8px;
  margin-bottom: 8px;
  cursor: pointer;
  background: rgba(0,0,0,0.3);
  font-size: 0.9em;
  color: white;
  transition: background 0.2s;
}

.notif-item:hover {
  background: rgba(255,255,255,0.1);
}

.notif-item.unread {
  border-left: 3px solid var(--primary-color);
  background: rgba(255,77,0, 0.1);
}
</style>
