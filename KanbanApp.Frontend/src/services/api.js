import axios from 'axios';

const BASE_URL = import.meta.env.PROD
    ? 'https://shellty-kanban.onrender.com'
    : 'http://localhost:5067';

const api = axios.create({
    baseURL: BASE_URL,
});

api.interceptors.request.use(config => {
    const token = localStorage.getItem('token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

api.interceptors.response.use(
    response => response,
    async error => {
        const original = error.config;

        if (error.response?.status === 401 && !original._retry) {
            original._retry = true;

            try {
                const refreshToken = localStorage.getItem('refreshToken');
                if (!refreshToken) throw new Error('no refresh token');

                const res = await axios.post(`${BASE_URL}/api/auth/refresh`, {
                    refreshToken
                });

                localStorage.setItem('token', res.data.accessToken);
                localStorage.setItem('refreshToken', res.data.refreshToken);

                original.headers.Authorization = `Bearer ${res.data.accessToken}`;
                return api(original);
            } catch {
                localStorage.removeItem('token');
                localStorage.removeItem('refreshToken');
                window.location.href = '/login';
            }
        }

        return Promise.reject(error);
    }
);

export default api;