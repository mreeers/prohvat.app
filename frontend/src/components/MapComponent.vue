<script setup lang="ts">
import { onMounted, onUnmounted, ref, watch } from 'vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const props = defineProps<{
  season: 'enduro' | 'snowmobile'
}>()

const mapContainer = ref<HTMLElement | null>(null)
let map: any = null
let ymaps: any = null
const authStore = useAuthStore()

// Default to Moscow, but will try geolocation
let currentLat = 55.7558
let currentLng = 37.6173

const rides = ref<any[]>([])
const spots = ref<any[]>([])
const activeRiders = ref<any[]>([])
const categories = ref<any[]>([])

// Create Event State
const showCreateForm = ref(false)
const createLat = ref(0)
const createLng = ref(0)
const createType = ref<'ride'|'spot'>('ride')
const newTitle = ref('')
const newDesc = ref('')
const newComplexity = ref(1)
const newMaxMembers = ref(10)
const newEventDate = ref('')
const newCategoryId = ref('')
const newSeason = ref(1) // 1=Summer, 2=Winter, 3=AllSeason
const createError = ref('')

const complexityColors: Record<number, string> = { 1: '#4caf50', 2: '#8bc34a', 3: '#ff9800', 4: '#ff5722', 5: '#f44336' }

const setDefaultDate = () => {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  newEventDate.value = d.toISOString().slice(0, 16)
}

import RideDetailModal from './RideDetailModal.vue'
const selectedRide = ref<any | null>(null)

const fetchEvents = async (bounds: number[][]) => {
  try {
    const minLat = bounds[0][0]
    const minLng = bounds[0][1]
    const maxLat = bounds[1][0]
    const maxLng = bounds[1][1]
    
    const seasonFilter = props.season === 'snowmobile' ? 2 : 1
    const [ridesRes, spotsRes] = await Promise.all([
      api.get(`/rides/bounds?minLat=${minLat}&minLng=${minLng}&maxLat=${maxLat}&maxLng=${maxLng}&season=${seasonFilter}`),
      api.get(`/spots/bounds?minLat=${minLat}&minLng=${minLng}&maxLat=${maxLat}&maxLng=${maxLng}&season=${seasonFilter}`)
    ])
    
    rides.value = ridesRes.data
    spots.value = spotsRes.data
    updateMapMarkers()
  } catch (err) {
    console.error('Failed to fetch events', err)
  }
}

const fetchActiveRiders = async () => {
  try {
    const res = await api.get('/profile/active-riders')
    activeRiders.value = res.data
    updateMapMarkers()
  } catch (err) {
    console.error('Failed to fetch active riders', err)
  }
}

const fetchCategories = async () => {
  try {
    const res = await api.get('/vehicles/categories')
    categories.value = res.data
    if (res.data.length > 0) newCategoryId.value = res.data[0].id
  } catch (err) {
    console.error('Failed to fetch categories', err)
  }
}

const createEvent = async () => {
  createError.value = ''
  if (!newTitle.value.trim()) { createError.value = 'Введите название'; return }
  
  try {
    if (createType.value === 'spot') {
      await api.post('/spots', {
        title: newTitle.value,
        description: newDesc.value,
        complexity: newComplexity.value,
        season: newSeason.value,
        latitude: createLat.value,
        longitude: createLng.value
      }, { headers: { 'Authorization': `Bearer ${authStore.token}` } })
      
      alert('Спот добавлен!');
    }
    
    showCreateForm.value = false
    newTitle.value = ''
    newDesc.value = ''
    newComplexity.value = 1
    newMaxMembers.value = 10
    setDefaultDate()
    if (map) fetchEvents(map.getBounds())
  } catch (err: any) {
    createError.value = err.response?.data?.Error || 'Ошибка при создании'
  }
}

