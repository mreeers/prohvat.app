<template>
  <div class="messages-container">
    <div class="sidebar">
      <div v-if="invites.length > 0" class="invites-section">
        <h3>Приглашения</h3>
        <div v-for="inv in invites" :key="inv.inviteId" class="invite-item">
          <div class="invite-info">
            <strong>{{ inv.inviterName }}</strong> зовет на <em>{{ inv.rideTitle }}</em>
          </div>
          <div class="invite-actions">
            <button @click="respondToInvite(inv.inviteId, true)" class="btn-sm btn-success">Принять</button>
            <button @click="respondToInvite(inv.inviteId, false)" class="btn-sm btn-danger">Отклонить</button>
          </div>
        </div>
      </div>
      
      <h3>Чаты</h3>
      <div 
        v-for="conv in conversations" 
        :key="conv.id" 
        class="conversation-item"
        :class="{ active: selectedConversationId === conv.id }"
        @click="selectConversation(conv.id)"
      >
        <div class="conv-avatar">
          <!-- Fallback avatar for now -->
          <span>{{ getConvName(conv).charAt(0) }}</span>
        </div>
        <div class="conv-info">
          <div class="conv-name">{{ getConvName(conv) }}</div>
          <div class="conv-last-msg" v-if="conv.lastMessage">
            {{ conv.lastMessage.text }}
          </div>
        </div>
      </div>
    </div>

    <div class="main-chat" v-if="selectedConversation">
      <div class="chat-header">
        <h4>{{ getConvName(selectedConversation) }}</h4>
      </div>
      <div class="chat-messages" ref="messagesContainer">
        <div 
          v-for="msg in currentMessages" 
          :key="msg.id" 
          class="message-wrapper"
          :class="{ 'mine': msg.senderId === authStore.user?.userId }"
        >
          <div class="message-bubble">
            <div class="sender-name" v-if="msg.senderId !== authStore.user?.userId">{{ msg.userName }}</div>
            <div class="message-text">{{ msg.text }}</div>
            <div class="message-time">{{ new Date(msg.createdAt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) }}</div>
          </div>
        </div>
      </div>
      <div class="chat-input">
        <input 
          type="text" 
          v-model="newMessage" 
          @keyup.enter="sendMessage"
          placeholder="Написать сообщение..." 
        />
        <button @click="sendMessage">Отправить</button>
      </div>
    </div>
    <div class="main-chat no-chat" v-else>
      <p>Выберите чат, чтобы начать общение</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick, computed } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useToast } from "vue-toastification";
import { signalRService } from '../services/signalr';

const authStore = useAuthStore();
const toast = useToast();

// Type definitions (simplified for UI)
interface Conversation {
  id: string;
  type: number; // 1: Direct, 2: RideGroup
  title?: string;
  lastMessage?: any;
  participants: any[];
}

interface Message {
  id: string;
  conversationId: string;
  senderId: string;
  userName: string;
  text: string;
  createdAt: string;
}

interface Invite {
  inviteId: string;
  rideId: string;
  rideTitle: string;
  inviterId: string;
  inviterName: string;
}

const conversations = ref<Conversation[]>([]);
const invites = ref<Invite[]>([]);
const selectedConversationId = ref<string | null>(null);
const currentMessages = ref<Message[]>([]);
const newMessage = ref('');
const messagesContainer = ref<HTMLElement | null>(null);

const selectedConversation = computed(() => {
  return conversations.value.find(c => c.id === selectedConversationId.value);
});

const getConvName = (conv: Conversation) => {
  if (conv.type === 2) return conv.title || 'Чат покатушки';
  // For direct messages, find the other participant
  const other = conv.participants.find(p => p.userId !== authStore.user?.userId);
  return other ? other.userName : 'Диалог';
};

const fetchConversations = async () => {
  try {
    const res = await fetch('/api/conversations', {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    });
    if (res.ok) {
      conversations.value = await res.json();
    }
  } catch (err) {
    console.error(err);
  }
};

const fetchInvites = async () => {
  try {
    const res = await fetch('/api/rideinvites/my', {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    });
    if (res.ok) {
      invites.value = await res.json();
    }
  } catch (err) {
    console.error(err);
  }
};

