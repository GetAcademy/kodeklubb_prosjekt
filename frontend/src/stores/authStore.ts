import { ref, computed } from 'vue';
import { defineStore } from 'pinia';

import type { User } from '@/types/stores/userAuth';

// Safe storage helpers — falls back to memory if localStorage is blocked
const memoryStorage: Record<string, string> = {};

function storageGet(key: string): string | null {
    try {
        return localStorage.getItem(key) ?? sessionStorage.getItem(key);
    } catch {
        return memoryStorage[key] ?? null;
    }
}

function storageSet(key: string, value: string): void {
    try {
        localStorage.setItem(key, value);
    } catch {
        try {
            sessionStorage.setItem(key, value);
        } catch {
            memoryStorage[key] = value;
        }
    }
}

function storageRemove(key: string): void {
    try { localStorage.removeItem(key); } catch { /* ignore */ }
    try { sessionStorage.removeItem(key); } catch { /* ignore */ }
    delete memoryStorage[key];
}

export const useAuthStore = defineStore('auth', () => {

    // --- STATE
    const loading = ref<boolean>(false);

    // Initialize user from storage (safe — won't crash if blocked)
    const storedUserData = storageGet('user_data');
    const token = ref<string | null>(storageGet('user_token'));
    const user = ref<User | null>(storedUserData ? JSON.parse(storedUserData) : null);


    // --- GETTERS
    const userName = computed(() => user.value?.username || '??');
    const isAuthenticated = computed(() => !!token.value && !!user.value);


    // --- ACTIONS
    async function setToken(key: string) {
        token.value = key;
        storageSet('user_token', key);
    }

    async function setUser(data: User) {
        user.value = data;
        storageSet('user_data', JSON.stringify(data));
    }

    async function logout() {
        user.value = null;
        token.value = null;
        storageRemove('user_data');
        storageRemove('user_token');
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
        user, userName, loading, isAuthenticated,
        setUser, setToken, logout, fetchCurrentUser
    };
});