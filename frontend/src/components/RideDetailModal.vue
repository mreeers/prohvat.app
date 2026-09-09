<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch, nextTick } from 'vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import CommentSection from './CommentSection.vue'
import { useToast } from 'vue-toastification'

const props = defineProps<{
  ride: any
}>()

const emit = defineEmits(['close', 'joined'])
const authStore = useAuthStore()
const toast = useToast()

const activeTab = ref<'info' | 'members' | 'comments' | 'reviews'>('info')
const fullRide = ref<any>(null)
const loadingRide = ref(true)
const isMember = ref(false)

const reviews = ref<any[]>([])
const newReviewText = ref('')
const newReviewRating = ref(5)

const newStatus = ref(1)
const newReport = ref('')
const savingStatus = ref(false)

// GPX upload state
const gpxInputRef = ref<HTMLInputElement | null>(null)
const uploadingGpx = ref(false)

// Map state
const mapContainer = ref<HTMLElement | null>(null)
let modalMap: any = null
let gpxPolyline: any = null

const isOrganizer = computed(() => {
  if (!authStore.user || !props.ride) return false
  const orgId = fullRide.value?.organizerId || props.ride.organizerId || props.ride.authorId
  return authStore.user.userId === orgId
})

const complexityLabels: Record<string, string> = {
  Easy: 'Легкий',
  Middle: 'Средний',
  Hard: 'Хард-экстрим'
}

const fetchRideData = async () => {
  loadingRide.value = true
  try {
    const rideId = props.ride.id || props.ride.targetId
    const res = await api.get(`/rides/${rideId}`)
    fullRide.value = res.data
    isMember.value = res.data.isJoined || false
    newStatus.value = res.data.status || 1
    newReport.value = res.data.report || ''

    // Fetch reviews
    try {
      const revRes = await api.get(`/social/reviews/${rideId}`)
      reviews.value = revRes.data || []
    } catch {
      reviews.value = []
    }

    // Init or update map
    await nextTick()
    initOrUpdateMap()
  } catch (err) {
    console.error('Failed to load ride details', err)
    toast.error('Не удалось загрузить подробности покатушки')
  } finally {
    loadingRide.value = false
  }
}

const initOrUpdateMap = () => {
  const ymaps = (window as any).ymaps
  if (!ymaps) {
    if (!document.getElementById('ymaps-script')) {
      const script = document.createElement('script')
      script.id = 'ymaps-script'
      script.src = 'https://api-maps.yandex.ru/2.1/?apikey=7b6b1509-5a50-482a-a9e9-b5f795db2b29&lang=ru_RU'
      script.onload = () => ymaps?.ready(renderMap)
      document.head.appendChild(script)
    }
    return
  }
  ymaps.ready(renderMap)
}

const renderMap = () => {
  const ymaps = (window as any).ymaps
  if (!mapContainer.value || !ymaps) return

  const ride = fullRide.value || props.ride
  const startLat = ride.startLat || (ride.startPoint ? ride.startPoint.y : 56.8389)
  const startLng = ride.startLng || (ride.startPoint ? ride.startPoint.x : 60.6057)

  if (!modalMap) {
    modalMap = new ymaps.Map(mapContainer.value, {
      center: [startLat, startLng],
      zoom: 12,
      controls: ['zoomControl', 'fullscreenControl']
    })
  } else {
    modalMap.setCenter([startLat, startLng])
    modalMap.geoObjects.removeAll()
  }

  // Draw start marker
  const startMarker = new ymaps.Placemark([startLat, startLng], {
    balloonContentHeader: `<b>🟢 Старт: ${ride.title}</b>`,
    balloonContentBody: `Сбор участников здесь`
  }, {
    preset: 'islands#darkOrangeDotIcon'
  })
  modalMap.geoObjects.add(startMarker)

  // Check if we have track points
  let points: number[][] = []
  if (ride.trackPoints && ride.trackPoints.length > 0) {
    points = ride.trackPoints.map((pt: any) => [pt.lat, pt.lng])
  } else if (ride.gpxTrackPath && ride.gpxTrackPath.trim().startsWith('<')) {
    // Parse on client
    try {
      const parser = new DOMParser()
      const doc = parser.parseFromString(ride.gpxTrackPath, 'text/xml')
      const trkpts = doc.querySelectorAll('trkpt, wpt')
      trkpts.forEach(pt => {
        const lat = parseFloat(pt.getAttribute('lat') || '0')
        const lon = parseFloat(pt.getAttribute('lon') || '0')
        if (lat && lon) points.push([lat, lon])
      })
    } catch (e) {
      console.error('Error parsing GPX in modal', e)
    }
  }

  if (points.length > 0) {
    gpxPolyline = new ymaps.Polyline(points, {
      balloonContent: `Маршрут: ~${ride.totalDistanceKm || ''} км`
    }, {
      strokeColor: '#ff6b00',
      strokeWidth: 5,
      strokeOpacity: 0.95
    })
    modalMap.geoObjects.add(gpxPolyline)

    // Finish marker
    const finishCoord = points[points.length - 1]
    const finishMarker = new ymaps.Placemark(finishCoord, {
      balloonContentHeader: '<b>🏁 Финиш маршрута</b>'
    }, {
      preset: 'islands#redFlagIcon'
    })
    modalMap.geoObjects.add(finishMarker)

    // Fit map view to track
    modalMap.setBounds(gpxPolyline.geometry.getBounds(), {
      checkZoomRange: true,
      zoomMargin: 30
    })
  }
}

