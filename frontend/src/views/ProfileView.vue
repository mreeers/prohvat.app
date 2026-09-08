<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import { useToast } from "vue-toastification"

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const toast = useToast()
const usernameParam = route.params.username as string

const profile = ref<any>(null)
const isOwner = ref(false)
const loading = ref(true)

const isEditing = ref(false)
const editUsername = ref('')
const editBio = ref('')
const editIsVisibleOnMap = ref(false)
const selectedAvatarFile = ref<File | null>(null)
const saveError = ref('')
const saveSuccess = ref(false)

const cities = ref<any[]>([])
const searchCityTerm = ref('')
const selectedCityId = ref<string | null>(null)
const citySearchTimeout = ref<any>(null)

const activeTab = ref<'rides' | 'garage' | 'friends'>('rides')
const friends = ref<any[]>([])

const complexityLabels: Record<number, string> = { 1: 'Лёгкая', 2: 'Средняя', 3: 'Жёсткая', 4: 'Экстрим', 5: 'Дикий Запад' }
const complexityColors: Record<number, string> = { 1: '#4caf50', 2: '#8bc34a', 3: '#ff9800', 4: '#ff5722', 5: '#f44336' }

const searchCities = () => {
  clearTimeout(citySearchTimeout.value)
  if (searchCityTerm.value.length < 2) { cities.value = []; return }
  citySearchTimeout.value = setTimeout(async () => {
    const res = await api.get(`/location/cities?search=${searchCityTerm.value}`)
    cities.value = res.data
  }, 300)
}

const selectCity = (city: any) => {
  selectedCityId.value = city.id
  searchCityTerm.value = city.name
  cities.value = []
}

const clearCity = () => {
  selectedCityId.value = null
  searchCityTerm.value = ''
  cities.value = []
}

const fetchProfile = async () => {
  loading.value = true
  try {
    const res = await api.get(`/profile/${usernameParam}`)
    profile.value = res.data
    isOwner.value = authStore.isAuthenticated() && authStore.user?.username === usernameParam
    editUsername.value = profile.value.username
    editBio.value = profile.value.bio
    editIsVisibleOnMap.value = profile.value.isVisibleOnMap || false
    selectedCityId.value = profile.value.cityId
    searchCityTerm.value = profile.value.cityName || ''
    
    // Fetch friends
    try {
      const friendsRes = await api.get(`/profile/${profile.value.id}/friends`)
      friends.value = friendsRes.data
      
      // Check if current user is friend or pending
      if (authStore.isAuthenticated() && authStore.user) {
        const friendMatch = friends.value.find(f => f.id === authStore.user!.userId)
        if (friendMatch) {
          if (friendMatch.status === 1) isFriend.value = true;
          else if (friendMatch.status === 0) isPendingFriend.value = true;
        }
      }
    } catch (e) {
      console.error('Failed to fetch friends')
    }
  } catch (e) {
    console.error('Profile not found')
  } finally {
    loading.value = false
  }
}

const saveProfile = async () => {
  saveError.value = ''
  try {
    const formData = new FormData()
    formData.append('username', editUsername.value)
    formData.append('bio', editBio.value)
    if (selectedCityId.value) {
      formData.append('cityId', selectedCityId.value)
    }
    if (selectedAvatarFile.value) {
      formData.append('avatarFile', selectedAvatarFile.value)
    }

    await api.put(`/profile`, formData, {
      headers: { 'Authorization': `Bearer ${authStore.token}`, 'Content-Type': 'multipart/form-data' }
    })
    
    await api.put(`/profile/visibility`, {
      isVisible: editIsVisibleOnMap.value
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })

    saveSuccess.value = true
    isEditing.value = false
    selectedAvatarFile.value = null
    setTimeout(() => saveSuccess.value = false, 3000)

    // If username changed, redirect to new profile
    if (editUsername.value !== usernameParam) {
      router.push(`/profile/${editUsername.value}`)
    } else {
      fetchProfile()
    }
  } catch (e: any) {
    saveError.value = e.response?.data?.Error || 'Не удалось сохранить профиль'
  }
}

