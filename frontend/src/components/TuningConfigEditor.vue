<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import api from '../services/api'
import { useToast } from 'vue-toastification'

const props = defineProps<{
  vehicleId?: string
  initialConfigJson: string | null
  categoryName?: string
  readOnly?: boolean
}>()

const emit = defineEmits(['configUpdated', 'update:modelValue'])
const toast = useToast()

const config = ref<Record<string, string>>({})
const customPairs = ref<{ key: string; value: string }[]>([])
const saving = ref(false)

// Determine vehicle profile category
const detectedCategory = computed(() => {
  const name = (props.categoryName || '').toLowerCase()
  if (name.includes('классика') || name.includes('ваз') || name.includes('дрифт')) return 'drift'
  if (name.includes('эндуро') || name.includes('питбайк') || name.includes('кросс')) return 'enduro'
  if (name.includes('снегоход')) return 'snow'
  return 'general'
})

// Category presets
const categoryPresets = computed(() => {
  if (detectedCategory.value === 'drift') {
    return [
      {
        key: 'steering',
        label: 'Выворот подвески',
        icon: '🔄',
        options: ['Красноярский выворот', 'Рычаги Турботема Дрифт', 'Рычаги We Ride JL', 'Clubturbo Спец', 'Сток']
      },
      {
        key: 'differential',
        label: 'Блокировка редуктора',
        icon: '⚙️',
        options: ['Заварка редуктора 4.1', 'Заварка 4.3 (Тяговая)', 'Заварка 3.9 (Скоростная)', 'Дисковая 2-way', 'Винтовая Val-Racing', 'Сток']
      },
      {
        key: 'handbrake',
        label: 'Гидроручник',
        icon: '🛑',
        options: ['В контур с регулятором Wilwood', 'На отдельный суппорт', 'Горизонтальный Create Tech', 'Без гидроручника']
      },
      {
        key: 'engine',
        label: 'Двигатель / Питание',
        icon: '⚡',
        options: ['1.6 Карбюратор Спорт', '1.6 Инжектор Чип', '16v 1.6 Шеснарь Атмо (130 л.с.)', '16v Шеснарь Турбо', 'Сток']
      },
      {
        key: 'chassis',
        label: 'Кузов / Безопасность',
        icon: '🛡️',
        options: ['Болтовой каркас Clubturbo + ковши', 'Распорки стаканов + пружины Нива', 'Амортизаторы SS20 Шоссе', 'Сток']
      }
    ]
  }

  if (detectedCategory.value === 'enduro') {
    return [
      {
        key: 'suspension',
        label: 'Подвеска',
        icon: '🛞',
        options: ['WP XACT Pro (тюнинг клапанов)', 'KYB Factory', 'WP Xplor стандарт', 'Ревавлинг клапанов', 'Сток']
      },
      {
        key: 'protection',
        label: 'Защита',
        icon: '🛡️',
        options: ['Комплект Арма (радиаторы + картер)', 'Защита радиаторов Hard Glide', 'Пластиковая защита Acerbis', 'Сток']
      },
      {
        key: 'tires',
        label: 'Шины и муссы',
        icon: '⭕',
        options: ['Mitas 754 + Tubliss', 'Michelin Enduro Medium + Mousse', 'Kenda Gauntlet + камеры 4mm', 'Сток']
      },
      {
        key: 'exhaust',
        label: 'Выхлопная система',
        icon: '💨',
        options: ['FMF Titanium Powercore', 'Akrapovic Racing Line', 'HGS Exhaust', 'Стоковый резонатор']
      },
      {
        key: 'fuel',
        label: 'Топливная система',
        icon: '⚡',
        options: ['TPI инжектор + прошивка TSP', 'Keihin PWK 38', 'Mikuni TMX', 'Сток']
      }
    ]
  }

  if (detectedCategory.value === 'snow') {
    return [
      {
        key: 'track',
        label: 'Гусеница / Зацеп',
        icon: '❄️',
        options: ['Зацеп 3.0" (76 мм) PowderMax', 'Зацеп 2.6" (66 мм) универсал', 'Сток']
      },
      {
        key: 'skis',
        label: 'Лыжи',
        icon: '🎿',
        options: ['Blade DS+ горные', 'Curve XS', 'Сток']
      },
      {
        key: 'bumpers',
        label: 'Бамперы и защита',
        icon: '🛡️',
        options: ['Усиленный экспедиционный бампер Voevoda', 'Защита радиатора и днища', 'Сток']
      },
      {
        key: 'riser',
        label: 'Проставка руля',
        icon: '🎮',
        options: ['Наклонная проставка 165 мм', 'Сток']
      }
    ]
  }

  return []
})