const updateMapMarkers = () => {
  if (!map) return
  map.geoObjects.removeAll()

  // Add Rides
  rides.value.forEach(ride => {
    const placemark = new ymaps.Placemark([ride.startLat, ride.startLng], {
      balloonContentHeader: `<b>${ride.title}</b>`,
      balloonContentBody: `
        <p>${ride.description}</p>
        <p><b>Дата:</b> ${new Date(ride.eventDate).toLocaleString()}</p>
        <button id="btn-join-${ride.id}" class="btn-primary" style="margin-top:10px;">Подробнее</button>
      `
    }, {
      iconLayout: 'default#imageWithContent',
      iconImageHref: '', 
      iconContentLayout: ymaps.templateLayoutFactory.createClass(
        `<div style="background-color: ${complexityColors[ride.complexity] || '#ff4d00'}; width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; font-weight: bold; border: 2px solid white; box-shadow: 0 0 10px rgba(0,0,0,0.5);">🏍️</div>`
      )
    })
    
    placemark.events.add('click', () => {
      setTimeout(() => {
        const btn = document.getElementById(`btn-join-${ride.id}`)
        if (btn) btn.onclick = () => { selectedRide.value = ride }
      }, 100)
    })

    map.geoObjects.add(placemark)
  })

  // Add Spots
  spots.value.forEach(spot => {
    const placemark = new ymaps.Placemark([spot.latitude, spot.longitude], {
      balloonContentHeader: `<b>⭐ Спот: ${spot.title}</b>`,
      balloonContentBody: `${spot.description}`
    }, {
      iconLayout: 'default#imageWithContent',
      iconImageHref: '', 
      iconContentLayout: ymaps.templateLayoutFactory.createClass(
        `<div style="background-color: gold; width: 20px; height: 20px; border-radius: 4px; border: 2px solid white; transform: rotate(45deg); box-shadow: 0 0 10px gold;"></div>`
      )
    })
    map.geoObjects.add(placemark)
  })

  // Add Active Riders
  activeRiders.value.forEach(rider => {
    const isStreet = rider.vehicleCategoryName === 'Дорожный мотоцикл'
    const ringColor = isStreet ? '#2196f3' : '#ff9800'
    const avatar = rider.avatarUrl ? (rider.avatarUrl.startsWith('http') ? rider.avatarUrl : '/s3' + rider.avatarUrl) : 'https://ui-avatars.com/api/?name=' + rider.username
    const vehicleText = rider.vehicleName ? `<br>🏍️ ${rider.vehicleName}` : ''

    const placemark = new ymaps.Placemark([rider.latitude, rider.longitude], {
      balloonContentHeader: `<b>@${rider.username}</b>`,
      balloonContentBody: `<p>Сейчас катается!${vehicleText}</p>`
    }, {
      iconLayout: 'default#imageWithContent',
      iconImageHref: '',
      iconContentLayout: ymaps.templateLayoutFactory.createClass(
        `<div style="width: 36px; height: 36px; border-radius: 50%; display: flex; align-items: center; justify-content: center; border: 3px solid ${ringColor}; box-shadow: 0 0 10px rgba(0,0,0,0.5); background-image: url('${avatar}'); background-size: cover; background-position: center; background-color: #333;"></div>`
      )
    })
    map.geoObjects.add(placemark)
  })
}

const initMap = async () => {
  // Try to get user's city location before rendering map
  let centerLat = currentLat
  let centerLng = currentLng

  if (authStore.isAuthenticated() && authStore.user?.username) {
    try {
      const res = await api.get(`/profile/${authStore.user.username}`)
      if (res.data && res.data.cityLat) {
        centerLat = res.data.cityLat
        centerLng = res.data.cityLng
      }
    } catch (e) { console.error("Failed to fetch user city") }
  }

  ymaps = (window as any).ymaps
  ymaps.ready(() => {
    if (!mapContainer.value) return
    map = new ymaps.Map(mapContainer.value, {
      center: [centerLat, centerLng],
      zoom: 10,
      controls: ['zoomControl', 'typeSelector', 'geolocationControl']
    })
    
    map.setType('yandex#hybrid')

    // Fetch rides on map move
    map.events.add('boundschange', (e: any) => {
      fetchEvents(e.get('newBounds'))
    })

    // Click map to organize ride
    map.events.add('click', (e: any) => {
      if (!authStore.isAuthenticated()) return
      const coords = e.get('coords')
      createLat.value = coords[0]
      createLng.value = coords[1]
      showCreateForm.value = true
    })

    fetchEvents(map.getBounds())
    fetchActiveRiders()
  })
}

let activeRidersInterval: any = null

onMounted(() => {
  fetchCategories()
  setDefaultDate()
  if (!(window as any).ymaps) {
    const script = document.createElement('script')
    script.src = 'https://api-maps.yandex.ru/2.1/?lang=ru_RU'
    script.onload = initMap
    document.head.appendChild(script)
  } else {
    initMap()
  }

  // Geolocation and SOS tracking
  if (navigator.geolocation && authStore.isAuthenticated()) {
    navigator.geolocation.getCurrentPosition(async (position) => {
      currentLat = position.coords.latitude
      currentLng = position.coords.longitude
      if (map) map.setCenter([currentLat, currentLng])

      try {
        await api.put('/profile/location', {
          latitude: currentLat,
          longitude: currentLng
        }, {
          headers: { 'Authorization': `Bearer ${authStore.token}` }
        })
      } catch (e) {
        console.error('Failed to update location', e)
      }
    })

    // Track periodically every 3 minutes
    setInterval(() => {
      navigator.geolocation.getCurrentPosition(async (position) => {
        try {
          await api.put('/profile/location', {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude
          }, {
            headers: { 'Authorization': `Bearer ${authStore.token}` }
          })
        } catch (e) {
          console.error('Failed to update location', e)
        }
      })
    }, 180000)
  }

  // Poll active riders every 30 seconds
  activeRidersInterval = setInterval(() => {
    fetchActiveRiders()
  }, 30000)
})

