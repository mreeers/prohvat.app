<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import CommentSection from './CommentSection.vue'

const props = defineProps<{
  ride: any
}>()

const emit = defineEmits(['close'])

const reviews = ref<any[]>([])
const authStore = useAuthStore()
const isMember = ref(false)
const activeTab = ref('comments')

const newReviewText = ref('')
const newReviewRating = ref(5)

const newStatus = ref(1)
const newReport = ref('')

const invites = ref<any[]>([])
const members = ref<any[]>([])
const myFriends = ref<any[]>([])
const selectedFriendId = ref('')
const newInviteUsername = ref('')
const isOrganizer = computed(() => authStore.user && authStore.user.userId === props.ride.organizerId)

const fetchRideData = async () => {
  // Assuming we don't have a GET /rides/{id} endpoint specifically, we can fetch reviews
  try {
    const res = await api.get(`/social/reviews/${props.ride.id}`, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    reviews.value = res.data

    if (authStore.isAuthenticated()) {
      const memRes = await api.get(`/rides/${props.ride.id}/membership`, {
        headers: { 'Authorization': `Bearer ${authStore.token}` }
      })
      isMember.value = memRes.data.isMember
      
      if (isOrganizer.value) {
        fetchInvites();
      }
    }
    
    // Fetch members
    const memListRes = await api.get(`/rides/${props.ride.id}/members`)
    members.value = memListRes.data

    // Fetch friends if logged in
    if (authStore.isAuthenticated() && authStore.user) {
      const friendsRes = await api.get(`/profile/${authStore.user.userId}/friends`)
      myFriends.value = friendsRes.data.filter((f: any) => f.status === 1) // only accepted friends
    }

  } catch (err) {
    console.error("Failed to fetch data", err)
  }
}

const fetchInvites = async () => {
  try {
    const res = await api.get(`/rideinvites/ride/${props.ride.id}`, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    invites.value = res.data
  } catch (err) {
    console.error("Failed to fetch invites", err)
  }
}

const sendInvite = async () => {
  let targetUsername = newInviteUsername.value;
  let inviteeId = selectedFriendId.value;

  if (!targetUsername && !inviteeId) {
    alert("Укажите пользователя или выберите друга.");
    return;
  }

  try {
    if (!inviteeId && targetUsername) {
      // Find user by username
      const profileRes = await api.get(`/profile/${targetUsername.replace('@', '')}`);
      inviteeId = profileRes.data.id;
    }

    // Send invite
    await api.post('/rideinvites', {
      rideId: props.ride.id,
      inviteeId: inviteeId
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    });

    alert('Приглашение отправлено!');
    newInviteUsername.value = '';
    selectedFriendId.value = '';
    fetchInvites();
  } catch (err: any) {
    if (err.response && err.response.status === 404) {
      alert('Пользователь с таким юзернеймом не найден.');
    } else {
      alert(err.response?.data?.Error || 'Ошибка при отправке приглашения.');
    }
  }
}

const postReview = async () => {
  try {
    await api.post('/social/reviews', {
      rideId: props.ride.id,
      rating: newReviewRating.value,
      text: newReviewText.value
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    newReviewText.value = ''
    fetchRideData()
  } catch (e: any) {
    alert(e.response?.data?.Error || "Failed to post review")
  }
}

const joinRide = async () => {
  if (!authStore.isAuthenticated()) {
    alert("Войдите, чтобы присоединиться к покатушке.")
    return
  }
  try {
    await api.post(`/rides/${props.ride.id}/join`, {}, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    isMember.value = true
    alert("Вы успешно присоединились!")
    fetchRideData() // Refresh members list if needed
  } catch (e: any) {
    alert(e.response?.data?.Error || "Ошибка при присоединении.")
  }
}

const updateStatus = async () => {
  try {
    await api.put(`/rides/${props.ride.id}/status`, {
      status: newStatus.value,
      report: newReport.value
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    alert("Статус обновлен!")
    props.ride.status = newStatus.value
  } catch (e: any) {
    alert("Ошибка при обновлении статуса")
  }
}

onMounted(() => {
  if (props.ride) {
    newStatus.value = props.ride.status || 1
  }
  fetchRideData()
})
</script>

<template>
  <div class="modal-backdrop" @click.self="emit('close')">
    <div class="modal-content glass-panel">
      <div class="modal-header">
        <h2>Покатушка</h2>
        <button class="close-btn" @click="emit('close')">×</button>
      </div>

      <div class="modal-body">
        <div class="tabs">
          <button :class="{ active: activeTab === 'info' }" @click="activeTab = 'info'">Инфо</button>
          <button :class="{ active: activeTab === 'members' }" @click="activeTab = 'members'">Участники</button>
          <button :class="{ active: activeTab === 'comments' }" @click="activeTab = 'comments'">Комментарии</button>
          <button :class="{ active: activeTab === 'chat' }" @click="activeTab = 'chat'">Чат</button>
          <button :class="{ active: activeTab === 'reviews' }" @click="activeTab = 'reviews'">Отзывы</button>
          <button v-if="isOrganizer" :class="{ active: activeTab === 'invites' }" @click="activeTab = 'invites'">Приглашения</button>
        </div>

        <div v-show="activeTab === 'info'">
          <h3>{{ ride.title }}</h3>
          <p>{{ ride.description }}</p>
          <p><strong>Дата:</strong> {{ new Date(ride.eventDate).toLocaleString() }}</p>
          <p><strong>Участников:</strong> до {{ ride.maxMembers }}</p>
          <p>
            <strong>Статус:</strong> 
            <span v-if="ride.status === 1">Запланирована</span>
            <span v-if="ride.status === 2" style="color: lightgreen;">Завершена</span>
            <span v-if="ride.status === 3" style="color: salmon;">Отменена/Провалена</span>
          </p>
          
          <div class="mt-4 mb-4" v-if="authStore.isAuthenticated() && !isOrganizer && ride.status === 1">
            <button v-if="!isMember" @click="joinRide" class="btn-primary w-full text-lg py-3">
              🏍️ Присоединиться
            </button>
            <div v-else class="text-green-500 font-bold p-3 text-center" style="background: rgba(0,255,0,0.1); border-radius: 8px;">
              ✅ Вы участвуете в этой покатушке
            </div>
          </div>

          <div v-if="authStore.user && authStore.user.userId === ride.organizerId" class="organizer-controls mt-4">
            <h4>Управление (только для организатора)</h4>
            <div class="form-group">
              <label>Статус</label>
              <select v-model="newStatus" class="input-field">
                <option :value="1">Запланирована</option>
                <option :value="2">Завершена</option>
                <option :value="3">Отменена</option>
              </select>
            </div>
            <div class="form-group mt-2" v-if="newStatus === 2 || newStatus === 3">
              <label>Отчет/Причина</label>
              <textarea v-model="newReport" class="input-field" placeholder="Как всё прошло или почему отменили?"></textarea>
            </div>
            <button @click="updateStatus" class="btn-primary mt-2">Сохранить статус</button>
          </div>
        </div>

        <div v-show="activeTab === 'members'">
          <h3>Участники ({{ members.length }} / {{ ride.maxMembers }})</h3>
          <div class="members-list mt-2">
            <div v-if="members.length === 0" class="text-gray-400">Пока никто не присоединился.</div>
            <div v-for="m in members" :key="m.userId" class="member-item flex items-center gap-3 p-2 border-b border-gray-700">
              <div class="avatar-mini">{{ m.username.charAt(0).toUpperCase() }}</div>
              <router-link :to="`/profile/${m.username}`" @click="emit('close')" class="font-bold text-white hover:text-orange-500">
                @{{ m.username }}
              </router-link>
              <span v-if="m.userId === ride.organizerId" class="text-xs bg-orange-500 text-white px-2 py-1 rounded ml-auto">Орг</span>
            </div>
          </div>
        </div>

        <div v-show="activeTab === 'comments'">
          <CommentSection :targetType="3" :targetId="ride.id" />
        </div>

        <div v-show="activeTab === 'chat'">
          <div v-if="isMember || isOrganizer">
            <!-- Redirect to Messages view with selected ride conversation if needed, 
                 or keep RideChat but pass the conversationId.
                 Since RideChat still expects RideId in its props, we'll keep it as is,
                 but it needs to be updated to use ConversationId internally or we just show a link. -->
            <p>Чат переехал в раздел "Сообщения".</p>
            <router-link to="/messages" class="btn-primary" @click="emit('close')">Перейти в Сообщения</router-link>
          </div>
          <div v-else>
            <p>Присоединитесь к покатушке, чтобы участвовать в чате.</p>
          </div>
        </div>

        <div v-show="activeTab === 'invites'" v-if="isOrganizer">
          <h3>Приглашения</h3>
          <div class="invite-form flex gap-2 mb-4 flex-col">
            <div class="flex gap-2">
              <select v-model="selectedFriendId" class="input-field flex-1" v-if="myFriends.length > 0">
                <option disabled value="">Выберите друга...</option>
                <option v-for="f in myFriends" :key="f.id" :value="f.id">@{{ f.username }}</option>
              </select>
              <span class="text-gray-400 self-center">или</span>
              <input v-model="newInviteUsername" placeholder="username пользователя" class="input-field flex-1" />
            </div>
            <button @click="sendInvite" class="btn-primary mt-2">Пригласить</button>
          </div>
          <div class="invites-list">
            <div v-if="invites.length === 0" class="no-reviews">Нет отправленных приглашений.</div>
            <div v-for="invite in invites" :key="invite.id" class="glass-panel p-2 mt-2">
              <div class="flex justify-between items-center">
                <div>
                  <strong>{{ invite.inviteeName }}</strong>
                  <div class="text-sm text-gray-400">
                    Статус: 
                    <span v-if="invite.status === 1" class="text-yellow-500">Ожидает</span>
                    <span v-if="invite.status === 2" class="text-green-500">Принято</span>
                    <span v-if="invite.status === 3" class="text-red-500">Отклонено</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-show="activeTab === 'reviews'" class="reviews-section mt-4">
          <h3>Отзывы о покатушке</h3>
          <div class="reviews-list">
            <div v-if="reviews.length === 0" class="no-reviews">Отзывов пока нет.</div>
            <div v-for="r in reviews" :key="r.id" class="review-item">
              <div class="review-header">
                <strong>@{{ r.username }}</strong>
                <span class="rating">⭐ {{ r.rating }}/5</span>
              </div>
              <p>{{ r.text }}</p>
            </div>
          </div>

          <div class="write-review mt-2" v-if="authStore.isAuthenticated()">
            <h4>Оставить отзыв (только для участников)</h4>
            <div class="rating-select">
              <label>Оценка:</label>
              <select v-model="newReviewRating" class="input-field select">
                <option :value="5">5 - Отлично</option>
                <option :value="4">4 - Хорошо</option>
                <option :value="3">3 - Нормально</option>
                <option :value="2">2 - Плохо</option>
                <option :value="1">1 - Ужасно</option>
              </select>
            </div>
            <textarea v-model="newReviewText" placeholder="Как всё прошло?" class="input-field mt-2"></textarea>
            <button @click="postReview" class="btn-primary mt-2">Отправить отзыв</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-backdrop {
  position: fixed; top: 0; left: 0; width: 100vw; height: 100vh;
  background: rgba(0,0,0,0.7); z-index: 3000;
  display: flex; align-items: center; justify-content: center;
}
.modal-content {
  width: 90%; max-width: 600px; max-height: 90vh;
  overflow-y: auto; padding: 24px;
}
.modal-header {
  display: flex; justify-content: space-between; align-items: center;
  border-bottom: 1px solid rgba(255,255,255,0.1); padding-bottom: 16px;
}
.close-btn {
  background: none; border: none; color: white; font-size: 1.5rem; cursor: pointer;
}
.mt-4 { margin-top: 32px; }
.mt-2 { margin-top: 16px; }

.review-item {
  background: rgba(0,0,0,0.3); padding: 12px; border-radius: 8px; margin-bottom: 10px;
}
.review-header {
  display: flex; justify-content: space-between; margin-bottom: 8px;
}
.rating { color: gold; }
.input-field {
  width: 100%; padding: 10px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.2);
  background: rgba(0,0,0,0.5); color: white;
}
.select { width: auto; display: inline-block; margin-left: 10px; }

.tabs {
  display: flex; gap: 10px; margin-bottom: 20px; border-bottom: 1px solid rgba(255,255,255,0.1);
}
.tabs button {
  background: none; border: none; color: var(--text-muted); padding: 10px 15px; cursor: pointer;
  border-bottom: 2px solid transparent; font-size: 1rem;
}
.tabs button.active {
  color: white; border-bottom-color: var(--accent-primary); font-weight: bold;
}
</style>
