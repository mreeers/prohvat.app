<script setup lang="ts">
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { useAuthStore } from '../stores/auth'

const props = defineProps<{
  vehicleId: string,
  initialConfigJson: string | null
}>()

const emit = defineEmits(['configUpdated'])
const authStore = useAuthStore()

// State for key-value pairs
const configPairs = ref<{key: string, value: string}[]>([])
const saving = ref(false)

onMounted(() => {
  if (props.initialConfigJson) {
    try {
      const parsed = JSON.parse(props.initialConfigJson)
      configPairs.value = Object.keys(parsed).map(k => ({ key: k, value: parsed[k] }))
    } catch (e) {
      console.error("Invalid JSON config")
    }
  }
})

const addPair = () => {
  configPairs.value.push({ key: '', value: '' })
}

const removePair = (index: number) => {
  configPairs.value.splice(index, 1)
}

const saveConfig = async () => {
  saving.value = true
  try {
    // Convert array back to object
    const configObj: Record<string, string> = {}
    configPairs.value.forEach(p => {
      if (p.key.trim()) {
        configObj[p.key.trim()] = p.value.trim()
      }
    })
    
    const jsonString = JSON.stringify(configObj)
    
    await axios.put(`http://localhost:8081/api/vehicles/${props.vehicleId}/config`, {
      technicalConfigJson: jsonString
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    
    emit('configUpdated', jsonString)
  } catch (err) {
    console.error("Failed to save config", err)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="tuning-editor glass-panel">
    <h3>Конфиг Тюнинга</h3>
    <p class="subtitle">Укажите, какие запчасти установлены на технике.</p>
    
    <div v-for="(pair, idx) in configPairs" :key="idx" class="pair-row">
      <input v-model="pair.key" placeholder="Узел (напр: Руль)" class="input-field" />
      <span class="divider">-</span>
      <input v-model="pair.value" placeholder="Деталь (напр: Renthal Twinwall)" class="input-field" />
      <button @click="removePair(idx)" class="btn-danger" title="Удалить">×</button>
    </div>
    
    <div class="actions">
      <button @click="addPair" class="btn-secondary">+ Добавить узел</button>
      <button @click="saveConfig" class="btn-primary" :disabled="saving">
        {{ saving ? 'Сохранение...' : 'Сохранить конфиг' }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.tuning-editor { padding: 20px; border-radius: 12px; margin: 20px 0; }
.subtitle { font-size: 0.9em; color: rgba(255,255,255,0.7); margin-bottom: 15px; }
.pair-row { display: flex; gap: 10px; align-items: center; margin-bottom: 10px; }
.input-field { padding: 8px; border-radius: 6px; border: 1px solid rgba(255,255,255,0.2); background: rgba(0,0,0,0.3); color: white; flex: 1; }
.divider { color: rgba(255,255,255,0.5); font-weight: bold; }
.btn-danger { background: rgba(255,0,0,0.5); color: white; border: none; border-radius: 6px; width: 30px; height: 30px; cursor: pointer; }
.btn-danger:hover { background: rgba(255,0,0,0.8); }
.btn-secondary { background: rgba(255,255,255,0.1); color: white; border: 1px solid rgba(255,255,255,0.2); border-radius: 6px; padding: 8px 12px; cursor: pointer; }
.btn-secondary:hover { background: rgba(255,255,255,0.2); }
.actions { display: flex; justify-content: space-between; margin-top: 15px; }
</style>