onUnmounted(() => {
  if (activeRidersInterval) clearInterval(activeRidersInterval)
})

const sendSos = async () => {
  if (!authStore.isAuthenticated()) return alert("Войдите чтобы отправить сигнал SOS")
  
  if (!navigator.geolocation) {
    alert("Геолокация не поддерживается вашим браузером")
    return
  }

  navigator.geolocation.getCurrentPosition(async (position) => {
    try {
      await api.post('/sos', {
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
        message: "Мне нужна помощь!"
      }, {
        headers: { 'Authorization': `Bearer ${authStore.token}` }
      })
      alert("Сигнал бедствия успешно отправлен. Ближайшие райдеры получили уведомление.")
    } catch (e) {
      alert("Ошибка при отправке SOS")
    }
  }, () => {
    alert("Не удалось определить местоположение для SOS")
  })
}

watch(() => props.season, () => {
  updateMapMarkers()
})

onUnmounted(() => {
  if (map) {
    map.destroy()
  }
})
</script>

<template>
  <div class="map-wrapper">
    <div ref="mapContainer" class="map-container"></div>
    
    <!-- Login hint for anonymous users (non-blocking) -->
    <div v-if="!authStore.isAuthenticated()" class="auth-hint glass-panel">
      Чтобы участвовать в покатушках и отправлять SOS, <router-link to="/login">войдите в аккаунт</router-link>.
    </div>

    <button v-if="authStore.isAuthenticated()" @click="sendSos" class="sos-btn glass-panel">
      <div class="sos-pulse"></div>
      🚨 SOS
    </button>

    <template v-if="authStore.isAuthenticated()">
      <button class="create-btn glass-panel" @click="showCreateForm = true; createType='spot'; setDefaultDate();">
        <span class="plus-icon">+</span> Добавить Спот
      </button>
      <router-link to="/rides/new" class="create-btn glass-panel" style="text-decoration: none; display: flex; align-items: center; justify-content: center;">
        <span class="plus-icon">+</span> Создать маршрут
      </router-link>
    </template>

    <!-- Create Event Form Overlay -->
    <div v-if="showCreateForm" class="create-form glass-panel">
      <div class="form-header">
        <h3>📍 Добавить маркер</h3>
        <button class="close-form-btn" @click="showCreateForm = false">✕</button>
      </div>
      
      <!-- Type Toggle -->
      <div class="type-toggle" style="margin-bottom: 10px;">
        <button class="type-btn active">⭐ Спот</button>
      </div>
      
      <input v-model="newTitle" placeholder="Название спота..." class="input-field" />
      <textarea v-model="newDesc" placeholder="Описание, требования, маршрут..." class="input-field textarea"></textarea>

      <!-- Ride-specific fields -->
      <template v-if="createType === 'spot'">
        <div class="form-group">
          <label class="field-label">🎯 Уровень скилла</label>
          <div class="skill-selector">
            <button 
              v-for="(label, val) in { 1: 'Начинающий', 2: 'Средний', 3: 'Продвинутый', 4: 'Эксперт', 5: 'Экстрим' }"
              :key="val"
              class="skill-btn"
              :class="{ active: newComplexity === Number(val) }"
              @click="newComplexity = Number(val)"
            >{{ label }}</button>
          </div>
          <br>
          <label class="field-label">Сезонность</label>
          <select v-model="newSeason" class="input-field">
            <option :value="1">Лето</option>
            <option :value="2">Зима</option>
            <option :value="3">Всесезон</option>
          </select>
        </div>
      </template>

      <div v-if="createError" class="form-error">⚠️ {{ createError }}</div>
      
      <div class="form-actions">
        <button @click="showCreateForm = false" class="btn-secondary">Отмена</button>
        <button @click="createEvent" class="btn-primary">✓ Создать</button>
      </div>
    </div>

    <!-- Ride Details Modal (Comments & Reviews) -->
    <RideDetailModal 
      v-if="selectedRide" 
      :ride="selectedRide" 
      @close="selectedRide = null" 
    />
  </div>
</template>

<style scoped>
.map-wrapper {
  position: absolute;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
}
.map-container { width: 100%; height: 100%; filter: saturate(1.2) contrast(1.1); }
:deep(.ymaps-2-1-79-copyrights-pane) { display: none !important; }

