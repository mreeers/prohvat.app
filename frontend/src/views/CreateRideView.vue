<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import api from '../services/api'
import { useToast } from 'vue-toastification'

const router = useRouter()
const authStore = useAuthStore()
const toast = useToast()

const mapContainer = ref<HTMLElement | null>(null)
let map: any = null
let ymaps: any = null
let startPlacemark: any = null

const categories = ref<any[]>([])
const cities = ref<any[]>([])

const title = ref('')
const description = ref('')
const targetCategoryId = ref('')
const cityId = ref('')
const eventDate = ref('')
const complexity = ref(1)
const season = ref(1) // 1=Summer, 2=Winter, 3=AllSeason
const maxMembers = ref(10)
const latitude = ref<number | null>(null)
const longitude = ref<number | null>(null)

const isCreating = ref(false)

const setDefaultDate = () => {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  eventDate.value = d.toISOString().slice(0, 16)
}

const fetchMetadata = async () => {
  try {
    const [catRes, cityRes] = await Promise.all([
      api.get('/vehicles/categories'),
      api.get('/location/cities')
    ])
    categories.value = catRes.data
    cities.value = cityRes.data
  } catch (e) {
    toast.error('Ошибка загрузки метаданных')
  }
}

const initMap = () => {
  ymaps = (window as any).ymaps
  if (!ymaps) return
  
  ymaps.ready(() => {
    map = new ymaps.Map(mapContainer.value, {
      center: [55.7558, 37.6173],
      zoom: 10,
      controls: ['zoomControl']
    })

    // Try geolocation
    ymaps.geolocation.get({ provider: 'browser', mapStateAutoApply: true })
      .then((res: any) => {
        map.setCenter(res.geoObjects.position)
      })

    map.events.add('click', (e: any) => {
      const coords = e.get('coords')
      latitude.value = coords[0]
      longitude.value = coords[1]

      if (startPlacemark) {
        startPlacemark.geometry.setCoordinates(coords)
      } else {
        startPlacemark = new ymaps.Placemark(coords, {}, { preset: 'islands#redStretchyIcon' })
        map.geoObjects.add(startPlacemark)
      }
    })
  })
}

onMounted(() => {
  if (!authStore.isAuthenticated()) {
    toast.warning("Войдите в аккаунт для создания маршрута")
    router.push('/auth/login')
    return
  }
  
  setDefaultDate()
  fetchMetadata()
  
  if ((window as any).ymaps) {
    initMap()
  } else {
    const script = document.createElement('script')
    script.src = 'https://api-maps.yandex.ru/2.1/?apikey=7b6b1509-5a50-482a-a9e9-b5f795db2b29&lang=ru_RU'
    script.onload = initMap
    document.head.appendChild(script)
  }
})

onUnmounted(() => {
  if (map) map.destroy()
})

const submitRide = async () => {
  if (!title.value || !targetCategoryId.value || !latitude.value || !longitude.value || !cityId.value) {
    toast.error("Заполните все обязательные поля и укажите точку на карте!")
    return
  }

  isCreating.value = true
  try {
    const payload = {
      title: title.value,
      description: description.value,
      targetCategoryId: targetCategoryId.value,
      eventDate: new Date(eventDate.value).toISOString(),
      complexity: complexity.value,
      season: season.value,
      latitude: latitude.value,
      longitude: longitude.value,
      cityId: cityId.value,
      maxMembers: maxMembers.value
    }
    
    await api.post('/rides', payload, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    
    toast.success("Покатушка успешно создана!")
    router.push('/')
  } catch (err: any) {
    toast.error(err.response?.data?.Error || "Ошибка создания покатушки")
  } finally {
    isCreating.value = false
  }
}
</script>

<template>
  <div class="create-ride-container">
    <div class="glass-panel form-layout">
      <div class="form-section">
        <h2>Создание покатушки</h2>
        
        <div class="form-group">
          <label>Название <span class="required">*</span></label>
          <input v-model="title" class="input-field" placeholder="Вечерний прохват по грязи..." />
        </div>

        <div class="form-group">
          <label>Описание</label>
          <textarea v-model="description" class="input-field" placeholder="Грязь, бревна, берем чай в термосах..."></textarea>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Город <span class="required">*</span></label>
            <select v-model="cityId" class="input-field">
              <option disabled value="">Выберите город</option>
              <option v-for="c in cities" :key="c.id" :value="c.id">{{ c.name }}, {{ c.regionName }}</option>
            </select>
          </div>
          <div class="form-group">
            <label>Тип техники <span class="required">*</span></label>
            <select v-model="targetCategoryId" class="input-field">
              <option disabled value="">Выберите тип</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Дата и время <span class="required">*</span></label>
            <input type="datetime-local" v-model="eventDate" class="input-field" />
          </div>
          <div class="form-group">
            <label>Сезонность</label>
            <select v-model="season" class="input-field">
              <option :value="1">Лето (Enduro/Питбайк)</option>
              <option :value="2">Зима (Снегоход/Сноубайк)</option>
              <option :value="3">Всесезонно</option>
            </select>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Сложность (1-5)</label>
            <input type="number" v-model="complexity" min="1" max="5" class="input-field" />
          </div>
          <div class="form-group">
            <label>Макс. участников</label>
            <input type="number" v-model="maxMembers" min="1" max="1000" class="input-field" />
          </div>
        </div>

        <button @click="submitRide" :disabled="isCreating" class="btn-primary mt-4 w-full text-lg py-3">
          {{ isCreating ? 'Создание...' : '🚀 Опубликовать' }}
        </button>
      </div>

      <div class="map-section">
        <h3>Точка сбора <span class="required">*</span></h3>
        <p class="text-gray-400 text-sm mb-2">Кликните на карту, чтобы установить маркер.</p>
        <div ref="mapContainer" class="mini-map"></div>
        <div v-if="latitude" class="coords text-sm text-green-400 mt-2">
          Координаты: {{ latitude.toFixed(4) }}, {{ longitude?.toFixed(4) }}
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.create-ride-container {
  padding: 100px 20px 20px;
  max-width: 1000px;
  margin: 0 auto;
  min-height: 100vh;
}
.form-layout {
  display: flex;
  flex-direction: row;
  gap: 30px;
  padding: 30px;
  border-radius: 16px;
}
.form-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 15px;
}
.map-section {
  flex: 1;
  display: flex;
  flex-direction: column;
}
.mini-map {
  flex: 1;
  width: 100%;
  min-height: 300px;
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid rgba(255,255,255,0.1);
  filter: saturate(1.2) contrast(1.1);
}
.form-row {
  display: flex;
  gap: 15px;
}
.form-group {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 5px;
}
.input-field {
  padding: 12px;
  border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.15);
  background: rgba(0,0,0,0.4);
  color: white;
  outline: none;
  font-size: 1rem;
}
.required {
  color: var(--accent-primary);
}
h2 {
  margin: 0 0 10px 0;
  color: var(--text-primary);
}
h3 {
  margin: 0 0 5px 0;
  color: var(--accent-primary);
}
@media (max-width: 768px) {
  .form-layout {
    flex-direction: column;
  }
  .mini-map {
    min-height: 400px;
  }
}
</style>
