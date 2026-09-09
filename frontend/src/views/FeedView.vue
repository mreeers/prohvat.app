<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useToast } from 'vue-toastification'
import api from '../services/api'
import RideDetailModal from '../components/RideDetailModal.vue'

const authStore = useAuthStore()
const toast = useToast()

const selectedRide = ref<any | null>(null)

// Feed state
const feedItems = ref<any[]>([])
const loading = ref(false)
const activeTab = ref<'all' | 'friends'>('all')
const selectedSeason = ref<number | null>(null) // null = all, 1 = summer, 2 = winter

// Expanded comments dictionary: { [targetId: string]: { loading: boolean, list: any[], newText: string } }
const commentsState = ref<Record<string, { loading: boolean, list: any[], newText: string }>>({})

// Image modal preview
const previewImage = ref<string | null>(null)

// Quick Post State
const showPostForm = ref(false)
const userVehicles = ref<any[]>([])
const newPost = ref({
  vehicleId: '',
  title: '',
  content: '',
  metricsValue: 0,
  imageUrl: '',
  videoUrl: '',
  submitting: false
})

const fetchUserVehicles = async () => {
  if (!authStore.isAuthenticated()) return
  try {
    const res = await api.get('/vehicles/my')
    userVehicles.value = res.data || []
    if (userVehicles.value.length > 0 && !newPost.value.vehicleId) {
      newPost.value.vehicleId = userVehicles.value[0].id
    }
  } catch (err) {
    console.error('Failed to fetch user vehicles', err)
  }
}

const fetchFeed = async () => {
  loading.value = true
  try {
    let url = `/feed?onlyFriends=${activeTab.value === 'friends'}`
    if (selectedSeason.value !== null) {
      url += `&season=${selectedSeason.value}`
    }
    const res = await api.get(url)
    feedItems.value = res.data || []
  } catch (err) {
    console.error('Failed to fetch feed', err)
    toast.error('Не удалось загрузить ленту новостей')
  } finally {
    loading.value = false
  }
}

const toggleLike = async (item: any) => {
  if (!authStore.isAuthenticated()) {
    toast.warning('Войдите в аккаунт, чтобы ставить лайки')
    return
  }

  const prevLiked = item.isLikedByCurrentUser
  const prevCount = item.likesCount

  // Optimistic update
  item.isLikedByCurrentUser = !prevLiked
  item.likesCount += prevLiked ? -1 : 1

  try {
    const res = await api.post('/interactions/likes/toggle', {
      targetId: item.targetId,
      targetType: item.targetType
    })
    item.isLikedByCurrentUser = res.data.isLiked
  } catch (err) {
    // Revert on error
    item.isLikedByCurrentUser = prevLiked
    item.likesCount = prevCount
    toast.error('Ошибка при переключении лайка')
  }
}

const toggleComments = async (item: any) => {
  const id = item.targetId
  if (!commentsState.value[id]) {
    commentsState.value[id] = { loading: true, list: [], newText: '' }
    await loadComments(item)
  } else {
    // Toggle visibility by resetting or caching
    if (commentsState.value[id].list.length === 0 && !commentsState.value[id].loading) {
      await loadComments(item)
    } else {
      delete commentsState.value[id]
    }
  }
}

const loadComments = async (item: any) => {
  const id = item.targetId
  commentsState.value[id].loading = true
  try {
    const res = await api.get(`/interactions/comments/${item.targetType}/${item.targetId}`)
    commentsState.value[id].list = res.data || []
  } catch (err) {
    console.error('Failed to load comments', err)
    toast.error('Не удалось загрузить комментарии')
  } finally {
    commentsState.value[id].loading = false
  }
}

