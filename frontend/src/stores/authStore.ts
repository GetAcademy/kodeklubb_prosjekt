import { ref, computed } from 'vue';
import { defineStore } from 'pinia';

import type { User } from '@/types/stores/userAuth';


export const useAuthStore = defineStore('auth', () => {

    const rawUserData = computed(() => localStorage.getItem('user_data'));

    const userData = computed(() => {
        if (!rawUserData.value) return null;
        return JSON.parse(rawUserData.value) as User;
    });

    // --- STATE
    const loading = ref<boolean>(false);
    const user = ref<User | null>(userData.value);
    const token = ref<string | null>(localStorage.getItem('user_token'));
    


    // --- GETTERS
    const userName = computed(() => user.value?.username || '??');
    const isAuthenticated = computed(() => !!token.value && user.value !== null);


    // --- ACTIONS
    async function setToken (key: string) { localStorage.setItem('user_token', key); }
    async function setUser(data: User) { localStorage.setItem('user_data', JSON.stringify(data)); }

    function logout()
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
