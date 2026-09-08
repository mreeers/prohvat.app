<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const props = defineProps<{
  targetType: number,
  targetId: string
}>()

const comments = ref<any[]>([])
const newComment = ref('')
const authStore = useAuthStore()

const fetchComments = async () => {
  try {
    const res = await api.get(`/interactions/comments/${props.targetType}/${props.targetId}`)
    comments.value = res.data
  } catch (e) {
    console.error("Failed to load comments", e)
  }
}

const postComment = async () => {
  if (!newComment.value.trim()) return
  try {
    await api.post('/interactions/comments', {
      targetId: props.targetId,
      targetType: props.targetType,
      text: newComment.value
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    newComment.value = ''
    fetchComments()
  } catch (e) {
    console.error("Failed to post comment", e)
  }
}

const highlightMentions = (text: string) => {
  return text.replace(/@(\w+)/g, '<span class="mention">@$1</span>')
}

onMounted(() => {
  fetchComments()
})
</script>

<template>
  <div class="comments-section">
    <h4>Комментарии ({{ comments.length }})</h4>
    <div class="comments-list">
      <div v-if="comments.length === 0" class="no-comments">Пока нет комментариев.</div>
      <div v-for="c in comments" :key="c.id" class="comment">
        <div class="comment-header">
          <router-link :to="'/profile/' + c.username" class="comment-author">@{{ c.username }}</router-link>
          <span class="comment-date">{{ new Date(c.createdAt).toLocaleString() }}</span>
        </div>
        <div class="comment-text" v-html="highlightMentions(c.text)"></div>
      </div>
    </div>
    <div class="comment-input-area" v-if="authStore.isAuthenticated()">
      <textarea v-model="newComment" placeholder="Написать комментарий... (можно упоминать через @username)" class="input-field"></textarea>
      <button @click="postComment" class="btn-primary mt-2">Отправить</button>
    </div>
  </div>
</template>

<style scoped>
.comments-section {
  margin-top: 16px;
  border-top: 1px solid rgba(255,255,255,0.1);
  padding-top: 16px;
}
.comments-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 16px;
}
.no-comments {
  color: var(--text-muted);
  font-size: 0.9em;
}
.comment {
  background: rgba(0,0,0,0.2);
  padding: 10px;
  border-radius: 8px;
}
.comment-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 6px;
  font-size: 0.85em;
}
.comment-author {
  color: var(--primary-color);
  font-weight: 500;
  text-decoration: none;
}
.comment-date {
  color: var(--text-muted);
}
.comment-text {
  font-size: 0.95em;
  color: #ddd;
  line-height: 1.4;
}
:deep(.mention) {
  color: var(--accent-primary);
  font-weight: bold;
}
.input-field {
  width: 100%;
  padding: 10px;
  border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.2);
  background: rgba(0,0,0,0.5);
  color: white;
  min-height: 60px;
  resize: vertical;
}
.mt-2 { margin-top: 8px; }
</style>
