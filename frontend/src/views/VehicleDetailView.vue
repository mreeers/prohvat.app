<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import api from '../services/api'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import LikeButton from '../components/LikeButton.vue'
import CommentSection from '../components/CommentSection.vue'

const route = useRoute()
const authStore = useAuthStore()
const vehicleId = route.params.id as string

const vehicle = ref<any>(null)
const loading = ref(true)
const loadError = ref('')

// Add log form
const showLogForm = ref(false)
const logTitle = ref('')
const logContent = ref('')
const logMetrics = ref<number>(0)
const selectedFiles = ref<File[]>([])
const submittingLog = ref(false)
const logError = ref('')

// Is the current user the owner?
const isOwner = computed(() => {
  if (!authStore.isAuthenticated() || !vehicle.value) return false
  return authStore.user?.userId === vehicle.value.ownerId
})

const fetchVehicle = async () => {
  loading.value = true
  loadError.value = ''
  try {
    const res = await api.get(`/vehicles/${vehicleId}`)
    vehicle.value = res.data
    logMetrics.value = res.data.currentMetricsValue
  } catch (err) {
    loadError.value = 'Транспортное средство не найдено.'
  } finally {
    loading.value = false
  }
}

const handleFileChange = (e: Event) => {
  const input = e.target as HTMLInputElement
  if (input.files) selectedFiles.value = Array.from(input.files)
}

const submitLog = async () => {
  if (!logTitle.value.trim()) { logError.value = 'Введите заголовок'; return }
  submittingLog.value = true
  logError.value = ''
  try {
    const formData = new FormData()
    formData.append('title', logTitle.value)
    formData.append('content', logContent.value)
    formData.append('metricsValue', logMetrics.value.toString())
    selectedFiles.value.forEach(file => formData.append('images', file))

    await api.post(`/vehicles/${vehicleId}/logs`, formData, {
      headers: { 'Content-Type': 'multipart/form-data', 'Authorization': `Bearer ${authStore.token}` }
    })

    logTitle.value = ''
    logContent.value = ''
    selectedFiles.value = []
    showLogForm.value = false
    await fetchVehicle() // Refresh to show new log
  } catch (error: any) {
    logError.value = error.response?.data?.Error || 'Не удалось добавить запись'
  } finally {
    submittingLog.value = false
  }
}

const formatDate = (dateStr: string) =>
  new Date(dateStr).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' })

onMounted(fetchVehicle)
</script>

