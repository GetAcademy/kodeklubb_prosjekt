import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { auth0 } from '@/auth0';
import type { User } from '@/types/stores/userAuth';

export const useAuthStore = defineStore('auth', () => {
    const loading = ref<boolean>(false);
    const user = ref<User | null>(null);

    const userName = computed(() => user.value?.username || '??');
    const isAuthenticated = computed(() => auth0.isAuthenticated.value && !!user.value);

    function login() {
        // connection: 'discord' skips the Auth0 login box and goes straight to Discord
        return auth0.loginWithRedirect({ authorizationParams: { connection: 'discord' } });
    }

    async function logout() {
        user.value = null;
        await auth0.logout({ logoutParams: { returnTo: window.location.origin } });
    }

    async function getToken() {
        return auth0.getAccessTokenSilently();
    }

    // Gets the Kodeklubb user from the API, identified by the validated token.
    async function fetchCurrentUser() {
        if (!auth0.isAuthenticated.value) return;
        loading.value = true;
        try {
            const token = await auth0.getAccessTokenSilently();
            const baseApi = import.meta.env.VITE_BASE_API || '';
            const res = await fetch(`${baseApi}/api/users/me`, {
                headers: { Authorization: `Bearer ${token}` },
            });
            if (!res.ok) throw new Error(`/api/users/me failed: ${res.status}`);
            const body = await res.json();
            const me = body.data ?? body;
            // id = Discord ID, so pages that still send ?discordId= keep working for now
            user.value = { ...me, id: me.discordId } as unknown as User;
        } catch (e) {
            console.error(e);
            user.value = null;
        } finally {
            loading.value = false;
        }
    }

    // Resolves when Auth0 has finished loading and the user is fetched.
    let readyPromise: Promise<void> | null = null;
    function whenReady() {
        if (!readyPromise) {
            readyPromise = (async () => {
                while (auth0.isLoading.value) {
                    await new Promise((r) => setTimeout(r, 25));
                }
                if (auth0.isAuthenticated.value) await fetchCurrentUser();
            })();
        }
        return readyPromise;
    }

    return {
        user, userName, loading, isAuthenticated,
        login, logout, getToken, fetchCurrentUser, whenReady,
    };
});