const addComment = async (item: any) => {
  if (!authStore.isAuthenticated()) {
    toast.warning('Войдите в аккаунт, чтобы оставить комментарий')
    return
  }
  const id = item.targetId
  const state = commentsState.value[id]
  if (!state || !state.newText.trim()) return

  const commentText = state.newText.trim()
  try {
    const res = await api.post('/interactions/comments', {
      targetId: item.targetId,
      targetType: item.targetType,
      text: commentText
    })

    // Append newly created comment
    state.list.push({
      id: res.data.id,
      text: commentText,
      createdAt: new Date().toISOString(),
      userName: authStore.user?.username || 'Вы',
      userAvatarUrl: ''
    })
    state.newText = ''
    item.commentsCount = (item.commentsCount || 0) + 1
    toast.success('Комментарий добавлен')
  } catch (err) {
    console.error('Failed to add comment', err)
    toast.error('Ошибка при добавлении комментария')
  }
}

const joinRide = async (item: any) => {
  if (!authStore.isAuthenticated()) {
    toast.warning('Войдите в аккаунт, чтобы присоединиться к заезду')
    return
  }

  try {
    await api.post(`/rides/${item.targetId}/join`, {})
    item.isJoined = true
    item.membersCount = (item.membersCount || 0) + 1
    toast.success('Вы успешно присоединились к покатушке!')
  } catch (err: any) {
    toast.error(err.response?.data?.Error || 'Ошибка при присоединении')
  }
}

const submitNewPost = async () => {
  if (!newPost.value.vehicleId) {
    toast.warning('Выберите технику из вашего гаража')
    return
  }
  if (!newPost.value.title.trim()) {
    toast.warning('Укажите заголовок записи')
    return
  }

  newPost.value.submitting = true
  try {
    const formData = new FormData()
    formData.append('Title', newPost.value.title)
    formData.append('Content', newPost.value.content)
    formData.append('MetricsValue', newPost.value.metricsValue.toString())
    if (newPost.value.imageUrl.trim()) {
      formData.append('ImageUrls', newPost.value.imageUrl.trim())
    }

    await api.post(`/vehicles/${newPost.value.vehicleId}/logs`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })

    toast.success('Запись успешно опубликована!')
    newPost.value.title = ''
    newPost.value.content = ''
    newPost.value.imageUrl = ''
    newPost.value.videoUrl = ''
    showPostForm.value = false

    // Refresh feed
    await fetchFeed()
  } catch (err) {
    console.error('Failed to publish log', err)
    toast.error('Не удалось опубликовать запись в бортжурнал')
  } finally {
    newPost.value.submitting = false
  }
}