<template>
  <div class="detail-container">
    <!-- Loading -->
    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
    </div>

    <!-- Error -->
    <div v-else-if="loadError" class="error-state glass-panel">
      <div style="font-size: 3rem; margin-bottom: 16px;">🔍</div>
      <h2>{{ loadError }}</h2>
    </div>

    <template v-else-if="vehicle">
      <!-- Vehicle Header -->
      <div class="glass-panel vehicle-header">
        <div class="vehicle-image-col">
          <img v-if="vehicle.imageUrl" :src="'/s3' + vehicle.imageUrl" :alt="`${vehicle.brand} ${vehicle.model}`" class="vehicle-img" />
          <div v-else class="vehicle-img-placeholder">🏍️</div>
        </div>
        <div class="vehicle-info-col">
          <div class="header-top">
            <span class="vehicle-category-tag">{{ vehicle.categoryName }}</span>
            <LikeButton :targetType="1" :targetId="vehicle.id" />
          </div>
          <h1 class="vehicle-title">{{ vehicle.brand }} {{ vehicle.model }}</h1>
          <p class="vehicle-year">{{ vehicle.year }}</p>

          <div class="metric-card">
            <span class="metric-label">Текущий пробег ({{ vehicle.odometerType === 2 ? 'м/ч' : 'км' }})</span>
            <span class="metric-value">{{ vehicle.currentMetricsValue }}</span>
          </div>

          <!-- Owner-only controls -->
          <div v-if="isOwner" class="owner-actions">
            <button class="btn-primary" @click="showLogForm = !showLogForm">
              {{ showLogForm ? '✕ Отмена' : '📝 Добавить запись в журнал' }}
            </button>
          </div>
          <div v-else-if="!authStore.isAuthenticated()" class="guest-hint">
            <router-link to="/auth/login">Войдите</router-link>, чтобы взаимодействовать
          </div>
        </div>
      </div>

      <!-- Add Log Form (owner only) -->
      <div v-if="showLogForm && isOwner" class="glass-panel log-form">
        <h3>📝 Новая запись в журнал</h3>
        <div class="form-group">
          <label>Заголовок <span class="required">*</span></label>
          <input v-model="logTitle" type="text" placeholder="Например: Замена масла, 5000 км" class="input-field" />
        </div>
        <div class="form-group">
          <label>Текущий пробег ({{ vehicle.odometerType === 2 ? 'м/ч' : 'км' }})</label>
          <input v-model.number="logMetrics" type="number" class="input-field" />
        </div>
        <div class="form-group">
          <label>Описание работ</label>
          <textarea v-model="logContent" placeholder="Что сделано, какие запчасти использованы..." class="input-field textarea"></textarea>
        </div>
        <div class="form-group">
          <label>Фотографии</label>
          <input type="file" multiple accept="image/*" @change="handleFileChange" class="file-input" />
          <span v-if="selectedFiles.length" class="file-hint">📎 {{ selectedFiles.length }} файл(ов) выбрано</span>
        </div>
        <div v-if="logError" class="form-error">⚠️ {{ logError }}</div>
        <div class="form-actions">
          <button class="btn-secondary" @click="showLogForm = false">Отмена</button>
          <button class="btn-primary" @click="submitLog" :disabled="submittingLog">
            {{ submittingLog ? 'Сохранение...' : '✓ Сохранить' }}
          </button>
        </div>
      </div>

      <!-- Logbook -->
      <div class="logbook-section">
        <h2 class="section-title">📋 Бортжурнал <span class="log-count">({{ vehicle.logs?.length || 0 }})</span></h2>

        <div v-if="!vehicle.logs || vehicle.logs.length === 0" class="empty-logs glass-panel">
          <div style="font-size: 2.5rem; margin-bottom: 12px;">📖</div>
          <p>{{ isOwner ? 'Журнал пуст. Добавьте первую запись!' : 'В журнале пока нет записей.' }}</p>
        </div>

        <div v-else class="logs-list">
          <div v-for="log in vehicle.logs" :key="log.id" class="glass-panel log-card">
            <div class="log-header">
              <h3 class="log-title">{{ log.title }}</h3>
              <div class="log-meta">
                <span class="log-date">{{ formatDate(log.createdAt) }}</span>
                <span class="log-metrics">🔧 {{ log.metricsValue }} км/мч</span>
              </div>
            </div>
            <p v-if="log.content" class="log-content">{{ log.content }}</p>
            <div v-if="log.imageUrls && log.imageUrls.length > 0" class="log-images">
              <img
                v-for="(url, i) in log.imageUrls" :key="i"
                :src="url.startsWith('http') ? url : '/s3' + url"
                :alt="'Фото ' + (Number(i)+1)"
                class="log-img"
              />
            </div>
            
            <div class="log-actions">
              <LikeButton :targetType="2" :targetId="log.id" />
            </div>

            <CommentSection :targetType="2" :targetId="log.id" />
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.detail-container {
  padding: 100px 32px 60px;
  max-width: 900px;
  margin: 0 auto;
  color: white;
}

.loading-state, .error-state {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  min-height: 60vh; text-align: center;
}
.error-state { padding: 60px; }
.spinner {
  width: 48px; height: 48px; border-radius: 50%;
  border: 4px solid rgba(255,255,255,0.1);
  border-top-color: var(--accent-primary);
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

/* Vehicle header */
.vehicle-header {
  display: flex; gap: 32px; padding: 32px; margin-bottom: 24px;
  align-items: flex-start;
}
.vehicle-image-col { flex-shrink: 0; }
.vehicle-img {
  width: 200px; height: 150px; object-fit: cover; border-radius: 12px;
  border: 2px solid rgba(255,255,255,0.1);
}
.vehicle-img-placeholder {
  width: 200px; height: 150px; border-radius: 12px;
  background: rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: center;
  font-size: 4rem; border: 2px dashed rgba(255,255,255,0.1);
}
.vehicle-info-col { flex: 1; }
.header-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}
.vehicle-category-tag {
  background: rgba(255,77,0,0.15); color: var(--accent-primary);
  border: 1px solid rgba(255,77,0,0.3); padding: 3px 12px; border-radius: 20px;
  font-size: 0.8rem; font-weight: 600;
}
.vehicle-title { font-size: 2rem; font-weight: 800; margin: 10px 0 4px; }
.vehicle-year { color: rgba(255,255,255,0.5); margin: 0 0 20px; }