.auth-hint {
  position: absolute; bottom: 24px; left: 50%; transform: translateX(-50%);
  z-index: 1000; padding: 10px 20px;
  font-size: 0.9rem; color: rgba(255,255,255,0.8);
  border-radius: 100px; white-space: nowrap;
}
.auth-hint a { color: var(--accent-primary); font-weight: 700; text-decoration: none; }
.auth-hint a:hover { text-decoration: underline; }

.create-form {
  position: absolute; top: 80px; right: 20px; z-index: 1000; width: 380px;
  max-height: calc(100vh - 120px); overflow-y: auto;
  padding: 20px; display: flex; flex-direction: column; gap: 12px;
  scrollbar-width: thin;
}

.form-header {
  display: flex; justify-content: space-between; align-items: center;
}
.form-header h3 { margin: 0; color: var(--primary-color); font-size: 1.1rem; }
.close-form-btn {
  background: none; border: none; color: rgba(255,255,255,0.5); cursor: pointer;
  font-size: 1.1rem; padding: 4px 8px; border-radius: 6px; transition: all 0.2s;
}
.close-form-btn:hover { background: rgba(255,255,255,0.1); color: white; }

.type-toggle {
  display: flex; gap: 8px; background: rgba(0,0,0,0.3); padding: 4px; border-radius: 10px;
}
.type-btn {
  flex: 1; padding: 8px; border: none; border-radius: 8px; cursor: pointer;
  background: transparent; color: rgba(255,255,255,0.6); font-size: 0.85rem;
  font-weight: 600; transition: all 0.2s;
}
.type-btn.active {
  background: var(--accent-primary); color: white;
  box-shadow: 0 0 10px rgba(255,77,0,0.3);
}
.type-btn:hover:not(.active) { background: rgba(255,255,255,0.08); color: white; }

.input-field {
  width: 100%; padding: 10px 12px; border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.15); background: rgba(0,0,0,0.4); color: white;
  outline: none; font-size: 0.9rem; box-sizing: border-box;
  transition: border-color 0.2s;
}
.input-field:focus { border-color: var(--accent-primary); }
.input-field option { background: #1a1a1a; }
.textarea { min-height: 70px; resize: vertical; }

.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.form-group { display: flex; flex-direction: column; gap: 5px; }
.field-label { font-size: 0.78rem; color: rgba(255,255,255,0.6); font-weight: 600; }

.complexity-selector { display: flex; gap: 4px; }
.complexity-btn {
  flex: 1; padding: 6px; border: 1px solid rgba(255,255,255,0.15);
  border-radius: 6px; background: rgba(0,0,0,0.3); color: rgba(255,255,255,0.5);
  cursor: pointer; font-weight: 700; transition: all 0.2s; font-size: 0.85rem;
}
.complexity-btn.active { background: var(--accent-primary); color: white; border-color: var(--accent-primary); }

.skill-selector { display: flex; flex-wrap: wrap; gap: 6px; }
.skill-btn {
  padding: 5px 10px; border: 1px solid rgba(255,255,255,0.15);
  border-radius: 20px; background: rgba(0,0,0,0.3); color: rgba(255,255,255,0.6);
  cursor: pointer; font-size: 0.78rem; font-weight: 600; transition: all 0.2s;
}
.skill-btn.active { background: var(--accent-primary); color: white; border-color: var(--accent-primary); }

.form-error {
  color: #ff6b6b; font-size: 0.85rem; padding: 8px 12px;
  background: rgba(255,0,0,0.1); border-radius: 8px; border: 1px solid rgba(255,0,0,0.2);
}

.form-actions { display: flex; justify-content: flex-end; gap: 10px; }
.btn-secondary { background: transparent; border: 1px solid rgba(255,255,255,0.2); color: white; padding: 8px 16px; border-radius: 8px; cursor: pointer; }
.btn-secondary:hover { background: rgba(255,255,255,0.1); }
</style>

<style scoped>
.sos-btn {
  position: absolute;
  top: 80px;
  right: 20px;
  background-color: rgba(255, 60, 60, 0.85) !important;
  color: white;
  border: 2px solid #ff4444 !important;
  border-radius: 50%;
  width: 60px;
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 800;
  font-size: 14px;
  box-shadow: 0 0 15px rgba(255, 0, 0, 0.5);
  cursor: pointer;
  z-index: 10;
  transition: all 0.2s ease-in-out;
}

.sos-btn:hover {
  transform: scale(1.1);
  box-shadow: 0 0 25px rgba(255, 0, 0, 0.8);
  background-color: rgba(255, 30, 30, 1) !important;
}

.sos-pulse {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background-color: rgba(255, 0, 0, 0.4);
  z-index: -1;
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0% { transform: scale(1); opacity: 1; }
  100% { transform: scale(1.5); opacity: 0; }
}
</style>
