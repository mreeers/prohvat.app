<script setup lang="ts">
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const brand = ref('')
const model = ref('')
const year = ref<number>(new Date().getFullYear())
const categoryId = ref('') 
const categories = ref<any[]>([])
const odometerType = ref<number>(1)
const maintenanceInterval = ref<number>(1000)
const selectedFile = ref<File | null>(null)

onMounted(async () => {
  try {
    const res = await axios.get('http://localhost:8081/api/vehicles/categories')
    categories.value = res.data
    if(categories.value.length > 0) categoryId.value = categories.value[0].id
  } catch (err) {
    console.error("Failed to load categories", err)
  }
})

const handleFileChange = (e: Event) => {
  const input = e.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    selectedFile.value = input.files[0]
  }
}

const submit = async () => {
  try {
    console.log("Submitting new vehicle...")
    const formData = new FormData()
    formData.append('categoryId', categoryId.value)
    formData.append('brand', brand.value)
    formData.append('model', model.value)
    formData.append('year', year.value.toString())
    formData.append('odometerType', odometerType.value.toString())
    formData.append('maintenanceInterval', maintenanceInterval.value.toString())
    if (selectedFile.value) {
      formData.append('image', selectedFile.value)
    }

    await axios.post('http://localhost:8081/api/vehicles', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
        'Authorization': `Bearer ${authStore.token}`
      }
    })
    console.log("Vehicle saved successfully")
    router.push('/garage')
  } catch (error) {
    console.error("Failed to create vehicle", error)
  }
}
</script>

<template>
  <div class="new-vehicle-container">
    <h2>Добавить Технику</h2>
    <form @submit.prevent="submit" class="glass-panel form-panel">
      <div class="form-group">
        <label>Категория</label>
        <select v-model="categoryId" required class="input-field">
          <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
        </select>
      </div>
      <div class="form-group">
        <label>Марка (Например: KTM, Yamaha)</label>
        <input v-model="brand" type="text" required class="input-field" />
      </div>
      <div class="form-group">
        <label>Модель (Например: 300 EXC)</label>
        <input v-model="model" type="text" required class="input-field" />
      </div>
      <div class="form-group">
        <label>Год выпуска</label>
        <input v-model="year" type="number" required class="input-field" />
      </div>
      <div class="form-group">
        <label>Тип учета пробега</label>
        <select v-model="odometerType" required class="input-field">
          <option :value="1">Километры</option>
          <option :value="2">Моточасы</option>
        </select>
      </div>
      <div class="form-group">
        <label>Интервал обслуживания (ТО) в {{ odometerType === 1 ? 'км' : 'м/ч' }}</label>
        <input v-model="maintenanceInterval" type="number" required min="1" class="input-field" placeholder="Например: 15 для м/ч или 1000 для км" />
      </div>
      <div class="form-group">
        <label>Фотография техники</label>
        <input type="file" accept="image/*" @change="handleFileChange" class="file-input" />
      </div>
      <button type="submit" class="btn-primary">Сохранить</button>
    </form>
  </div>
</template>

<style scoped>
.new-vehicle-container { padding: 80px 20px 20px; max-width: 600px; margin: 0 auto; color: var(--text-light); }
.form-panel { padding: 30px; border-radius: 16px; margin-top: 20px; }
.form-group { margin-bottom: 20px; display: flex; flex-direction: column; gap: 8px; }
.input-field { padding: 12px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.2); background: rgba(0,0,0,0.5); color: white; outline: none; }
.input-field:focus { border-color: var(--primary-color); }
.file-input { color: white; }
</style>