const parseInitial = () => {
  if (!props.initialConfigJson) {
    config.value = {}
    customPairs.value = []
    return
  }

  try {
    const parsed = JSON.parse(props.initialConfigJson)
    config.value = { ...parsed }
    
    // Separate standard keys from custom keys
    const presetKeys = new Set(categoryPresets.value.map(p => p.key))
    const extra: { key: string; value: string }[] = []
    
    Object.keys(parsed).forEach(k => {
      if (!presetKeys.has(k)) {
        extra.push({ key: k, value: parsed[k] })
      }
    })
    customPairs.value = extra
  } catch (e) {
    console.error('Invalid JSON config', e)
    config.value = {}
  }
}

onMounted(() => {
  parseInitial()
})

watch(() => props.initialConfigJson, () => {
  parseInitial()
})

const addCustomPair = () => {
  customPairs.value.push({ key: '', value: '' })
}

const removeCustomPair = (index: number) => {
  customPairs.value.splice(index, 1)
}

const selectPresetOption = (key: string, option: string) => {
  if (props.readOnly) return
  if (config.value[key] === option) {
    delete config.value[key]
  } else {
    config.value[key] = option
  }
}

const getFullConfigObject = () => {
  const result: Record<string, string> = { ...config.value }
  customPairs.value.forEach(p => {
    if (p.key.trim() && p.value.trim()) {
      result[p.key.trim()] = p.value.trim()
    }
  })
  return result
}

const saveConfig = async () => {
  if (!props.vehicleId) {
    const json = JSON.stringify(getFullConfigObject())
    emit('configUpdated', json)
    emit('update:modelValue', json)
    return
  }

  saving.value = true
  try {
    const configObj = getFullConfigObject()
    const jsonString = JSON.stringify(configObj)

    await api.put(`/vehicles/${props.vehicleId}/config`, {
      technicalConfigJson: jsonString
    })

    toast.success('Конфиг тюнинга успешно сохранен!')
    emit('configUpdated', jsonString)
  } catch (err) {
    console.error('Failed to save config', err)
    toast.error('Ошибка сохранения конфигуратора')
  } finally {
    saving.value = false
  }
}

const hasActiveSpecs = computed(() => {
  return Object.keys(getFullConfigObject()).length > 0
})
</script>

<template>
  <div class="tuning-configurator glass-panel">
    <div class="header-row">
      <div class="title-wrap">
        <span class="category-icon">
          {{ detectedCategory === 'drift' ? '🏎️' : detectedCategory === 'enduro' ? '🏍️' : detectedCategory === 'snow' ? '❄️' : '🔧' }}
        </span>
        <div>
          <h3>Тюнинг-Конфигуратор</h3>
          <p class="subtitle">Спек-лист доработок и ключевых компонентов боевой техники</p>
        </div>
      </div>
      <div v-if="!readOnly" class="header-actions">
        <button class="btn-save-sm" :disabled="saving" @click="saveConfig">
          {{ saving ? 'Сохранение...' : '💾 Сохранить' }}
        </button>
      </div>
    </div>

    <!-- Category Preset Options -->
    <div v-if="categoryPresets.length > 0" class="presets-section">
      <div v-for="preset in categoryPresets" :key="preset.key" class="preset-group">
        <div class="preset-label">
          <span class="preset-icon">{{ preset.icon }}</span>
          <strong>{{ preset.label }}</strong>
          <span v-if="config[preset.key]" class="current-badge">
            {{ config[preset.key] }}
          </span>
        </div>

        <div v-if="!readOnly" class="options-chips">
          <button
            v-for="opt in preset.options"
            :key="opt"
            type="button"
            class="chip-btn"
            :class="{ active: config[preset.key] === opt }"
            @click="selectPresetOption(preset.key, opt)"
          >
            {{ opt }}
          </button>
        </div>
      </div>
    </div>

    <!-- Read-only View of Active Specs -->
    <div v-if="readOnly && hasActiveSpecs" class="specs-grid">
      <div
        v-for="(val, key) in getFullConfigObject()"
        :key="key"
        class="spec-card glass-panel"
      >
        <span class="spec-name">{{ key }}:</span>
        <span class="spec-value">{{ val }}</span>
      </div>
    </div>

    <div v-else-if="readOnly && !hasActiveSpecs" class="empty-specs">
      Конфиг тюнинга пока не заполнен владельцем техники.
    </div>

    <!-- Custom Key-Value Pairs when editing -->
    <div v-if="!readOnly" class="custom-section">
      <div class="custom-header">
        <h4>Дополнительные доработки</h4>
        <button type="button" class="btn-add-param" @click="addCustomPair">+ Добавить узел</button>
      </div>

      <div v-if="customPairs.length === 0" class="no-custom">
        Нет дополнительных узлов. Нажмите «+ Добавить узел», чтобы указать нестандартные детали.
      </div>

      <div v-for="(pair, idx) in customPairs" :key="idx" class="custom-row">
        <input
          v-model="pair.key"
          placeholder="Узел (напр: Сцепление)"
          class="custom-input"
        />
        <span class="sep">—</span>
        <input
          v-model="pair.value"
          placeholder="Деталь (напр: Усиленная корзина Barnett)"
          class="custom-input full-flex"
        />
        <button type="button" class="btn-del" @click="removeCustomPair(idx)">×</button>
      </div>

      <div class="bottom-actions">
        <button
          type="button"
          class="btn-save-primary"
          :disabled="saving"
          @click="saveConfig"
        >
          {{ saving ? 'Сохранение...' : 'Сохранить спек-лист' }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tuning-configurator {
  padding: 22px;
  border-radius: 16px;
  background: rgba(26, 26, 36, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.08);
  margin: 20px 0;
}

.header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  padding-bottom: 14px;
  margin-bottom: 18px;
}