const handleAvatarChange = (e: Event) => {
  const target = e.target as HTMLInputElement
  if (target.files && target.files.length > 0) {
    selectedAvatarFile.value = target.files[0]
  }
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

const startDirectMessage = async () => {
  if (!profile.value) return;
  // Send empty message to create chat if it doesn't exist, or just navigate to messages and preselect
  // Since we don't have a direct "create conversation" endpoint, 
  // maybe we should just redirect to /messages and let them find the user, or add an endpoint for it.
  // For now, let's just go to messages. In a real app we'd open a dialog with them.
  router.push('/messages');
}

const showInviteModal = ref(false);
const myRides = ref<any[]>([]);

const inviteToRide = async () => {
  if (!profile.value) return;
  try {
    const res = await api.get('/rides/my', {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    });
    myRides.value = res.data;
    showInviteModal.value = true;
  } catch (e) {
    console.error(e);
  }
}

const sendInvite = async (rideId: string) => {
  try {
    await api.post('/rideinvites', {
      rideId,
      inviteeId: profile.value.id
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    });
    toast.success('Приглашение отправлено!');
    showInviteModal.value = false;
  } catch (e) {
    console.error(e);
    toast.error('Ошибка при отправке приглашения');
  }
}

const blockUser = async () => {
  if (!profile.value) return;
  if (!confirm('Вы уверены, что хотите заблокировать этого пользователя? Он больше не сможет писать вам.')) return;

  try {
    const res = await fetch(`/api/profile/${profile.value.id}/block`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    });
    if (res.ok) {
      toast.success('Пользователь заблокирован.');
    }
  } catch (e) {
    console.error(e);
  }
}

const isFriend = ref(false)
const isPendingFriend = ref(false)

