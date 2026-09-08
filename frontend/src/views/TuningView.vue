<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import { useToast } from "vue-toastification"

const authStore = useAuthStore()
const toast = useToast()
const categories = ref<any[]>([])
const selectedCategoryId = ref('')
const parts = ref<any[]>([])
const loading = ref(false)

const showAddForm = ref(false)
const newPartName = ref('')
const newVendorCode = ref('')
const newMarketplaceLink = ref('')

onMounted(async () => {
  try {
    const res = await api.get('/vehicles/categories')
    categories.value = res.data
    if (categories.value.length > 0) {
      selectedCategoryId.value = categories.value[0].id
    }
  } catch (err) {
    console.error("Failed to load categories", err)
  }
})

watch(selectedCategoryId, async (newVal) => {
  if (newVal) {
    await fetchParts(newVal)
  }
})

const fetchParts = async (categoryId: string) => {
  loading.value = true
  try {
    const res = await api.get(`/parts?categoryId=${categoryId}`)
    parts.value = res.data
  } catch (err) {
    console.error("Failed to fetch parts", err)
  } finally {
    loading.value = false
  }
}

const submitNewPart = async () => {
  if (!authStore.isAuthenticated()) {
    toast.error("Нужно войти в систему!")
    return
  }

  try {
    await api.post('/parts', {
      vehicleCategoryId: selectedCategoryId.value,
      partName: newPartName.value,
      vendorCode: newVendorCode.value,
      marketplaceLink: newMarketplaceLink.value
    }, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    })
    
    showAddForm.value = false
    newPartName.value = ''
    newVendorCode.value = ''
    newMarketplaceLink.value = ''
    showAddForm.value = false
    toast.success("Деталь добавлена!")
    await fetchParts(selectedCategoryId.value)
  } catch (err) {
    console.error("Failed to add part", err)
    toast.error("Ошибка при добавлении детали.")
  }
}
</script>

<template>
  <div class="tuning-container">
    <h2>База Тюнинга и Запчастей</h2>
    <p>Народная база проверенных запчастей и тюнинга. Выберите класс техники.</p>
    
    <div class="category-selector glass-panel">
      <select v-model="selectedCategoryId" class="input-field">
        <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
      </select>
      <button v-if="authStore.isAuthenticated()" @click="showAddForm = !showAddForm" class="btn-primary" style="margin-left: 10px;">
        + Добавить Деталь
      </button>
    </div>

    <form v-if="showAddForm" @submit.prevent="submitNewPart" class="glass-panel form-panel">
      <div class="form-group">
        <label>Название детали (например: Защита радиатора ARMA)</label>
        <input v-model="newPartName" type="text" required class="input-field" />
      </div>
      <div class="form-group">
        <label>Артикул / Партномер (Опционально)</label>
        <input v-model="newVendorCode" type="text" class="input-field" />
      </div>
      <div class="form-group">
        <label>Ссылка на магазин (Опционально)</label>
        <input v-model="newMarketplaceLink" type="url" class="input-field" placeholder="https://ozon.ru/..." />
      </div>
      <button type="submit" class="btn-primary">Сохранить деталь в базу</button>
    </form>

    <div v-if="loading" class="spinner"></div>
    <div v-else class="parts-grid">
      <div v-for="part in parts" :key="part.id" class="part-card glass-panel">
        <h3>{{ part.partName }}</h3>
        <p v-if="part.vendorCode"><strong>Артикул:</strong> {{ part.vendorCode }}</p>
        <a v-if="part.marketplaceLink" :href="part.marketplaceLink" target="_blank" class="shop-link">Где купить? 🛒</a>
      </div>
      <div v-if="parts.length === 0" class="no-data">
        В этой категории еще нет добавленных деталей. Станьте первым!
      </div>
    </div>
  </div>
</template>

<style scoped>
.tuning-container { padding: 80px 20px 20px; max-width: 1000px; margin: 0 auto; color: var(--text-light); }
.category-selector { padding: 15px; border-radius: 12px; margin: 20px 0; display: flex; align-items: center; }
.input-field { padding: 10px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.2); background: rgba(0,0,0,0.5); color: white; outline: none; flex: 1; }
.form-panel { padding: 25px; border-radius: 12px; margin-bottom: 20px; }
.form-group { margin-bottom: 15px; display: flex; flex-direction: column; gap: 5px; }
.parts-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 20px; margin-top: 20px; }
.part-card { padding: 20px; border-radius: 12px; transition: transform 0.2s; display: flex; flex-direction: column; gap: 10px; }
.part-card:hover { transform: translateY(-3px); }
.part-card h3 { color: var(--primary-color); margin: 0; }
.shop-link { color: var(--accent-primary); text-decoration: none; font-weight: bold; margin-top: auto; }
.shop-link:hover { text-decoration: underline; }
.no-data { padding: 20px; color: rgba(255,255,255,0.5); text-align: center; grid-column: 1 / -1; }
</style>
