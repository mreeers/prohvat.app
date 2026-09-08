import axios from 'axios';

// Используем относительный путь '/api', который проксируется:
// - в режиме разработки (npm run dev) через vite.config.ts
// - в production (в докере) через nginx.conf
const api = axios.create({
    baseURL: '/api'
});

export default api;