const handleGpxUpload = async (e: Event) => {
  const input = e.target as HTMLInputElement
  if (!input.files || !input.files[0]) return
  const file = input.files[0]

  uploadingGpx.value = true
  try {
    const formData = new FormData()
    formData.append('file', file)

    const rideId = props.ride.id || props.ride.targetId
    const res = await api.post(`/rides/${rideId}/gpx`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })

    toast.success(`GPX-трек успешно загружен! Длина: ${res.data.totalDistanceKm} км`)
    await fetchRideData()
  } catch (err) {
    console.error('Failed to upload GPX', err)
    toast.error('Не удалось загрузить GPX файл')
  } finally {
    uploadingGpx.value = false
    if (gpxInputRef.value) gpxInputRef.value.value = ''
  }
}

const joinRide = async () => {
  if (!authStore.isAuthenticated()) {
    toast.warning('Войдите, чтобы присоединиться к покатушке.')
    return
  }
  const rideId = props.ride.id || props.ride.targetId
  try {
    await api.post(`/rides/${rideId}/join`, {})
    isMember.value = true
    toast.success('Вы успешно присоединились к заезду!')
    emit('joined', rideId)
    await fetchRideData()
  } catch (e: any) {
    toast.error(e.response?.data?.Error || 'Ошибка при присоединении.')
  }
}

const updateStatus = async () => {
  const rideId = props.ride.id || props.ride.targetId
  savingStatus.value = true
  try {
    await api.put(`/rides/${rideId}/status`, {
      status: newStatus.value,
      report: newReport.value
    })
    toast.success('Статус заезда успешно обновлен!')
    await fetchRideData()
  } catch (e: any) {
    toast.error('Ошибка при обновлении статуса')
  } finally {
    savingStatus.value = false
  }
}

const submitReview = async () => {
  if (!newReviewText.value.trim()) return
  const rideId = props.ride.id || props.ride.targetId
  try {
    await api.post('/social/reviews', {
      targetId: rideId,
      text: newReviewText.value,
      rating: newReviewRating.value
    })
    newReviewText.value = ''
    toast.success('Отзыв опубликован!')
    await fetchRideData()
  } catch (e: any) {
    toast.error('Не удалось отправить отзыв')
  }
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return ''
  return new Date(dateStr).toLocaleString('ru-RU', {
    day: 'numeric',
    month: 'long',
    hour: '2-digit',
    minute: '2-digit',
    weekday: 'short'
  })
}

onMounted(() => {
  fetchRideData()
})

onUnmounted(() => {
  if (modalMap) {
    modalMap.destroy()
    modalMap = null
  }
})

watch(activeTab, async (val) => {
  if (val === 'info') {
    await nextTick()
    if (modalMap) {
      modalMap.container.fitToViewport()
    } else {
      initOrUpdateMap()
    }
  }
})
</script>