const respondToInvite = async (inviteId: string, accept: boolean) => {
  try {
    const res = await fetch(`/api/rideinvites/${inviteId}/respond`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${authStore.token}`
      },
      body: JSON.stringify({ accept })
    });
    if (res.ok) {
      // Remove from list and refresh chats just in case we accepted and got added to a ride chat
      invites.value = invites.value.filter(i => i.inviteId !== inviteId);
      if (accept) {
        await fetchConversations();
      }
    }
  } catch (err) {
    console.error(err);
  }
};

const selectConversation = async (id: string) => {
  if (selectedConversationId.value) {
    await signalRService.leaveConversation(selectedConversationId.value);
  }
  
  selectedConversationId.value = id;
  currentMessages.value = [];
  
  try {
    const res = await fetch(`/api/conversations/${id}/messages`, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    });
    if (res.ok) {
      currentMessages.value = await res.json();
      scrollToBottom();
    }
    
    // Join SignalR group
    await signalRService.joinConversation(id);
  } catch (err) {
    console.error(err);
  }
};

const sendMessage = async () => {
  if (!newMessage.value.trim() || !selectedConversationId.value) return;
  
  try {
    const res = await fetch(`/api/conversations/${selectedConversationId.value}/messages`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${authStore.token}`
      },
      body: JSON.stringify({ text: newMessage.value })
    });
    
    if (res.ok) {
      newMessage.value = '';
    } else {
      toast.error('Не удалось отправить сообщение. Возможно вы заблокированы.');
    }
  } catch (err) {
    console.error(err);
  }
};

const handleReceiveMessage = (msg: Message) => {
  if (msg.conversationId === selectedConversationId.value) {
    currentMessages.value.push(msg);
    scrollToBottom();
  } else {
    // Refresh conversations list to update last message
    fetchConversations();
  }
};

const scrollToBottom = () => {
  nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
    }
  });
};

onMounted(async () => {
  await fetchConversations();
  await fetchInvites();
  signalRService.onReceiveMessage(handleReceiveMessage);
});

onUnmounted(() => {
  if (selectedConversationId.value) {
    signalRService.leaveConversation(selectedConversationId.value);
  }
  signalRService.offReceiveMessage(handleReceiveMessage);
});
</script>

<style scoped>
.messages-container {
  display: flex;
  height: calc(100vh - 60px);
  background: var(--bg-dark);
  color: var(--text-light);
}

.sidebar {
  width: 300px;
  background: var(--glass-bg);
  backdrop-filter: blur(10px);
  border-right: 1px solid var(--border-color);
  padding: 20px;
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.invites-section {
  margin-bottom: 20px;
  padding-bottom: 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.invite-item {
  background: rgba(255, 77, 0, 0.1);
  border: 1px solid var(--accent-primary);
  border-radius: 8px;
  padding: 10px;
  margin-top: 10px;
}

.invite-info {
  font-size: 0.9rem;
  margin-bottom: 8px;
}

.invite-actions {
  display: flex;
  gap: 10px;
}

.invite-actions button {
  flex: 1;
}

.sidebar h3 {
  padding: 20px;
  margin: 0;
  border-bottom: 1px solid var(--border-color);
}

.conversation-item {
  display: flex;
  padding: 15px 20px;
  cursor: pointer;
  border-bottom: 1px solid var(--border-color);
  transition: background 0.2s;
}

.conversation-item:hover {
  background: rgba(255, 255, 255, 0.05);
}

.conversation-item.active {
  background: rgba(255, 90, 0, 0.1);
  border-left: 3px solid var(--primary-orange);
}

.conv-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: var(--primary-orange);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 15px;
  font-weight: bold;
}

.conv-info {
  flex: 1;
  overflow: hidden;
}

.conv-name {
  font-weight: 600;
  margin-bottom: 5px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.conv-last-msg {
  font-size: 0.85em;
  color: #888;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.main-chat {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.main-chat.no-chat {
  align-items: center;
  justify-content: center;
  color: #888;
}

.chat-header {
  padding: 20px;
  border-bottom: 1px solid var(--border-color);
  background: rgba(255, 255, 255, 0.02);
}

.chat-header h4 {
  margin: 0;
}

.chat-messages {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.message-wrapper {
  display: flex;
  justify-content: flex-start;
}

.message-wrapper.mine {
  justify-content: flex-end;
}

.message-bubble {
  max-width: 70%;
  padding: 10px 15px;
  border-radius: 12px;
  background: #333;
  position: relative;
}

.message-wrapper.mine .message-bubble {
  background: var(--primary-orange);
  color: #fff;
}

.sender-name {
  font-size: 0.8em;
  color: var(--primary-orange);
  margin-bottom: 5px;
  font-weight: 600;
}

.message-wrapper.mine .sender-name {
  display: none;
}

.message-text {
  word-break: break-word;
}

.message-time {
  font-size: 0.7em;
  color: rgba(255, 255, 255, 0.5);
  text-align: right;
  margin-top: 5px;
}

.chat-input {
  padding: 20px;
  border-top: 1px solid var(--border-color);
  display: flex;
  gap: 10px;
  background: var(--bg-dark);
}

.chat-input input {
  flex: 1;
  padding: 12px 15px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background: rgba(255, 255, 255, 0.05);
  color: var(--text-light);
  outline: none;
}

.chat-input input:focus {
  border-color: var(--primary-orange);
}

.chat-input button {
  padding: 12px 25px;
  border-radius: 8px;
  background: var(--primary-orange);
  color: #fff;
  border: none;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}

.chat-input button:hover {
  background: #e65000;
}
</style>
