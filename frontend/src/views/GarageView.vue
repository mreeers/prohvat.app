<script setup lang="ts">
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const vehicles = ref<any[]>([])
const loading = ref(true)

const fetchVehicles = async () => {
  try {
    const response = await axios.get('http://localhost:8081/api/vehicles/my', {
      headers: {
        'Authorization': `Bearer ${authStore.token}`
      }
    })
    vehicles.value = response.data
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchVehicles()
})
</script>

<template>
  <div class="garage-container">
    <h2>Мой Гараж</h2>
    <div v-if="loading">Загрузка...</div>
    <div v-else class="vehicle-grid">
      <div v-for="vehicle in vehicles" :key="vehicle.id" class="vehicle-card glass-panel" @click="router.push(`/garage/${vehicle.id}`)">
        <h3>{{ vehicle.brand }} {{ vehicle.model }}</h3>
        <p>Год: {{ vehicle.year }}</p>
        <p>Категория: {{ vehicle.categoryName }}</p>
        <div class="metrics">{{ vehicle.currentMetricsValue }} мч/км</div>
      </div>
      
      <div class="vehicle-card add-card glass-panel" @click="router.push('/garage/new')">
        <div class="plus-icon">+</div>
        <p>Добавить технику</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.garage-container { padding: 80px 20px 20px; max-width: 1200px; margin: 0 auto; color: var(--text-light); }
.vehicle-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 20px; margin-top: 20px; }
.vehicle-card { padding: 20px; border-radius: 16px; cursor: pointer; transition: transform 0.2s, box-shadow 0.2s; }
.vehicle-card:hover { transform: translateY(-5px); box-shadow: 0 10px 20px rgba(0,0,0,0.3); }
.vehicle-card h3 { color: var(--primary-color); margin-bottom: 10px; }
.add-card { display: flex; flex-direction: column; align-items: center; justify-content: center; border: 2px dashed rgba(255,255,255,0.2); }
.plus-icon { font-size: 3rem; color: var(--primary-color); margin-bottom: 10px; }
.metrics { margin-top: 15px; font-weight: bold; padding: 5px 10px; background: rgba(255,255,255,0.1); border-radius: 8px; display: inline-block; }
</style>