const addFriend = async () => {
  if (!authStore.isAuthenticated()) {
    toast.warning("Войдите чтобы добавлять друзей.")
    return
  }
  try {
    await api.post(`/profile/friends/${profile.value.id}`, {}, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    toast.success("Заявка в друзья отправлена!")
    isPendingFriend.value = true
  } catch (err: any) {
    toast.error(err.response?.data?.Error || "Ошибка при добавлении в друзья.")
  }
}

const removeFriend = async () => {
  if (!confirm('Отменить заявку / удалить из друзей?')) return;
  try {
    await api.delete(`/profile/friends/${profile.value.id}`, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    toast.success("Удалено из друзей.")
    isFriend.value = false;
    isPendingFriend.value = false;
  } catch (err: any) {
    toast.error("Ошибка при удалении.")
  }
}

onMounted(() => {
  fetchProfile()
})
</script>

<template>
  <div v-if="loading" class="loading-screen">
    <div class="spinner"></div>
  </div>
  <div v-else-if="profile" class="profile-container">
    <!-- Success Toast -->
    <div v-if="saveSuccess" class="toast-success">✓ Профиль обновлён</div>

    <!-- Profile Header Card -->
    <div class="glass-panel profile-header">
      <div class="avatar-section">
        <div class="avatar-wrapper">
          <img v-if="profile.avatarUrl" :src="profile.avatarUrl.startsWith('http') ? profile.avatarUrl : '/s3' + profile.avatarUrl" alt="Avatar" class="avatar" />
          <div v-else class="avatar-placeholder">{{ profile.name?.charAt(0) || '?' }}</div>
          <label v-if="isEditing" class="avatar-edit-btn" title="Изменить фото">
            📷
            <input type="file" @change="handleAvatarChange" accept="image/*" class="hidden-file-input" />
          </label>
        </div>
      </div>

      <div class="info-section">
        <template v-if="!isEditing">
          <div class="name-row">
            <h2 class="display-name">{{ profile.name }}</h2>
            <span class="username-badge">@{{ profile.username }}</span>
          </div>
          <p v-if="profile.cityName" class="city-tag">
            <span class="city-pin">📍</span> {{ profile.cityName }}
          </p>
          <p class="bio-text">{{ profile.bio || 'Нет информации о себе.' }}</p>
          
          <div class="stats-row">
            <div class="stat-pill">
              <span class="stat-num">{{ profile.organizedRidesCount }}</span>
              <span class="stat-lbl">покатушек</span>
            </div>
            <div class="stat-pill">
              <span class="stat-num">{{ profile.vehicles?.length || 0 }}</span>
              <span class="stat-lbl">мотов</span>
            </div>
          </div>

          <div class="profile-actions">
            <button v-if="isOwner" @click="isEditing = true" class="btn-edit">✏️ Редактировать профиль</button>
            <template v-else-if="authStore.isAuthenticated()">
              <button @click="startDirectMessage" class="btn-primary">💬 Написать</button>
              <button v-if="!isFriend && !isPendingFriend" @click="addFriend" class="btn-secondary">➕ В друзья</button>
              <button v-else-if="isPendingFriend" @click="removeFriend" class="btn-secondary" style="color: #ff9800; border-color: #ff9800;">⏳ Заявка отправлена</button>
              <button v-else @click="removeFriend" class="btn-secondary" style="color: #f44336; border-color: #f44336;">❌ Удалить из друзей</button>
              <button @click="inviteToRide" class="btn-secondary">📅 Пригласить</button>
              <button @click="blockUser" class="btn-danger-outline" style="margin-left:auto" title="Заблокировать">🚫</button>
            </template>
          </div>
        </template>

        <template v-else>
          <!-- Edit Form -->
          <div class="edit-form">
            <div class="form-group">
              <label class="field-label">Username</label>
              <div class="username-input-wrapper">
                <span class="username-prefix">@</span>
                <input v-model="editUsername" placeholder="username" class="input-field" />
              </div>
            </div>
            
            <div class="form-group">
              <label class="field-label">📍 Ваш город</label>
              <div class="city-selector">
                <div class="city-input-row">
                  <input 
                    v-model="searchCityTerm" 
                    @input="searchCities" 
                    placeholder="Начните вводить название города..." 
                    class="input-field city-input"
                  />
                  <button v-if="selectedCityId" @click="clearCity" class="clear-city-btn" title="Убрать город">✕</button>
                </div>
                <div v-if="cities.length > 0" class="city-dropdown">
                  <div v-for="city in cities" :key="city.id" @click="selectCity(city)" class="city-option">
                    <span class="city-name-opt">{{ city.name }}</span>
                    <span class="city-region-opt">{{ city.regionName }}</span>
                  </div>
                </div>
                <p v-if="selectedCityId" class="city-selected-hint">✓ Выбрано: {{ searchCityTerm }}</p>
              </div>
            </div>

            <div class="form-group">
              <label class="field-label">О себе</label>
              <textarea v-model="editBio" placeholder="Расскажи о себе, своём стиле езды, любимых маршрутах..." class="input-field textarea"></textarea>
            </div>

            <div class="form-group">
              <label class="field-label" style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
                <input type="checkbox" v-model="editIsVisibleOnMap" />
                <span>Показывать меня на карте (для других)</span>
              </label>
            </div>

            <div v-if="saveError" class="form-error">⚠️ {{ saveError }}</div>

            <div class="edit-actions">
              <button @click="isEditing = false" class="btn-secondary">Отмена</button>
              <button @click="saveProfile" class="btn-primary">💾 Сохранить</button>
            </div>
          </div>
        </template>
      </div>
    </div>

    <!-- Invite Modal -->
    <div v-if="showInviteModal" class="modal-overlay" @click.self="showInviteModal = false">
      <div class="modal-content glass-panel">
        <div class="modal-header">
          <h3>Пригласить в покатушку</h3>
          <button class="close-btn" @click="showInviteModal = false">✕</button>
        </div>
        <div class="modal-body">
          <div v-if="myRides.length === 0" class="empty-state">
            У вас нет активных покатушек, куда можно пригласить.
          </div>
          <div v-else class="rides-list">
            <div v-for="ride in myRides" :key="ride.id" class="ride-select-item">
              <span class="ride-title-small">{{ ride.title }}</span>
              <button @click="sendInvite(ride.id)" class="btn-sm btn-primary">Пригласить</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Tab Bar -->
    <div class="tabs">
      <button class="tab" :class="{ active: activeTab === 'rides' }" @click="activeTab = 'rides'">
        🏍️ Покатушки ({{ profile.organizedRidesCount }})
      </button>
      <button class="tab" :class="{ active: activeTab === 'garage' }" @click="activeTab = 'garage'">
        🔧 Гараж ({{ profile.vehicles?.length || 0 }})
      </button>
      <button class="tab" :class="{ active: activeTab === 'friends' }" @click="activeTab = 'friends'">
        🤝 Друзья ({{ friends.length }})
      </button>
    </div>

    <!-- Rides Tab -->
    <div v-if="activeTab === 'rides'" class="tab-content">
      <div v-if="!profile.organizedRides || profile.organizedRides.length === 0" class="empty-state">
        <div class="empty-icon">🏁</div>
        <p>{{ isOwner ? 'Вы ещё не организовали покатушек. Нажмите на карту чтобы создать первую!' : 'Нет организованных покатушек.' }}</p>
        <router-link v-if="isOwner" to="/" class="btn-primary">Открыть карту</router-link>
      </div>
      <div v-else class="rides-grid">
        <div v-for="ride in profile.organizedRides" :key="ride.id" class="glass-panel ride-card">
          <div class="ride-card-header">
            <h4 class="ride-title">{{ ride.title }}</h4>
            <span class="complexity-badge" :style="{ background: complexityColors[ride.complexity] + '33', color: complexityColors[ride.complexity], borderColor: complexityColors[ride.complexity] }">
              {{ complexityLabels[ride.complexity] || ride.complexity }}
            </span>
          </div>
          <p class="ride-description">{{ ride.description || 'Без описания' }}</p>
          <div class="ride-meta">
            <span>📅 {{ formatDate(ride.eventDate) }}</span>
            <span>👥 {{ ride.membersCount }}/{{ ride.maxMembers }}</span>
            <span v-if="ride.categoryName">🏷️ {{ ride.categoryName }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Garage Tab -->
    <div v-if="activeTab === 'garage'" class="tab-content">
      <div v-if="!profile.vehicles || profile.vehicles.length === 0" class="empty-state">
        <div class="empty-icon">🏍️</div>
        <p>{{ isOwner ? 'В гараже пусто. Добавьте свой первый мотоцикл!' : 'Гараж пуст.' }}</p>
        <router-link v-if="isOwner" to="/garage/new" class="btn-primary">Добавить технику</router-link>
      </div>
      <div v-else class="garage-grid">
        <router-link :to="`/garage/${v.id}`" v-for="v in profile.vehicles" :key="v.id" class="glass-panel vehicle-card">
          <div class="vehicle-icon">🏍️</div>
          <h4 class="vehicle-name">{{ v.brand }} {{ v.model }}</h4>
          <p class="vehicle-year">{{ v.year }}</p>
          <span class="vehicle-category">{{ v.categoryName }}</span>
        </router-link>
      </div>
    </div>

    <!-- Friends Tab -->
    <div v-if="activeTab === 'friends'" class="tab-content">
      <div v-if="friends.length === 0" class="empty-state">
        <div class="empty-icon">👥</div>
        <p>Список друзей пока пуст.</p>
      </div>
      <div v-else class="friends-list">
        <div v-for="f in friends" :key="f.id" class="friend-item glass-panel">
          <div class="friend-info">
            <span class="avatar-mini">{{ f.username.charAt(0).toUpperCase() }}</span>
            <router-link :to="`/profile/${f.username}`" class="friend-name">@{{ f.username }}</router-link>
          </div>
          <div class="friend-status">
            <span v-if="f.status === 0" class="status-pending">Ожидает подтверждения</span>
            <span v-else-if="f.status === 1" class="status-accepted">✓ Друзья</span>
          </div>
        </div>
      </div>
    </div>
  </div>
  <div v-else class="profile-container not-found">
    <div class="glass-panel" style="padding: 60px; text-align: center;">
      <div style="font-size: 4rem; margin-bottom: 20px;">🔍</div>
      <h2>Профиль не найден</h2>
      <p style="color: rgba(255,255,255,0.6);">Пользователя @{{ usernameParam }} не существует</p>
    </div>
  </div>
</template>

<style scoped>
.loading-screen {
  display: flex; align-items: center; justify-content: center;
  min-height: 100vh;
}
.spinner {
  width: 48px; height: 48px; border-radius: 50%;
  border: 4px solid rgba(255,255,255,0.1);
  border-top-color: var(--accent-primary);
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.profile-container {
  padding: 100px 32px 60px;
  max-width: 900px;
  margin: 0 auto;
  color: white;
  position: relative;
}

.toast-success {
  position: fixed; top: 80px; left: 50%; transform: translateX(-50%);
  background: #4caf50; color: white; padding: 12px 24px; border-radius: 100px;
  font-weight: 600; z-index: 9999; box-shadow: 0 4px 20px rgba(76,175,80,0.4);
  animation: slideDown 0.3s ease;
}
@keyframes slideDown { from { opacity: 0; transform: translateX(-50%) translateY(-10px); } }

.profile-header {
  display: flex;
  gap: 32px;
  padding: 36px;
  align-items: flex-start;
  margin-bottom: 24px;
}

.avatar-section { flex-shrink: 0; }

.avatar-wrapper {
  position: relative;
  width: 120px; height: 120px;
}
.avatar, .avatar-placeholder {
  width: 120px; height: 120px; border-radius: 50%;
  object-fit: cover;
  border: 3px solid rgba(255,255,255,0.15);
  box-shadow: 0 0 30px rgba(255,77,0,0.3);
}
.avatar-placeholder {
  display: flex; align-items: center; justify-content: center;
  font-size: 3rem; font-weight: 700;
  background: linear-gradient(135deg, var(--accent-primary), var(--accent-secondary));
}
.avatar-edit-btn {
  position: absolute; bottom: 0; right: 0;
  background: rgba(0,0,0,0.7); border-radius: 50%;
  width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;
  cursor: pointer; font-size: 1rem; transition: background 0.2s;
  border: 2px solid rgba(255,255,255,0.2);
}
.avatar-edit-btn:hover { background: rgba(255,77,0,0.5); }
.hidden-file-input { display: none; }

.info-section { flex: 1; min-width: 0; }

.name-row { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 8px; }
.display-name { font-size: 1.8rem; font-weight: 800; margin: 0; }
.username-badge {
  background: rgba(255,77,0,0.15); color: var(--accent-primary);
  border: 1px solid rgba(255,77,0,0.3); padding: 3px 12px; border-radius: 20px;
  font-size: 0.85rem; font-weight: 600;
}

.city-tag { color: rgba(255,255,255,0.7); font-size: 0.9rem; margin: 4px 0 12px; }
.city-pin { margin-right: 4px; }

.bio-text { color: rgba(255,255,255,0.75); line-height: 1.6; margin: 0 0 20px; }

.stats-row { display: flex; gap: 16px; margin-bottom: 20px; }
.stat-pill {
  display: flex; flex-direction: column; align-items: center;
  background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1);
  border-radius: 12px; padding: 10px 20px;
}
.stat-num { font-size: 1.5rem; font-weight: 800; color: var(--accent-primary); }
.stat-lbl { font-size: 0.75rem; color: rgba(255,255,255,0.5); }

.btn-edit {
  width: 100%;
  padding: 12px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid var(--border-color);
  color: var(--text-light);
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
  transition: all 0.2s;
  margin-top: 15px;
}

.btn-edit:hover {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.2);
}

.profile-actions {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 20px;
}

.profile-actions .btn-primary,
.profile-actions .btn-secondary,
.profile-actions .btn-danger,
.profile-actions .btn-edit {
  width: 100%;
  text-align: center;
}

.btn-danger {
  padding: 12px 24px;
  background: rgba(244, 67, 54, 0.1);
  color: #f44336;
  border: 1px solid rgba(244, 67, 54, 0.3);
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
  transition: all 0.2s;
}

.btn-danger:hover {
  background: rgba(244, 67, 54, 0.2);
}

/* Edit form styles */
.edit-form { display: flex; flex-direction: column; gap: 16px; }
.form-group { display: flex; flex-direction: column; gap: 6px; }
.field-label { font-size: 0.8rem; color: rgba(255,255,255,0.6); font-weight: 600; }
.input-field {
  padding: 10px 14px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.15);
  background: rgba(0,0,0,0.3); color: white; font-size: 0.9rem;
  transition: border-color 0.2s; outline: none; width: 100%; box-sizing: border-box;
}
.input-field:focus { border-color: var(--accent-primary); }
.textarea { min-height: 80px; resize: vertical; }

.username-input-wrapper { display: flex; align-items: center; gap: 0; }
.username-prefix {
  background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.15);
  border-right: none; padding: 10px 10px; border-radius: 8px 0 0 8px;
  color: var(--accent-primary); font-weight: 700;
}
.username-input-wrapper .input-field { border-radius: 0 8px 8px 0; }

