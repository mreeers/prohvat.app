<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const props = defineProps<{
  targetType: number,
  targetId: string
}>()

const authStore = useAuthStore()
const likesCount = ref(0)
const isLiked = ref(false)
const isLoading = ref(false)

const fetchLikes = async () => {
  try {
    const res = await api.get(`/interactions/likes/${props.targetType}/${props.targetId}`)
    likesCount.value = res.data.count
  } catch (e) {
    console.error("Failed to fetch likes", e)
  }
}

const toggleLike = async () => {
  if (!authStore.isAuthenticated() || isLoading.value) return
  
  isLoading.value = true
  try {
    // Optimistic UI update could be added here, but let's do it safely for now
    const res = await api.post('/interactions/likes/toggle', {
      targetId: props.targetId,
      targetType: props.targetType
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    
    isLiked.value = res.data.isLiked
    // Adjust count based on action
    if (isLiked.value) {
      likesCount.value++
    } else {
      likesCount.value--
    }
  } catch (e) {
    console.error("Failed to toggle like", e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchLikes()
})
</script>

<template>
  <button 
    class="like-btn" 
    :class="{ 'liked': isLiked, 'disabled': !authStore.isAuthenticated() }"
    @click="toggleLike"
    :title="!authStore.isAuthenticated() ? 'Войдите, чтобы поставить лайк' : ''"
  >
    <span class="heart">❤️</span>
    <span class="count">{{ likesCount }}</span>
  </button>
</template>

<style scoped>
.like-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 20px;
  padding: 4px 12px;
  color: white;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 0.9em;
}

.like-btn:hover:not(.disabled) {
  background: rgba(255, 255, 255, 0.2);
  transform: scale(1.05);
}

.like-btn.liked {
  background: rgba(255, 75, 75, 0.2);
  border-color: rgba(255, 75, 75, 0.5);
}

.like-btn.liked .heart {
  animation: pop 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.heart {
  font-size: 1.1em;
  filter: grayscale(100%) opacity(0.7);
  transition: filter 0.2s;
}

.like-btn.liked .heart {
  filter: grayscale(0%) opacity(1);
}

.count {
  font-weight: 500;
}

.disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

@keyframes pop {
  0% { transform: scale(1); }
  50% { transform: scale(1.3); }
  100% { transform: scale(1); }
}
</style>