.title-wrap {
  display: flex;
  align-items: center;
  gap: 12px;
}

.category-icon {
  font-size: 2rem;
  background: rgba(255, 255, 255, 0.05);
  padding: 8px 12px;
  border-radius: 12px;
}

.title-wrap h3 {
  margin: 0;
  font-size: 1.15rem;
  color: #fff;
  font-weight: 700;
}

.subtitle {
  margin: 4px 0 0;
  font-size: 0.85rem;
  color: #9aa0a6;
}

.presets-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin-bottom: 22px;
}

.preset-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.preset-label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.95rem;
  color: #e0e0e0;
}

.current-badge {
  font-size: 0.8rem;
  background: rgba(255, 107, 0, 0.2);
  color: #ff8c42;
  border: 1px solid rgba(255, 107, 0, 0.35);
  padding: 2px 8px;
  border-radius: 20px;
  margin-left: 6px;
}

.options-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.chip-btn {
  padding: 6px 14px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 20px;
  color: #ccc;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.chip-btn:hover {
  background: rgba(255, 255, 255, 0.1);
  color: #fff;
  border-color: rgba(255, 255, 255, 0.25);
}

.chip-btn.active {
  background: linear-gradient(135deg, #ff6b00 0%, #ff8c42 100%);
  color: #fff;
  border-color: #ff8c42;
  box-shadow: 0 4px 12px rgba(255, 107, 0, 0.35);
  font-weight: 600;
}

.specs-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 12px;
}

.spec-card {
  padding: 12px 14px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.07);
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.spec-name {
  font-size: 0.78rem;
  color: #888;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.spec-value {
  font-size: 0.95rem;
  color: #fff;
  font-weight: 500;
}

.empty-specs {
  color: #888;
  font-size: 0.9rem;
  font-style: italic;
  padding: 10px 0;
}

.custom-section {
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  padding-top: 16px;
}

.custom-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.custom-header h4 {
  margin: 0;
  font-size: 0.95rem;
  color: #ddd;
}

.btn-add-param {
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #eee;
  padding: 5px 12px;
  border-radius: 8px;
  font-size: 0.8rem;
  cursor: pointer;
}

.btn-add-param:hover {
  background: rgba(255, 255, 255, 0.12);
}

.no-custom {
  color: #777;
  font-size: 0.85rem;
  margin-bottom: 12px;
}

.custom-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

.custom-input {
  padding: 8px 12px;
  background: rgba(0, 0, 0, 0.35);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 8px;
  color: #fff;
  font-size: 0.88rem;
}

.custom-input.full-flex {
  flex: 1;
}

.sep {
  color: #666;
}

.btn-del {
  background: rgba(239, 68, 68, 0.2);
  color: #ef4444;
  border: 1px solid rgba(239, 68, 68, 0.3);
  border-radius: 8px;
  width: 32px;
  height: 32px;
  cursor: pointer;
  font-size: 1.1rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-del:hover {
  background: rgba(239, 68, 68, 0.4);
}

.bottom-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.btn-save-primary {
  padding: 10px 24px;
  background: linear-gradient(135deg, #ff6b00 0%, #ff8c42 100%);
  border: none;
  border-radius: 10px;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(255, 107, 0, 0.3);
}

.btn-save-sm {
  padding: 6px 14px;
  background: rgba(255, 107, 0, 0.15);
  border: 1px solid rgba(255, 107, 0, 0.35);
  color: #ff8c42;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.85rem;
}

.btn-save-sm:hover {
  background: rgba(255, 107, 0, 0.3);
}
</style>
