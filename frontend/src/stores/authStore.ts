import { ref, computed } from 'vue';
import { defineStore } from 'pinia';

import type { User } from '@/types/stores/userAuth';


export const useAuthStore = defineStore('auth', () => {

    // --- STATE
    const loading = ref<boolean>(false);
    
    // Initialize user from localStorage
    const storedUserData = localStorage.getItem('user_data');
    const token = ref<string | null>(localStorage.getItem('user_token'));
    const user = ref<User | null>(storedUserData ? JSON.parse(storedUserData) : null);


    // --- GETTERS
    const userName = computed(() => user.value?.username || '??');
    const isAuthenticated = computed(() => !!token.value && !!user.value);


    // --- ACTIONS
    async function setToken (key: string) {
        token.value = key;
        localStorage.setItem('user_token', key);
    }

    async function setUser(data: User) {
        user.value = data;
        localStorage.setItem('user_data', JSON.stringify(data));
    }

    async function logout()
    {
        user.value = null;
        token.value = null;
        localStorage.removeItem('user_data');
        localStorage.removeItem('user_token');
    }

    async function fetchCurrentUser()
    {
        loading.value = true;
        try {
        // Her ville du vanligvis hatt et API-kall
        // const res = await axios.get('/api/user/me');
        // user.value = res.data;
        } finally {
        loading.value = false;
        }
    }

    return { 
        user, userName, loading,  isAuthenticated, 
        setUser, setToken, logout, fetchCurrentUser 
    };
});