<template>
  <div class="modal-backdrop" @click.self="emit('close')">
    <div class="modal-content glass-panel">
      <!-- Modal Header -->
      <div class="modal-header">
        <div class="header-left">
          <span class="ride-category-pill">{{ (fullRide || ride).categoryName || 'Покатушка' }}</span>
          <h2>{{ (fullRide || ride).title }}</h2>
        </div>
        <button class="close-btn" @click="emit('close')">×</button>
      </div>

      <!-- Navigation Tabs -->
      <div class="modal-tabs">
        <button :class="{ active: activeTab === 'info' }" @click="activeTab = 'info'">
          🗺️ Маршрут и Инфо
        </button>
        <button :class="{ active: activeTab === 'members' }" @click="activeTab = 'members'">
          👥 Участники ({{ (fullRide?.members?.length || ride.membersCount || 1) }})
        </button>
        <button :class="{ active: activeTab === 'comments' }" @click="activeTab = 'comments'">
          💬 Комментарии
        </button>
        <button :class="{ active: activeTab === 'reviews' }" @click="activeTab = 'reviews'">
          ⭐ Отзывы ({{ reviews.length }})
        </button>
      </div>

      <div class="modal-body">
        <!-- TAB 1: Route & Info -->
        <div v-show="activeTab === 'info'" class="tab-pane">
          <!-- Interactive Map with GPX Track -->
          <div class="map-wrapper">
            <div ref="mapContainer" class="ride-map-container"></div>
            <div v-if="fullRide?.totalDistanceKm" class="distance-tag">
              🚩 Длина трека: <strong>{{ fullRide.totalDistanceKm }} км</strong>
            </div>
          </div>

          <!-- Organizer GPX Track Upload Box -->
          <div v-if="isOrganizer" class="organizer-gpx-box glass-panel">
            <div class="gpx-box-info">
              <strong>🛰️ GPX-трек маршрута</strong>
              <span v-if="fullRide?.totalDistanceKm" class="gpx-status">
                ✓ Загружен трек на {{ fullRide.totalDistanceKm }} км
              </span>
              <span v-else class="gpx-status empty">
                Маршрут не прикреплен. Загрузите .gpx файл трека.
              </span>
            </div>
            <div>
              <input
                ref="gpxInputRef"
                type="file"
                accept=".gpx"
                class="hidden-file"
                @change="handleGpxUpload"
              />
              <button
                type="button"
                class="btn-upload-gpx"
                :disabled="uploadingGpx"
                @click="gpxInputRef?.click()"
              >
                {{ uploadingGpx ? 'Загрузка...' : '📤 Загрузить .gpx' }}
              </button>
            </div>
          </div>

          <!-- Details Grid -->
          <div class="details-grid">
            <div class="detail-card glass-panel">
              <span class="detail-label">Дата и время сбора</span>
              <span class="detail-val">📅 {{ formatDate((fullRide || ride).eventDate) }}</span>
            </div>
            <div class="detail-card glass-panel">
              <span class="detail-label">Сложность прохвата</span>
              <span class="detail-val complexity-text">
                ⚡ {{ complexityLabels[(fullRide || ride).complexity] || (fullRide || ride).complexity }}
              </span>
            </div>
            <div class="detail-card glass-panel">
              <span class="detail-label">Организатор</span>
              <router-link
                v-if="(fullRide || ride).organizerUsername"
                :to="`/profile/${(fullRide || ride).organizerUsername}`"
                class="detail-val org-link"
              >
                👤 {{ (fullRide || ride).organizerName || `@${(fullRide || ride).organizerUsername}` }}
              </router-link>
              <span v-else class="detail-val">👤 Организатор</span>
            </div>
            <div class="detail-card glass-panel">
              <span class="detail-label">Город / Локация</span>
              <span class="detail-val">📍 {{ (fullRide || ride).cityName || 'По координатам' }}</span>
            </div>
          </div>

          <!-- Description -->
          <div class="description-block glass-panel">
            <h4>План заезда и требования:</h4>
            <p>{{ (fullRide || ride).description || 'Описание отсутствует.' }}</p>
          </div>

          <!-- Join CTA -->
          <div class="join-action-box" v-if="authStore.isAuthenticated() && !isOrganizer">
            <button
              v-if="!isMember"
              class="btn-join-primary"
              @click="joinRide"
            >
              🏍️ Присоединиться к покатушке
            </button>
            <div v-else class="joined-badge">
              ✅ Вы в списке участников этого заезда!
            </div>
          </div>

          <!-- Organizer Status Control -->
          <div v-if="isOrganizer" class="organizer-controls glass-panel">
            <h4>⚙️ Управление статусом заезда</h4>
            <div class="status-row">
              <select v-model="newStatus" class="input-select">
                <option :value="1">Запланирована</option>
                <option :value="2">Завершена успешно</option>
                <option :value="3">Отменена</option>
              </select>
              <button class="btn-save-status" :disabled="savingStatus" @click="updateStatus">
                {{ savingStatus ? 'Сохранение...' : 'Обновить статус' }}
              </button>
            </div>
            <div v-if="newStatus === 2 || newStatus === 3" class="mt-2">
              <textarea
                v-model="newReport"
                placeholder="Итоговый отчет о заезде или причина отмены..."
                class="input-textarea"
              ></textarea>
            </div>
          </div>
        </div>

        <!-- TAB 2: Members -->
        <div v-show="activeTab === 'members'" class="tab-pane">
          <div v-if="!fullRide?.members || fullRide.members.length === 0" class="empty-list">
            <p>Пока нет подтвержденных участников.</p>
          </div>
          <div v-else class="members-grid">
            <div
              v-for="member in fullRide.members"
              :key="member.userId"
              class="member-card glass-panel"
            >
              <img
                v-if="member.avatarUrl"
                :src="member.avatarUrl.startsWith('http') ? member.avatarUrl : '/s3' + member.avatarUrl"
                class="member-avatar"
              />
              <div v-else class="avatar-placeholder">
                {{ (member.name || member.username || 'R').charAt(0).toUpperCase() }}
              </div>
              <div class="member-info">
                <router-link :to="`/profile/${member.username}`" class="member-name">
                  {{ member.name || member.username }}
                </router-link>
                <span class="member-role">
                  {{ member.userId === (fullRide.organizerId || ride.organizerId) ? '👑 Организатор' : 'Райдер' }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 3: Comments -->
        <div v-show="activeTab === 'comments'" class="tab-pane">
          <CommentSection :targetType="3" :targetId="props.ride.id || props.ride.targetId" />
        </div>

        <!-- TAB 4: Reviews -->
        <div v-show="activeTab === 'reviews'" class="tab-pane">
          <div v-if="authStore.isAuthenticated()" class="add-review-box glass-panel">
            <h4>Оставить отзыв о покатушке</h4>
            <div class="rating-select">
              <span>Оценка:</span>
              <select v-model.number="newReviewRating" class="input-select-sm">
                <option :value="5">⭐⭐⭐⭐⭐ 5</option>
                <option :value="4">⭐⭐⭐⭐ 4</option>
                <option :value="3">⭐⭐⭐ 3</option>
                <option :value="2">⭐⭐ 2</option>
                <option :value="1">⭐ 1</option>
              </select>
            </div>
            <textarea
              v-model="newReviewText"
              placeholder="Как прошел заезд? Сложность трассы, организация..."
              class="input-textarea"
            ></textarea>
            <button class="btn-primary-sm" @click="submitReview">Отправить отзыв</button>
          </div>

          <div class="reviews-list">
            <div v-if="reviews.length === 0" class="empty-list">
              Отзывов пока нет. Будьте первым!
            </div>
            <div v-for="rev in reviews" :key="rev.id" class="review-card glass-panel">
              <div class="review-header">
                <strong>{{ rev.authorName || 'Райдер' }}</strong>
                <span class="rev-stars">⭐ {{ rev.rating }}/5</span>
              </div>
              <p class="rev-text">{{ rev.text }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.75);
  backdrop-filter: blur(8px);
  z-index: 1200;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.modal-content {
  width: 100%;
  max-width: 820px;
  max-height: 90vh;
  background: #161622;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 20px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.6);
  color: #fff;
}

.modal-header {
  padding: 20px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.header-left {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.ride-category-pill {
  font-size: 0.75rem;
  background: rgba(255, 107, 0, 0.2);
  color: #ff8c42;
  padding: 3px 10px;
  border-radius: 20px;
  width: fit-content;
  font-weight: 600;
}

.header-left h2 {
  margin: 0;
  font-size: 1.4rem;
  color: #fff;
}

.close-btn {
  background: transparent;
  border: none;
  font-size: 1.8rem;
  color: #888;
  cursor: pointer;
  transition: color 0.2s;
}

.close-btn:hover {
  color: #fff;
}

.modal-tabs {
  display: flex;
  background: rgba(0, 0, 0, 0.25);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  padding: 0 16px;
  gap: 6px;
}

.modal-tabs button {
  background: transparent;
  border: none;
  color: #999;
  padding: 12px 18px;
  font-size: 0.9rem;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: all 0.2s;
}

.modal-tabs button:hover {
  color: #ddd;
}

.modal-tabs button.active {
  color: #ff8c42;
  border-bottom-color: #ff8c42;
  font-weight: 600;
}

.modal-body {
  padding: 22px 24px;
  overflow-y: auto;
  flex: 1;
}

.tab-pane {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.map-wrapper {
  position: relative;
  width: 100%;
  height: 280px;
  border-radius: 14px;
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.ride-map-container {
  width: 100%;
  height: 100%;
}

.distance-tag {
  position: absolute;
  bottom: 12px;
  right: 12px;
  background: rgba(0, 0, 0, 0.85);
  backdrop-filter: blur(6px);
  padding: 6px 14px;
  border-radius: 20px;
  font-size: 0.85rem;
  color: #ff8c42;
  border: 1px solid rgba(255, 107, 0, 0.4);
  z-index: 100;
}

.organizer-gpx-box {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 18px;
  border-radius: 12px;
  background: rgba(255, 107, 0, 0.08);
  border: 1px solid rgba(255, 107, 0, 0.25);
}

.gpx-box-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.gpx-status {
  font-size: 0.82rem;
  color: #4caf50;
}

.gpx-status.empty {
  color: #bbb;
}

.hidden-file {
  display: none;
}

.btn-upload-gpx {
  padding: 8px 16px;
  background: #ff6b00;
  border: none;
  border-radius: 8px;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
  font-size: 0.85rem;
}

.btn-upload-gpx:hover {
  background: #ff8c42;
}

.details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
}

.detail-card {
  padding: 12px 14px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.03);
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.detail-label {
  font-size: 0.78rem;
  color: #888;
}

.detail-val {
  font-size: 0.95rem;
  font-weight: 600;
  color: #fff;
}

.org-link {
  color: #ff8c42;
  text-decoration: none;
}

.org-link:hover {
  text-decoration: underline;
}

.description-block {
  padding: 16px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.03);
}

.description-block h4 {
  margin: 0 0 8px;
  font-size: 0.95rem;
  color: #bbb;
}

.description-block p {
  margin: 0;
  font-size: 0.92rem;
  line-height: 1.5;
  color: #eee;
}

.join-action-box {
  display: flex;
  justify-content: center;
  margin: 6px 0;
}

.btn-join-primary {
  width: 100%;
  padding: 14px;
  background: linear-gradient(135deg, #ff6b00 0%, #ff8c42 100%);
  border: none;
  border-radius: 12px;
  color: #fff;
  font-size: 1.1rem;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 4px 16px rgba(255, 107, 0, 0.35);
  transition: all 0.2s;
}

.btn-join-primary:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(255, 107, 0, 0.45);
}

.joined-badge {
  width: 100%;
  text-align: center;
  padding: 12px;
  background: rgba(76, 175, 80, 0.15);
  border: 1px solid rgba(76, 175, 80, 0.35);
  color: #4caf50;
  border-radius: 12px;
  font-weight: 600;
}

.organizer-controls {
  padding: 16px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.03);
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.organizer-controls h4 {
  margin: 0;
  font-size: 0.95rem;
  color: #ddd;
}

.status-row {
  display: flex;
  gap: 12px;
}

.input-select {
  flex: 1;
  padding: 10px;
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 8px;
  color: #fff;
}

.input-textarea {
  width: 100%;
  min-height: 80px;
  padding: 10px;
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 8px;
  color: #fff;
  font-size: 0.88rem;
  resize: vertical;
}

.btn-save-status {
  padding: 8px 18px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 8px;
  color: #fff;
  cursor: pointer;
}

.members-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 12px;
}

.member-card {
  padding: 12px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.03);
  display: flex;
  align-items: center;
  gap: 12px;
}

.member-avatar {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  object-fit: cover;
}

.avatar-placeholder {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: rgba(255, 107, 0, 0.2);
  color: #ff8c42;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.1rem;
}

.member-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.member-name {
  color: #fff;
  font-weight: 600;
  text-decoration: none;
  font-size: 0.92rem;
}

.member-name:hover {
  color: #ff8c42;
}

.member-role {
  font-size: 0.78rem;
  color: #888;
}

.add-review-box {
  padding: 16px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.03);
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.rating-select {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 0.9rem;
}

.input-select-sm {
  padding: 6px 10px;
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 6px;
  color: #fff;
}

.btn-primary-sm {
  align-self: flex-start;
  padding: 8px 18px;
  background: #ff6b00;
  border: none;
  border-radius: 8px;
  color: #fff;
  cursor: pointer;
}

.reviews-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.review-card {
  padding: 14px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.03);
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.review-header {
  display: flex;
  justify-content: space-between;
  font-size: 0.9rem;
}

.rev-stars {
  color: #ffc107;
}

.rev-text {
  margin: 0;
  font-size: 0.88rem;
  color: #ccc;
}

.empty-list {
  text-align: center;
  padding: 30px 0;
  color: #777;
  font-size: 0.95rem;
}
</style>
