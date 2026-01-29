import { ref, computed } from 'vue';
import { defineStore } from 'pinia';

import type { User } from '@/types/stores/userAuth';


export const useAuthStore = defineStore('auth', () => {

    // --- STATE ---
    const loading = ref<boolean>(false);
    const rawUserData = ref<User | any >(null);

    const user = computed(() => {
        if (!rawUserData.value) return null;
        console.log(rawUserData.value)
        console.log(localStorage.getItem('user_data'))
        return JSON.parse(rawUserData.value) as User;
    })

    const token = ref<string | null>(null);


    // --- GETTERS ---
    const userName = computed(() => user.value?.username || '??');
    const isAuthenticated = computed(() => !!token.value && user.value !== null);

    // --- ACTIONS ---
    async function setUser(data: User) {localStorage.setItem('user_data', JSON.stringify(data));}

    async function setToken (key: string) {
        //token.value = key;
        localStorage.setItem('user_token', key);}


    function logout() {
        user.value = null;
        token.value = null;
        localStorage.removeItem('user_token');
    }

    async function fetchCurrentUser() {
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
