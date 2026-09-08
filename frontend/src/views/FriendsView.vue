<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const activeTab = ref('all') // all, requests

const friends = ref<any[]>([])
const loading = ref(false)

const fetchFriends = async () => {
  if (!authStore.isAuthenticated()) return
  loading.value = true
  try {
    const res = await api.get(`/profile/${authStore.user?.userId}/friends`)
    friends.value = res.data
  } catch (err) {
    console.error("Failed to load friends", err)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchFriends()
})
</script>

<template>
  <div class="friends-page glass-panel">
    <div class="tabs">
      <button :class="{ active: activeTab === 'all' }" @click="activeTab = 'all'">Все друзья</button>
      <button :class="{ active: activeTab === 'requests' }" @click="activeTab = 'requests'">Заявки</button>
    </div>

    <div class="friends-list" v-if="!loading">
      <div v-for="friend in friends" :key="friend.id" class="friend-card">
        <!-- Display logic based on friend.status -->
        <router-link :to="`/profile/${friend.username}`" class="friend-info">
          <div class="avatar-mini">{{ friend.username.charAt(0).toUpperCase() }}</div>
          <span>{{ friend.username }}</span>
        </router-link>
        <span class="status-badge" v-if="friend.status === 0">
           {{ friend.isRequester ? 'Исходящая заявка' : 'Входящая заявка' }}
        </span>
      </div>
      <div v-if="friends.length === 0" class="empty-state">
        У вас пока нет друзей или заявок
      </div>
    </div>
    <div v-else class="loading">Загрузка...</div>
  </div>
</template>

<style scoped>
.friends-page {
  padding: 24px;
}
.tabs {
  display: flex;
  gap: 16px;
  border-bottom: 1px solid rgba(255,255,255,0.1);
  margin-bottom: 24px;
}
.tabs button {
  background: none;
  border: none;
  color: var(--text-muted);
  padding: 8px 16px;
  cursor: pointer;
  font-weight: 600;
  border-bottom: 2px solid transparent;
}
.tabs button.active {
  color: var(--text-primary);
  border-bottom-color: var(--accent-primary);
}
.friends-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.friend-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px;
  background: rgba(255,255,255,0.05);
  border-radius: 8px;
}
.friend-info {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: white;
  font-weight: 600;
}
.avatar-mini {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent-primary), var(--accent-secondary));
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
}
.status-badge {
  font-size: 0.8rem;
  background: rgba(255,255,255,0.1);
  padding: 4px 8px;
  border-radius: 4px;
}
.empty-state {
  text-align: center;
  color: var(--text-muted);
  padding: 40px;
}
</style>
