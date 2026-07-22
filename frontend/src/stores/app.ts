import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAppStore = defineStore('app', () => {
  // Enduro (Summer) or Snowmobile (Winter)
  const currentSeason = ref<'enduro' | 'snowmobile'>('enduro')

  const toggleSeason = () => {
    currentSeason.value = currentSeason.value === 'enduro' ? 'snowmobile' : 'enduro'
  }

  return { currentSeason, toggleSeason }
})
