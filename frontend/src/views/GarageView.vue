<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../services/api'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const vehicles = ref<any[]>([])
const loading = ref(true)

const fetchVehicles = async () => {
  try {
    const response = await api.get('/vehicles/my', {
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

const getTopSpecs = (configJson: string | null) => {
  if (!configJson) return []
  try {
    const obj = JSON.parse(configJson)
    return Object.values(obj).slice(0, 3) as string[]
  } catch {
    return []
  }
}

onMounted(() => {
  fetchVehicles()
})
</script>

<template>
  <div class="garage-container">
    <div class="garage-header">
      <h2>🏍️ Мой Гараж</h2>
      <button class="btn-add-vehicle" @click="router.push('/garage/new')">+ Добавить боевую технику</button>
    </div>

    <div v-if="loading" class="loading-state">Загрузка гаража...</div>
    <div v-else class="vehicle-grid">
      <div
        v-for="vehicle in vehicles"
        :key="vehicle.id"
        class="vehicle-card glass-panel"
        @click="router.push(`/garage/${vehicle.id}`)"
      >
        <div class="card-img-wrap">
          <img
            v-if="vehicle.imageUrl"
            :src="vehicle.imageUrl.startsWith('http') ? vehicle.imageUrl : '/s3' + vehicle.imageUrl"
            :alt="`${vehicle.brand} ${vehicle.model}`"
            class="card-img"
          />
          <div v-else class="card-img-placeholder">🏍️</div>
          <span class="card-category-badge">{{ vehicle.categoryName }}</span>
        </div>

        <div class="card-body">
          <h3 class="card-title">{{ vehicle.brand }} {{ vehicle.model }}</h3>
          <div class="card-meta">
            <span>{{ vehicle.year }} г.в.</span>
            <span class="dot">•</span>
            <span class="metrics-badge">
              {{ vehicle.currentMetricsValue }} {{ vehicle.odometerType === 2 ? 'м/ч' : 'км' }}
            </span>
          </div>

          <!-- Tuning highlights -->
          <div v-if="getTopSpecs(vehicle.technicalConfigJson).length > 0" class="tuning-chips">
            <span
              v-for="(spec, i) in getTopSpecs(vehicle.technicalConfigJson)"
              :key="i"
              class="tuning-badge"
            >
              ⚡ {{ spec }}
            </span>
          </div>
        </div>
      </div>
      
      <div class="vehicle-card add-card glass-panel" @click="router.push('/garage/new')">
        <div class="plus-icon">+</div>
        <p>Добавить технику</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.garage-container {
  padding: 80px 20px 40px;
  max-width: 1200px;
  margin: 0 auto;
  color: var(--text-light);
}

.garage-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.garage-header h2 {
  font-size: 1.8rem;
  margin: 0;
  color: #fff;
}

.btn-add-vehicle {
  padding: 10px 20px;
  background: linear-gradient(135deg, #ff6b00 0%, #ff8c42 100%);
  border: none;
  border-radius: 10px;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(255, 107, 0, 0.35);
}

.vehicle-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 22px;
}

.vehicle-card {
  border-radius: 16px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.25s ease;
  background: rgba(26, 26, 36, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.08);
  display: flex;
  flex-direction: column;
}

.vehicle-card:hover {
  transform: translateY(-6px);
  border-color: rgba(255, 107, 0, 0.4);
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.4);
}

.card-img-wrap {
  position: relative;
  width: 100%;
  height: 170px;
  background: #111;
  overflow: hidden;
}

.card-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.card-img-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 3rem;
  background: rgba(255, 255, 255, 0.03);
}

.card-category-badge {
  position: absolute;
  top: 12px;
  left: 12px;
  background: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(8px);
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 0.75rem;
  color: #fff;
  border: 1px solid rgba(255, 255, 255, 0.15);
}

.card-body {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  flex: 1;
}

.card-title {
  margin: 0;
  font-size: 1.15rem;
  color: #fff;
  font-weight: 700;
}

.card-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.85rem;
  color: #999;
}

.metrics-badge {
  color: #ff8c42;
  font-weight: 600;
}

.tuning-chips {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-top: 4px;
}

.tuning-badge {
  font-size: 0.75rem;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  padding: 3px 8px;
  border-radius: 6px;
  color: #ccc;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.add-card {
  min-height: 250px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border: 2px dashed rgba(255, 255, 255, 0.2);
  background: rgba(255, 255, 255, 0.02);
}

.plus-icon {
  font-size: 3.5rem;
  color: #ff8c42;
  line-height: 1;
  margin-bottom: 8px;
}

.add-card p {
  margin: 0;
  color: #aaa;
  font-size: 0.95rem;
}
</style>
