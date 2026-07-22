<script setup lang="ts">
import { useAppStore } from '../stores/app'
import MapComponent from '../components/MapComponent.vue'

const appStore = useAppStore()
</script>

<template>
  <div class="home-view">
    <MapComponent :season="appStore.currentSeason" />

    <div class="floating-controls glass-panel">
      <div class="season-toggle" @click="appStore.toggleSeason">
        <div class="toggle-track" :class="appStore.currentSeason">
          <div class="toggle-thumb">
            <span v-if="appStore.currentSeason === 'enduro'">🏍️</span>
            <span v-else>❄️</span>
          </div>
        </div>
      </div>
      <div class="season-label">
        {{ appStore.currentSeason === 'enduro' ? 'Enduro Season' : 'Snowmobile Season' }}
      </div>
    </div>
  </div>
</template>

<style scoped>
.home-view {
  position: relative;
  width: 100vw;
  height: 100vh;
}

.floating-controls {
  position: absolute;
  bottom: 32px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 1000;
  padding: 16px 24px;
  display: flex;
  align-items: center;
  gap: 16px;
  border-radius: 100px;
}

.season-toggle {
  cursor: pointer;
}

.toggle-track {
  width: 80px;
  height: 40px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 20px;
  position: relative;
  transition: all 0.3s ease;
}

.toggle-track.enduro {
  border: 1px solid var(--accent-primary);
  box-shadow: inset 0 0 10px rgba(255, 77, 0, 0.2);
}

.toggle-track.snowmobile {
  border: 1px solid var(--accent-secondary);
  box-shadow: inset 0 0 10px rgba(0, 210, 255, 0.2);
}

.toggle-thumb {
  position: absolute;
  top: 4px;
  left: 4px;
  width: 32px;
  height: 32px;
  background: white;
  border-radius: 50%;
  display: flex;
  justify-content: center;
  align-items: center;
  transition: all 0.3s cubic-bezier(0.68, -0.55, 0.265, 1.55);
  font-size: 1.2rem;
}

.toggle-track.snowmobile .toggle-thumb {
  transform: translateX(40px);
}

.season-label {
  font-weight: 600;
  font-family: var(--font-heading);
  min-width: 150px;
}
</style>
