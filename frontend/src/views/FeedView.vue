<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useAppStore } from '../stores/app'
import axios from 'axios'
import { useToast } from 'vue-toastification'

import { useAuthStore } from '../stores/auth'

const appStore = useAppStore()
const authStore = useAuthStore()
const toast = useToast()

const rides = ref<any[]>([])
const cities = ref<any[]>([])
const selectedCityId = ref<string | null>(null)
const loading = ref(false)

const fetchCities = async () => {
  try {
    const response = await axios.get('http://localhost:8081/api/location/cities')
    cities.value = response.data
  } catch (err) {
    console.error('Failed to fetch cities', err)
    toast.error('Ошибка загрузки городов')
  }
}

const fetchFeed = async () => {
  loading.value = true
  try {
    let url = `http://localhost:8081/api/rides/feed?season=${appStore.currentSeason === 'enduro' ? 0 : 1}`
    if (selectedCityId.value) {
      url += `&cityId=${selectedCityId.value}`
    }
    const response = await axios.get(url)
    rides.value = response.data
  } catch (err) {
    console.error('Failed to fetch feed', err)
    toast.error('Ошибка загрузки ленты')
  } finally {
    loading.value = false
  }
}

const joinRide = async (ride: any) => {
  if (!authStore.isAuthenticated()) {
    toast.warning("Войдите в аккаунт, чтобы присоединиться!")
    return
  }
  try {
    await axios.post(`http://localhost:8081/api/rides/${ride.id}/join`, {}, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    toast.success("Вы успешно присоединились к покатушке!")
    fetchFeed()
  } catch (err: any) {
    toast.error(err.response?.data?.Error || "Ошибка при присоединении")
  }
}

onMounted(() => {
  fetchCities()
  fetchFeed()
})

watch(() => appStore.currentSeason, () => {
  fetchFeed()
})

watch(selectedCityId, () => {
  fetchFeed()
})

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString('ru-RU', {
    day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit'
  })
}
</script>

<template>
  <div class="feed-container">
    <div class="feed-header glass-panel">
      <h2>Лента Покатушек</h2>
      <div class="filters">
        <select v-model="selectedCityId" class="input-field">
          <option :value="null">Все регионы</option>
          <option v-for="city in cities" :key="city.id" :value="city.id">
            {{ city.name }}, {{ city.regionName }}
          </option>
        </select>
      </div>
    </div>

    <div v-if="loading" class="spinner"></div>
    <div v-else class="feed-list">
      <div v-for="ride in rides" :key="ride.id" class="ride-card glass-panel">
        <div class="card-header">
          <h3>{{ ride.title }}</h3>
          <span class="date-badge">{{ formatDate(ride.eventDate) }}</span>
        </div>
        <p class="description">{{ ride.description }}</p>
        <div class="card-footer">
          <span>Орг: <strong>{{ ride.organizerName || 'Организатор' }}</strong></span>
          <span>Участники: {{ ride.membersCount || 0 }} / {{ ride.maxMembers || '∞' }}</span>
        </div>
        <div class="card-actions" v-if="authStore.isAuthenticated() && authStore.user?.userId !== ride.organizerId">
          <button class="btn-primary join-btn" @click.stop="joinRide(ride)">🏍️ Присоединиться</button>
        </div>
      </div>
      
      <div v-if="rides.length === 0" class="no-data">
        В этом регионе пока нет запланированных заездов для сезона 
        {{ appStore.currentSeason === 'enduro' ? 'Enduro' : 'Снегоходы' }}.
      </div>
    </div>
  </div>
</template>

<style scoped>
.feed-container {
  padding: 80px 20px 20px;
  max-width: 800px;
  margin: 0 auto;
  color: var(--text-light);
}
.feed-header {
  padding: 20px;
  border-radius: 16px;
  margin-bottom: 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 15px;
}
.feed-header h2 {
  margin: 0;
  color: var(--text-primary);
}
.input-field {
  padding: 10px 15px;
  border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.2);
  background: rgba(0,0,0,0.5);
  color: white;
  outline: none;
  min-width: 200px;
}
.feed-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}
.ride-card {
  padding: 20px;
  border-radius: 16px;
  transition: transform 0.2s, box-shadow 0.2s;
}
.ride-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 32px rgba(255, 77, 0, 0.15);
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 10px;
}
.card-header h3 {
  margin: 0;
  color: var(--accent-primary);
  font-size: 1.2rem;
}
.date-badge {
  background: rgba(255, 255, 255, 0.1);
  padding: 5px 10px;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 600;
}
.description {
  margin-bottom: 15px;
  color: rgba(255, 255, 255, 0.8);
  line-height: 1.5;
}
.card-footer {
  display: flex;
  justify-content: space-between;
  font-size: 0.9rem;
  color: rgba(255, 255, 255, 0.6);
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  padding-top: 10px;
}
.no-data {
  text-align: center;
  padding: 40px;
  background: rgba(0,0,0,0.2);
  border-radius: 16px;
  color: rgba(255,255,255,0.5);
}
.card-actions {
  margin-top: 15px;
}
.join-btn {
  width: 100%;
  padding: 12px;
  font-size: 1rem;
}
</style>
