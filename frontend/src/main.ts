import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import './style.css'
import Toast from "vue-toastification";
import "vue-toastification/dist/index.css";
import App from './App.vue'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(Toast)

app.mount('#app')