.city-selector { position: relative; }
.city-input-row { display: flex; gap: 8px; }
.city-input { flex: 1; }
.clear-city-btn {
  background: rgba(255,0,0,0.1); border: 1px solid rgba(255,0,0,0.2);
  color: rgba(255,255,255,0.6); border-radius: 8px; padding: 0 14px;
  cursor: pointer; transition: all 0.2s;
}
.clear-city-btn:hover { background: rgba(255,0,0,0.2); color: white; }
.city-selected-hint { font-size: 0.8rem; color: #4caf50; margin: 4px 0 0; }

.city-dropdown {
  position: absolute; top: calc(100% + 4px); left: 0; right: 0;
  background: #1a1a1a; border: 1px solid rgba(255,255,255,0.15);
  border-radius: 10px; max-height: 220px; overflow-y: auto; z-index: 100;
  box-shadow: 0 8px 30px rgba(0,0,0,0.5);
}
.city-option {
  padding: 10px 14px; cursor: pointer;
  display: flex; justify-content: space-between; align-items: center;
  transition: background 0.15s;
}
.city-option:hover { background: rgba(255,255,255,0.08); }
.city-name-opt { font-weight: 600; }
.city-region-opt { font-size: 0.8rem; color: rgba(255,255,255,0.4); }

.edit-actions { display: flex; gap: 10px; justify-content: flex-end; }

.form-error {
  color: #ff6b6b; font-size: 0.85rem; padding: 8px 12px;
  background: rgba(255,0,0,0.1); border-radius: 8px;
}

/* Tabs */
.tabs {
  display: flex; gap: 4px; margin-bottom: 20px;
  background: rgba(0,0,0,0.2); padding: 4px; border-radius: 12px;
}
.tab {
  flex: 1; padding: 10px 16px; border: none; background: transparent;
  color: rgba(255,255,255,0.5); cursor: pointer; border-radius: 8px;
  font-weight: 600; font-size: 0.9rem; transition: all 0.2s;
}
.tab.active { background: rgba(255,255,255,0.1); color: white; }
.tab:hover:not(.active) { color: rgba(255,255,255,0.8); }

.tab-content { min-height: 200px; }

/* Empty state */
.empty-state {
  text-align: center; padding: 60px 20px;
  color: rgba(255,255,255,0.5);
}
.empty-icon { font-size: 3rem; margin-bottom: 16px; }
.empty-state p { margin: 0 0 20px; }

/* Rides grid */
.rides-grid {
  display: flex; flex-direction: column; gap: 14px;
}
.ride-card {
  padding: 20px; transition: transform 0.2s, box-shadow 0.2s;
}
.ride-card:hover { transform: translateY(-2px); box-shadow: 0 8px 30px rgba(0,0,0,0.3); }
.ride-card-header {
  display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 8px;
}
.ride-title { margin: 0; font-size: 1.1rem; font-weight: 700; }
.complexity-badge {
  padding: 3px 10px; border-radius: 20px; font-size: 0.75rem;
  font-weight: 700; border: 1px solid; flex-shrink: 0; margin-left: 10px;
}
.ride-description { color: rgba(255,255,255,0.6); font-size: 0.9rem; margin: 0 0 12px; }
.ride-meta { display: flex; gap: 16px; flex-wrap: wrap; font-size: 0.8rem; color: rgba(255,255,255,0.5); }

/* Garage grid */
.garage-grid {
  display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 16px;
}
.vehicle-card {
  padding: 24px; text-align: center; cursor: pointer;
  text-decoration: none; color: white;
  transition: transform 0.2s, box-shadow 0.2s;
}
.vehicle-card:hover { transform: translateY(-3px); box-shadow: 0 8px 30px rgba(255,77,0,0.2); }
.vehicle-icon { font-size: 2.5rem; margin-bottom: 10px; }
.vehicle-name { margin: 0 0 4px; font-size: 1rem; font-weight: 700; }
.vehicle-year { margin: 0 0 8px; color: rgba(255,255,255,0.5); font-size: 0.85rem; }
.vehicle-category {
  background: rgba(255,77,0,0.15); color: var(--accent-primary);
  border: 1px solid rgba(255,77,0,0.25); padding: 2px 10px;
  border-radius: 20px; font-size: 0.75rem; font-weight: 600;
}

.not-found { padding-top: 160px; }

/* Primary/Secondary buttons (same as global) */
.btn-primary {
  background: var(--accent-primary); color: white; border: none;
  padding: 10px 20px; border-radius: 8px; cursor: pointer; font-weight: 600;
  transition: all 0.2s; text-decoration: none; display: inline-block;
}
.btn-primary:hover { opacity: 0.85; }
.btn-secondary {
  background: transparent; border: 1px solid rgba(255,255,255,0.2);
  color: rgba(255,255,255,0.8); padding: 10px 20px; border-radius: 8px;
  cursor: pointer; font-weight: 600; transition: all 0.2s;
}
.btn-secondary:hover { background: rgba(255,255,255,0.08); }

/* Modal */
.modal-overlay {
  position: fixed; top: 0; left: 0; width: 100%; height: 100%;
  background: rgba(0,0,0,0.7); backdrop-filter: blur(5px);
  display: flex; justify-content: center; align-items: center;
  z-index: 1000;
}
.modal-content {
  width: 90%; max-width: 400px;
  padding: 20px; border-radius: 16px;
}
.modal-header {
  display: flex; justify-content: space-between; align-items: center;
  margin-bottom: 20px;
}
.close-btn {
  background: transparent; border: none; color: white;
  font-size: 1.5rem; cursor: pointer;
}
.rides-list {
  display: flex; flex-direction: column; gap: 10px;
}
.ride-select-item {
  display: flex; justify-content: space-between; align-items: center;
  padding: 10px; background: rgba(255,255,255,0.05); border-radius: 8px;
}
.ride-title-small {
  font-weight: 600;
}
.btn-sm {
  padding: 6px 12px; font-size: 0.8rem; border-radius: 6px;
}
.friends-list {
  display: flex; flex-direction: column; gap: 10px;
}
.friend-item {
  display: flex; justify-content: space-between; align-items: center;
  padding: 15px; border-radius: 12px;
}
.friend-info {
  display: flex; align-items: center; gap: 10px;
}
.avatar-mini {
  width: 32px; height: 32px; background: rgba(255,255,255,0.1);
  border-radius: 50%; display: flex; align-items: center; justify-content: center;
  font-weight: 700; color: var(--accent-primary);
}
.friend-name {
  color: white; font-weight: 600; text-decoration: none; font-size: 1.1rem;
}
.friend-name:hover {
  text-decoration: underline; color: var(--accent-primary);
}
.status-pending { color: #ff9800; font-size: 0.9rem; }
.status-accepted { color: #4caf50; font-weight: bold; }
</style>