const formatRelativeTime = (dateStr: string) => {
  const date = new Date(dateStr)
  const now = new Date()
  const diffSec = Math.floor((now.getTime() - date.getTime()) / 1000)

  if (diffSec < 60) return 'только что'
  if (diffSec < 3600) return `${Math.floor(diffSec / 60)} мин. назад`
  if (diffSec < 86400) return `${Math.floor(diffSec / 3600)} ч. назад`
  if (diffSec < 172800) return `вчера в ${date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}`

  return date.toLocaleDateString('ru-RU', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const formatEventDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString('ru-RU', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const getEmbedUrl = (url: string) => {
  if (url.includes('youtube.com/watch?v=')) {
    return url.replace('watch?v=', 'embed/')
  }
  if (url.includes('youtu.be/')) {
    const id = url.split('youtu.be/')[1]
    return `https://www.youtube.com/embed/${id}`
  }
  return url
}

onMounted(() => {
  fetchFeed()
  fetchUserVehicles()
})

watch(activeTab, () => {
  fetchFeed()
})

watch(selectedSeason, () => {
  fetchFeed()
})
</script>

<template>
  <div class="feed-view-page">
    <!-- Top Control Bar (VK style tabs) -->
    <div class="feed-nav-bar glass-panel">
      <div class="feed-tabs">
        <button 
          class="feed-tab" 
          :class="{ active: activeTab === 'all' }" 
          @click="activeTab = 'all'"
        >
          📰 Все записи
        </button>
        <button 
          class="feed-tab" 
          :class="{ active: activeTab === 'friends' }" 
          @click="activeTab = 'friends'"
        >
          👥 Новости друзей
        </button>
      </div>

      <!-- Season Filters (PRD Requirement) -->
      <div class="season-filters">
        <button 
          class="season-btn" 
          :class="{ active: selectedSeason === null }" 
          @click="selectedSeason = null"
        >
          Все сезоны
        </button>
        <button 
          class="season-btn summer" 
          :class="{ active: selectedSeason === 1 }" 
          @click="selectedSeason = 1"
        >
          ☀️ Лето (Эндуро)
        </button>
        <button 
          class="season-btn winter" 
          :class="{ active: selectedSeason === 2 }" 
          @click="selectedSeason = 2"
        >
          ❄️ Зима (Дрифт / Снегоходы)
        </button>
      </div>
    </div>

    <!-- Quick Post Box (VK style "Что у вас нового?") -->
    <div v-if="authStore.isAuthenticated()" class="quick-post-card glass-panel">
      <div class="quick-post-header" @click="showPostForm = !showPostForm">
        <div class="user-avatar-sm">
          <span class="avatar-placeholder">🏍️</span>
        </div>
        <div class="fake-input">
          {{ showPostForm ? 'Новая запись в бортжурнал техники' : 'Что нового в гараже или на треке? Поделиться...' }}
        </div>
        <button class="expand-btn">
          {{ showPostForm ? '✕' : '✏️' }}
        </button>
      </div>

      <!-- Expanded Form -->
      <div v-if="showPostForm" class="quick-post-form">
        <div v-if="userVehicles.length === 0" class="no-vehicle-warning">
          У вас пока нет техники в гараже. 
          <router-link to="/garage">Добавьте технику</router-link>, чтобы вести бортжурнал!
        </div>
        <div v-else class="form-fields">
          <div class="form-group">
            <label>Выберите технику:</label>
            <select v-model="newPost.vehicleId" class="input-select">
              <option v-for="v in userVehicles" :key="v.id" :value="v.id">
                {{ v.brand }} {{ v.model }} ({{ v.year }})
              </option>
            </select>
          </div>

          <div class="form-group">
            <label>Заголовок записи:</label>
            <input 
              v-model="newPost.title" 
              type="text" 
              placeholder="Например: Замена масла и проверка зазоров клапанов" 
              class="input-text"
            />
          </div>

          <div class="form-group">
            <label>Текущий пробег / Моточасы:</label>
            <input 
              v-model.number="newPost.metricsValue" 
              type="number" 
              placeholder="Значение на одометре" 
              class="input-text"
            />
          </div>

          <div class="form-group">
            <label>Текст записи:</label>
            <textarea 
              v-model="newPost.content" 
              rows="3" 
              placeholder="Опишите процесс, впечатления от покатушки или установленные детали..." 
              class="input-textarea"
            ></textarea>
          </div>

          <div class="form-group">
            <label>Ссылка на фото (URL):</label>
            <input 
              v-model="newPost.imageUrl" 
              type="url" 
              placeholder="https://..." 
              class="input-text"
            />
          </div>

          <div class="form-actions">
            <button class="btn-cancel" @click="showPostForm = false">Отмена</button>
            <button 
              class="btn-submit" 
              :disabled="newPost.submitting" 
              @click="submitNewPost"
            >
              {{ newPost.submitting ? 'Публикация...' : 'Опубликовать запись' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Feed Content -->
    <div v-if="loading" class="loading-state">
      <div class="loader-spinner"></div>
      <p>Загрузка ленты активности...</p>
    </div>

    <div v-else-if="feedItems.length === 0" class="empty-feed glass-panel">
      <div class="empty-icon">🏁</div>
      <h3>В ленте пока пусто</h3>
      <p v-if="activeTab === 'friends'">
        У ваших друзей пока нет опубликованных записей. Добавьте больше райдеров в друзья в разделе 
        <router-link to="/friends" class="link">«Мои друзья»</router-link> или переключитесь на вкладку «Все записи».
      </p>
      <p v-else>
        Пока нет публикаций по выбранному сезону. Будьте первым, кто опубликует отчет о покатушке!
      </p>
    </div>

    <!-- Feed List -->
    <div v-else class="feed-posts">
      <article 
        v-for="item in feedItems" 
        :key="item.id" 
        class="feed-card glass-panel"
      >
        <!-- Card Author Header -->
        <div class="card-author-row">
          <div class="author-left">
            <img 
              v-if="item.authorAvatarUrl" 
              :src="item.authorAvatarUrl" 
              :alt="item.authorName" 
              class="author-avatar"
            />
            <div v-else class="author-avatar avatar-fallback">
              {{ item.authorName ? item.authorName.charAt(0) : 'R' }}
            </div>

            <div class="author-meta">
              <div class="author-name-line">
                <router-link 
                  v-if="item.authorUsername" 
                  :to="'/profile/' + item.authorUsername" 
                  class="author-name"
                >
                  {{ item.authorName }}
                </router-link>
                <span v-else class="author-name">{{ item.authorName }}</span>

                <span v-if="item.authorUsername" class="author-tag">
                  @{{ item.authorUsername }}
                </span>
              </div>
              <span class="post-time">{{ formatRelativeTime(item.createdAt) }}</span>
            </div>
          </div>

          <!-- Vehicle / Type Badge -->
          <div class="post-type-badge">
            <span v-if="item.type === 'vehicle_log'" class="vehicle-badge">
              <span class="badge-icon">
                {{ item.season === 2 ? '❄️' : '🏍️' }}
              </span>
              {{ item.vehicleBrand }} {{ item.vehicleModel }}
              <span v-if="item.metricsValue" class="metrics-pill">
                • {{ item.metricsValue }} {{ item.metricUnit }}
              </span>
            </span>
            <span v-else class="ride-badge">
              📍 Покатушка <span v-if="item.cityName">• {{ item.cityName }}</span>
            </span>
          </div>
        </div>

        <!-- Post Content -->
        <div class="card-content">
          <h3 class="post-title">{{ item.title }}</h3>
          <p class="post-text">{{ item.content }}</p>

          <!-- Part Reviews (PRD 2.0 Tuning Tags) -->
          <div v-if="item.partReviews && item.partReviews.length > 0" class="tuning-parts-block">
            <span class="parts-label">🔧 Установленный тюнинг:</span>
            <div class="parts-chips">
              <span 
                v-for="pr in item.partReviews" 
                :key="pr.id" 
                class="part-chip"
              >
                <strong>{{ pr.partName }}</strong>
                <small v-if="pr.vendorCode"> (арт. {{ pr.vendorCode }})</small>
                <a 
                  v-if="pr.marketplaceLink" 
                  :href="pr.marketplaceLink" 
                  target="_blank" 
                  class="part-link"
                >
                  🔗 Магазин
                </a>
              </span>
            </div>
          </div>

          <!-- Image Gallery -->
          <div 
            v-if="item.imageUrls && item.imageUrls.length > 0" 
            class="media-gallery" 
            :class="'count-' + Math.min(item.imageUrls.length, 3)"
          >
            <div 
              v-for="(img, idx) in item.imageUrls" 
              :key="idx" 
              class="gallery-item"
              @click="previewImage = img"
            >
              <img :src="img" :alt="item.title" loading="lazy" />
            </div>
          </div>

          <!-- Video Embed (PRD 2.0 Videourls) -->
          <div v-if="item.videoUrls && item.videoUrls.length > 0" class="video-embed-container">
            <iframe 
              v-for="(vid, vIdx) in item.videoUrls" 
              :key="vIdx" 
              :src="getEmbedUrl(vid)" 
              frameborder="0" 
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" 
              allowfullscreen
              class="video-frame"
            ></iframe>
          </div>

          <!-- Ride Specific Block -->
          <div v-if="item.type === 'ride'" class="ride-details-box">
            <div class="ride-info-grid">
              <div class="info-item">
                <span class="info-label">Дата сбора:</span>
                <span class="info-value">{{ formatEventDate(item.eventDate) }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Сложность:</span>
                <span class="info-value difficulty-tag" :class="item.complexity?.toLowerCase()">
                  {{ item.complexity }}
                </span>
              </div>
              <div class="info-item">
                <span class="info-label">Участники:</span>
                <span class="info-value">{{ item.membersCount || 1 }} / {{ item.maxMembers || '∞' }}</span>
              </div>
            </div>

            <div class="ride-action">
              <button 
                type="button"
                class="btn-view-route" 
                @click="selectedRide = item"
              >
                🗺️ Маршрут и GPX-трек
              </button>
              <button 
                v-if="authStore.isAuthenticated() && authStore.user?.userId !== item.authorId && !item.isJoined" 
                class="btn-join-ride" 
                @click="joinRide(item)"
              >
                🏍️ Присоединиться
              </button>
              <span v-else-if="item.isJoined" class="already-joined-badge">
                ✅ Вы участник
              </span>
            </div>
          </div>
        </div>

        <!-- Post Action Bar (VK style: Likes & Comments) -->
        <div class="card-footer-actions">
          <button 
            class="action-btn like-btn" 
            :class="{ active: item.isLikedByCurrentUser }" 
            @click="toggleLike(item)"
          >
            <span class="btn-icon">{{ item.isLikedByCurrentUser ? '❤️' : '🤍' }}</span>
            <span class="btn-count">{{ item.likesCount || 0 }}</span>
          </button>

          <button 
            class="action-btn comment-btn" 
            :class="{ active: commentsState[item.targetId] }"
            @click="toggleComments(item)"
          >
            <span class="btn-icon">💬</span>
            <span class="btn-count">{{ item.commentsCount || 0 }}</span>
          </button>
        </div>

        <!-- Expandable Comments Block -->
        <div v-if="commentsState[item.targetId]" class="comments-section">
          <div v-if="commentsState[item.targetId].loading" class="comments-loading">
            Загрузка комментариев...
          </div>

          <div v-else class="comments-list">
            <div 
              v-if="commentsState[item.targetId].list.length === 0" 
              class="no-comments"
            >
              Комментариев пока нет. Будьте первым!
            </div>

            <div 
              v-for="c in commentsState[item.targetId].list" 
              :key="c.id" 
              class="comment-row"
            >
              <div class="comment-avatar">
                <img v-if="c.userAvatarUrl" :src="c.userAvatarUrl" />
                <span v-else>{{ (c.userName || 'U').charAt(0) }}</span>
              </div>
              <div class="comment-bubble">
                <div class="comment-header">
                  <span class="comment-author">{{ c.userName || 'Райдер' }}</span>
                  <span class="comment-date">{{ formatRelativeTime(c.createdAt) }}</span>
                </div>
                <p class="comment-text">{{ c.text }}</p>
              </div>
            </div>

            <!-- Add Comment Form -->
            <div v-if="authStore.isAuthenticated()" class="add-comment-row">
              <input 
                v-model="commentsState[item.targetId].newText" 
                type="text" 
                placeholder="Написать комментарий..." 
                class="comment-input"
                @keyup.enter="addComment(item)"
              />
              <button 
                class="btn-send-comment" 
                :disabled="!commentsState[item.targetId].newText.trim()"
                @click="addComment(item)"
              >
                Отправить
              </button>
            </div>
            <div v-else class="login-prompt">
              <router-link to="/login" class="link">Войдите</router-link>, чтобы оставить комментарий.
            </div>
          </div>
        </div>
      </article>
    </div>

    <!-- Image Lightbox Modal -->
    <div v-if="previewImage" class="lightbox-overlay" @click="previewImage = null">
      <div class="lightbox-modal">
        <img :src="previewImage" alt="Preview" />
        <button class="lightbox-close" @click="previewImage = null">✕</button>
      </div>
    </div>

    <!-- Ride Detail & GPX Modal -->
    <RideDetailModal
      v-if="selectedRide"
      :ride="selectedRide"
      @close="selectedRide = null"
      @joined="() => { if (selectedRide) selectedRide.isJoined = true }"
    />
  </div>
</template>

<style scoped>
.btn-view-route {
  padding: 8px 14px;
  background: rgba(255, 107, 0, 0.15);
  border: 1px solid rgba(255, 107, 0, 0.4);
  color: #ff8c42;
  border-radius: 8px;
  font-weight: 600;
  font-size: 0.88rem;
  cursor: pointer;
  transition: all 0.2s ease;
}
.btn-view-route:hover {
  background: rgba(255, 107, 0, 0.3);
  color: #fff;
}

.feed-view-page {
  max-width: 820px;
  margin: 0 auto;
  padding: 16px 12px 60px;
  color: var(--text-light, #e0e0e0);
}

/* Glass Panel Styling */
.glass-panel {
  background: rgba(26, 26, 36, 0.75);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.35);
}

/* Top Navigation Bar */
.feed-nav-bar {
  padding: 12px 16px;
  margin-bottom: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.feed-tabs {
  display: flex;
  gap: 8px;
}

.feed-tab {
  background: transparent;
  border: none;
  color: rgba(255, 255, 255, 0.65);
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.feed-tab:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #fff;
}

.feed-tab.active {
  background: rgba(255, 77, 0, 0.15);
  color: #ff5722;
  border: 1px solid rgba(255, 77, 0, 0.3);
}

.season-filters {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.season-btn {
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.6);
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 0.82rem;
  cursor: pointer;
  transition: all 0.2s;
}

.season-btn:hover {
  background: rgba(255, 255, 255, 0.1);
  color: #fff;
}

.season-btn.active {
  background: rgba(255, 255, 255, 0.15);
  color: #fff;
  border-color: rgba(255, 255, 255, 0.3);
}

.season-btn.summer.active {
  background: rgba(255, 152, 0, 0.2);
  color: #ff9800;
  border-color: #ff9800;
}

.season-btn.winter.active {
  background: rgba(33, 150, 243, 0.2);
  color: #29b6f6;
  border-color: #29b6f6;
}

/* Quick Post Widget */
.quick-post-card {
  padding: 14px 16px;
  margin-bottom: 20px;
}

.quick-post-header {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
}

.user-avatar-sm {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: rgba(255, 77, 0, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.fake-input {
  flex: 1;
  background: rgba(0, 0, 0, 0.3);
  padding: 10px 14px;
  border-radius: 20px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.5);
  font-size: 0.9rem;
}

.expand-btn {
  background: transparent;
  border: none;
  font-size: 1.1rem;
  color: rgba(255, 255, 255, 0.5);
  cursor: pointer;
}

.quick-post-form {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.form-fields {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-group label {
  font-size: 0.85rem;
  color: rgba(255, 255, 255, 0.7);
}

.input-text, .input-select, .input-textarea {
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 8px;
  padding: 9px 12px;
  color: #fff;
  font-size: 0.9rem;
  outline: none;
}

.input-text:focus, .input-select:focus, .input-textarea:focus {
  border-color: #ff5722;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 6px;
}

.btn-cancel {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: rgba(255, 255, 255, 0.7);
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
}

.btn-submit {
  background: #ff5722;
  border: none;
  color: #fff;
  padding: 8px 20px;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s;
}

.btn-submit:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Feed Posts */
.feed-posts {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.feed-card {
  padding: 16px;
  border-radius: 12px;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.feed-card:hover {
  border-color: rgba(255, 255, 255, 0.12);
}

.card-author-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
  margin-bottom: 14px;
}

.author-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.author-avatar {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  object-fit: cover;
  border: 1px solid rgba(255, 255, 255, 0.15);
}

.avatar-fallback {
  background: #ff5722;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 1.1rem;
}

.author-meta {
  display: flex;
  flex-direction: column;
}

.author-name-line {
  display: flex;
  align-items: center;
  gap: 6px;
}

.author-name {
  color: #fff;
  font-weight: 600;
  font-size: 0.98rem;
  text-decoration: none;
}

.author-name:hover {
  color: #ff7043;
}

.author-tag {
  color: rgba(255, 255, 255, 0.4);
  font-size: 0.82rem;
}

.post-time {
  font-size: 0.78rem;
  color: rgba(255, 255, 255, 0.45);
}

.post-type-badge {
  font-size: 0.82rem;
}

.vehicle-badge {
  background: rgba(255, 255, 255, 0.07);
  padding: 4px 10px;
  border-radius: 16px;
  color: #ffa726;
  font-weight: 500;
  border: 1px solid rgba(255, 167, 38, 0.2);
}

.ride-badge {
  background: rgba(0, 200, 83, 0.1);
  padding: 4px 10px;
  border-radius: 16px;
  color: #00e676;
  font-weight: 500;
  border: 1px solid rgba(0, 230, 118, 0.2);
}

.metrics-pill {
  color: rgba(255, 255, 255, 0.6);
  font-size: 0.8rem;
}

/* Content */
.post-title {
  margin: 0 0 8px 0;
  font-size: 1.15rem;
  font-weight: 700;
  color: #ffffff;
  line-height: 1.35;
}

.post-text {
  margin: 0 0 14px 0;
  font-size: 0.94rem;
  line-height: 1.55;
  color: rgba(255, 255, 255, 0.85);
  white-space: pre-line;
}

/* Tuning parts (PRD 2.0) */
.tuning-parts-block {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  padding: 8px 12px;
  margin-bottom: 12px;
}

.parts-label {
  font-size: 0.8rem;
  color: rgba(255, 255, 255, 0.5);
  display: block;
  margin-bottom: 6px;
}

.parts-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.part-chip {
  background: rgba(255, 87, 34, 0.12);
  border: 1px solid rgba(255, 87, 34, 0.25);
  color: #ff8a65;
  padding: 3px 8px;
  border-radius: 4px;
  font-size: 0.82rem;
}

.part-link {
  color: #64b5f6;
  text-decoration: none;
  margin-left: 4px;
}

.part-link:hover {
  text-decoration: underline;
}

/* Media Gallery */
.media-gallery {
  display: grid;
  gap: 6px;
  border-radius: 10px;
  overflow: hidden;
  margin-bottom: 14px;
}

.media-gallery.count-1 {
  grid-template-columns: 1fr;
}

.media-gallery.count-2 {
  grid-template-columns: 1fr 1fr;
}

.media-gallery.count-3 {
  grid-template-columns: 2fr 1fr;
}

.gallery-item {
  cursor: pointer;
  max-height: 400px;
  overflow: hidden;
  border-radius: 6px;
}

.gallery-item img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s ease;
}

.gallery-item:hover img {
  transform: scale(1.03);
}

/* Video Embeds */
.video-embed-container {
  margin-bottom: 14px;
  border-radius: 10px;
  overflow: hidden;
  background: #000;
}

.video-frame {
  width: 100%;
  height: 360px;
}

/* Ride Details */
.ride-details-box {
  background: rgba(0, 0, 0, 0.3);
  border: 1px solid rgba(0, 230, 118, 0.2);
  border-radius: 8px;
  padding: 12px 16px;
  margin-bottom: 14px;
}

.ride-info-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 10px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.info-label {
  font-size: 0.75rem;
  color: rgba(255, 255, 255, 0.5);
}

.info-value {
  font-size: 0.88rem;
  font-weight: 600;
  color: #fff;
}

.difficulty-tag.hard { color: #ff5252; }
.difficulty-tag.middle { color: #ffa726; }
.difficulty-tag.light { color: #69f0ae; }

.btn-join-ride {
  background: #00c853;
  color: #000;
  font-weight: 700;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
  transition: opacity 0.2s;
}

.btn-join-ride:hover {
  opacity: 0.9;
}

.already-joined-badge {
  color: #00e676;
  font-weight: 600;
  font-size: 0.88rem;
}

/* Actions Footer */
.card-footer-actions {
  display: flex;
  gap: 12px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  padding-top: 10px;
}

.action-btn {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 20px;
  padding: 6px 14px;
  color: rgba(255, 255, 255, 0.7);
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  font-size: 0.88rem;
  transition: all 0.2s;
}

.action-btn:hover {
  background: rgba(255, 255, 255, 0.1);
  color: #fff;
}

.action-btn.like-btn.active {
  background: rgba(255, 82, 82, 0.15);
  border-color: rgba(255, 82, 82, 0.3);
  color: #ff5252;
}

.action-btn.comment-btn.active {
  background: rgba(255, 255, 255, 0.15);
  color: #fff;
}

/* Comments Section */
.comments-section {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}

.comments-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.comment-row {
  display: flex;
  gap: 10px;
  align-items: flex-start;
}

.comment-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.85rem;
  font-weight: bold;
  flex-shrink: 0;
  overflow: hidden;
}

.comment-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.comment-bubble {
  background: rgba(0, 0, 0, 0.25);
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 10px;
  padding: 8px 12px;
  flex: 1;
}

.comment-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 3px;
}

.comment-author {
  font-weight: 600;
  font-size: 0.84rem;
  color: #ff7043;
}

.comment-date {
  font-size: 0.72rem;
  color: rgba(255, 255, 255, 0.4);
}

.comment-text {
  margin: 0;
  font-size: 0.88rem;
  color: rgba(255, 255, 255, 0.9);
  line-height: 1.4;
}

.add-comment-row {
  display: flex;
  gap: 8px;
  margin-top: 8px;
}

.comment-input {
  flex: 1;
  background: rgba(0, 0, 0, 0.35);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 20px;
  padding: 8px 14px;
  color: #fff;
  font-size: 0.88rem;
  outline: none;
}

.comment-input:focus {
  border-color: #ff5722;
}

.btn-send-comment {
  background: #ff5722;
  border: none;
  color: #fff;
  border-radius: 20px;
  padding: 8px 16px;
  font-size: 0.84rem;
  font-weight: 600;
  cursor: pointer;
}

.btn-send-comment:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.no-comments, .comments-loading, .login-prompt {
  text-align: center;
  font-size: 0.85rem;
  color: rgba(255, 255, 255, 0.45);
  padding: 8px 0;
}

/* Lightbox Modal */
.lightbox-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.85);
  backdrop-filter: blur(8px);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.lightbox-modal {
  position: relative;
  max-width: 90vw;
  max-height: 90vh;
}

.lightbox-modal img {
  max-width: 100%;
  max-height: 90vh;
  border-radius: 8px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.8);
}

.lightbox-close {
  position: absolute;
  top: -40px;
  right: -10px;
  background: transparent;
  border: none;
  color: #fff;
  font-size: 2rem;
  cursor: pointer;
}

/* Empty & Loading */
.loading-state, .empty-feed {
  text-align: center;
  padding: 50px 20px;
}

.empty-icon {
  font-size: 2.5rem;
  margin-bottom: 12px;
}

.loader-spinner {
  width: 36px;
  height: 36px;
  border: 3px solid rgba(255, 255, 255, 0.1);
  border-top-color: #ff5722;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 12px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.link {
  color: #ff7043;
  text-decoration: underline;
}
</style>
