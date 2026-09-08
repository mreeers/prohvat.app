<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useAuthStore } from './stores/auth'
import { useRouter, useRoute } from 'vue-router'
import api from './services/api'
import { signalRService } from './services/signalr'

const authStore = useAuthStore()
const router = useRouter()
const route = useRoute()

const isMapRoute = computed(() => {
  return ['home', 'map-users', 'map-spots'].includes(route.name as string)
})

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
    const res = await api.get('/social/notifications', {
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
      await api.put(`/social/notifications/${notif.id}/read`, {}, {
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
    <!-- Top Fixed Header -->
    <header class="top-header glass-panel">
      <div class="header-content">
        <div class="logo">
          <span class="prohvat">PROHVAT</span><span class="dot">.</span>APP
        </div>

        <div class="header-right">
          <template v-if="!authStore.isAuthenticated()">
            <router-link to="/auth/login" class="btn-primary" style="padding: 8px 16px; text-decoration: none;">Войти</router-link>
          </template>
          <template v-else>
            <!-- Notifications Bell -->
            <div class="notif-wrapper">
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

            <!-- Profile Link in Header -->
            <router-link 
              v-if="authStore.user?.username" 
              :to="`/profile/${authStore.user.username}`" 
              class="profile-nav-link"
            >
              <div class="avatar-mini">{{ authStore.user.username.charAt(0).toUpperCase() }}</div>
              <span>{{ authStore.user.username }}</span>
            </router-link>
            
            <button @click="handleLogout" class="btn-secondary logout-btn">Выйти</button>
          </template>
        </div>
      </div>
    </header>

    <!-- Main Page Layout -->
    <div :class="['page-layout', { 'full-width': isMapRoute }]">
      <!-- Left Sidebar (VK Style) -->
      <aside class="sidebar" v-if="authStore.isAuthenticated()">
        <nav class="sidebar-nav">
          <router-link :to="`/profile/${authStore.user?.username}`" class="sidebar-link">
            <span class="icon">👤</span> Моя страница
          </router-link>
          <router-link to="/feed" class="sidebar-link">
            <span class="icon">📰</span> Новости
          </router-link>
          <router-link to="/friends" class="sidebar-link">
            <span class="icon">👥</span> Мои друзья
          </router-link>
          <router-link to="/map/users" class="sidebar-link" :class="{ 'router-link-active': route.path === '/' }">
            <span class="icon">🗺️</span> Карта райдеров
          </router-link>
          <router-link to="/map/spots" class="sidebar-link">
            <span class="icon">📍</span> Споты
          </router-link>
          <router-link to="/garage" class="sidebar-link">
            <span class="icon">🏍️</span> Гараж
          </router-link>
          <router-link to="/videos" class="sidebar-link">
            <span class="icon">🎥</span> Видео
          </router-link>
          <div class="sidebar-divider"></div>
          <router-link to="/settings" class="sidebar-link">
            <span class="icon">⚙️</span> Настройки
          </router-link>
        </nav>
      </aside>

      <!-- Center Content Area -->
      <main class="content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<style scoped>
.app-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

/* Header */
.top-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 56px;
  z-index: 1000;
  border-radius: 0;
  border-top: none;
  border-left: none;
  border-right: none;
  border-bottom: var(--border-glass);
  background: rgba(15, 17, 21, 0.85); /* Slightly darker for header */
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
  height: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 16px;
}

.logo {
  font-family: var(--font-heading);
  font-weight: 800;
  font-size: 1.5rem;
  letter-spacing: -1px;
}

.prohvat { color: var(--text-primary); }
.dot { color: var(--accent-primary); }

.header-right {
  display: flex;
  gap: 16px;
  align-items: center;
}

/* Page Layout */
.page-layout {
  display: flex;
  max-width: 1200px;
  margin: 72px auto 0 auto; /* 56px header + 16px gap */
  padding: 0 16px;
  width: 100%;
  gap: 24px;
  transition: all 0.3s ease;
}

.page-layout.full-width {
  max-width: 100%;
  margin-top: 56px; /* No gap */
  padding: 0;
  gap: 0;
}

.page-layout.full-width .content {
  padding-bottom: 0;
}

/* Sidebar */
.sidebar {
  width: 220px;
  flex-shrink: 0;
}
.page-layout.full-width .sidebar {
  padding: 16px 0 0 16px;
}

.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 4px;
  position: sticky;
  top: 72px;
}

.sidebar-link {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  color: var(--text-primary);
  text-decoration: none;
  font-size: 0.95rem;
  border-radius: 6px;
  transition: all 0.2s ease;
}

.sidebar-link:hover {
  background: rgba(255, 255, 255, 0.05);
}

.sidebar-link.router-link-active {
  background: rgba(255, 255, 255, 0.1);
  font-weight: 600;
  border-left: 3px solid var(--accent-primary);
}

.sidebar-divider {
  height: 1px;
  background: rgba(255, 255, 255, 0.1);
  margin: 8px 0;
}

.icon {
  font-size: 1.1rem;
  width: 20px;
  text-align: center;
}

/* Content Area */
.content {
  flex: 1;
  min-width: 0; /* Prevent overflow */
  padding-bottom: 40px;
}

/* Shared Header UI Elements */
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

.badge {
  position: absolute;
  top: -2px;
  right: -2px;
  background: var(--accent-primary);
  color: white;
  border-radius: 50%;
  padding: 2px 6px;
  font-size: 0.7rem;
  font-weight: bold;
}

.notifications-dropdown {
  position: absolute;
  top: 45px;
  right: -10px;
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
  border-left: 3px solid var(--accent-primary);
  background: rgba(255,77,0, 0.1);
}
</style>