.metric-card {
  display: inline-flex; flex-direction: column;
  background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1);
  border-radius: 12px; padding: 12px 20px; margin-bottom: 20px;
}
.metric-label { font-size: 0.75rem; color: rgba(255,255,255,0.5); }
.metric-value { font-size: 1.8rem; font-weight: 800; color: var(--accent-primary); }

.owner-actions { margin-top: 8px; }
.guest-hint { font-size: 0.85rem; color: rgba(255,255,255,0.4); margin-top: 8px; }
.guest-hint a { color: var(--accent-primary); text-decoration: none; font-weight: 600; }
.guest-hint a:hover { text-decoration: underline; }

/* Log form */
.log-form {
  padding: 28px; margin-bottom: 28px;
  display: flex; flex-direction: column; gap: 16px;
}
.log-form h3 { margin: 0; color: var(--accent-primary); }
.form-group { display: flex; flex-direction: column; gap: 6px; }
.form-group label { font-size: 0.85rem; color: rgba(255,255,255,0.6); font-weight: 600; }
.required { color: var(--accent-primary); }
.input-field {
  padding: 10px 14px; border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.15); background: rgba(0,0,0,0.3);
  color: white; font-size: 0.9rem; outline: none; transition: border-color 0.2s;
}
.input-field:focus { border-color: var(--accent-primary); }
.textarea { min-height: 100px; resize: vertical; }
.file-input { color: rgba(255,255,255,0.7); font-size: 0.85rem; }
.file-hint { font-size: 0.8rem; color: rgba(255,255,255,0.5); }
.form-error {
  color: #ff6b6b; font-size: 0.85rem; padding: 8px 12px;
  background: rgba(255,0,0,0.1); border-radius: 8px;
}
.form-actions { display: flex; gap: 10px; justify-content: flex-end; }

/* Logbook */
.logbook-section { margin-top: 8px; }
.section-title { font-size: 1.3rem; margin-bottom: 16px; }
.log-count { color: rgba(255,255,255,0.4); font-size: 1rem; font-weight: 400; }

.empty-logs {
  padding: 48px; text-align: center; color: rgba(255,255,255,0.5);
}
.logs-list { display: flex; flex-direction: column; gap: 16px; }

.log-card { padding: 24px; transition: transform 0.2s; }
.log-card:hover { transform: translateY(-2px); }
.log-header {
  display: flex; justify-content: space-between; align-items: flex-start;
  margin-bottom: 12px; gap: 16px;
}
.log-title { margin: 0; font-size: 1.1rem; font-weight: 700; }
.log-meta { display: flex; flex-direction: column; align-items: flex-end; gap: 4px; flex-shrink: 0; }
.log-date { font-size: 0.8rem; color: rgba(255,255,255,0.4); }
.log-metrics { font-size: 0.85rem; color: var(--accent-primary); font-weight: 600; }
.log-content { color: rgba(255,255,255,0.75); line-height: 1.6; margin: 0 0 16px; }

.log-images {
  display: flex; flex-wrap: wrap; gap: 10px; margin-top: 12px;
}
.log-img {
  width: 120px; height: 90px; object-fit: cover; border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.1); cursor: pointer;
  transition: transform 0.2s;
}
.log-img:hover { transform: scale(1.05); }

/* Buttons */
.btn-primary {
  background: var(--accent-primary); color: white; border: none;
  padding: 10px 20px; border-radius: 8px; cursor: pointer; font-weight: 600;
  transition: opacity 0.2s; font-size: 0.9rem;
}
.btn-primary:hover:not(:disabled) { opacity: 0.85; }
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-secondary {
  background: transparent; border: 1px solid rgba(255,255,255,0.2);
  color: rgba(255,255,255,0.8); padding: 10px 20px; border-radius: 8px;
  cursor: pointer; font-weight: 600; transition: all 0.2s;
}
.btn-secondary:hover { background: rgba(255,255,255,0.08); }
</style>
