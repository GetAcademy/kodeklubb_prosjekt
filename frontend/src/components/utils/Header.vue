<template>
    <p> Logo</p>
    <NavigationMenu v-if="isAuthenticated" :data="authMenu"/>
    <NavigationMenu v-else :data="menu"/>
    <h1> GET - Kode Klubb</h1>
    {{ menu }}

</template>
<script setup lang="ts">

    // --- Importing Dependencies & Types
    import { computed } from 'vue';
    import { storeToRefs } from 'pinia'; 
    import { useRouter } from 'vue-router';  
    import { useAuthStore } from '@/stores/authStore';

    // --- State Management
    const router = useRouter()
    const authStore = useAuthStore();
    const { isAuthenticated } = storeToRefs(authStore);

    const meta = import.meta.env;
    const BASE_API = meta.VITE_BASE_API;
    const discordAPI = `${BASE_API}${meta.VITE_LOGIN_API}`;

    const authMenu = computed(() =>
    {
        return router.getRoutes().filter(route => route.meta?.requiresAuth).map(route =>
        {
            switch (route.path)
            {
                case '/': return { type: 'router', label: 'Dashboard' };
                case '/logout': return { type: 'button', label: 'Logg ut', action: () => handleLogout(), icon: 'logout' };
                default : return { type: 'router', path: route.path, label: route.name || route.path,};
            }
        });
    });

    const menu = computed(() =>
    {
        return router.getRoutes().filter(route => !route.meta?.requiresAuth).map(route => {

            if (route.path === '/') { return { type: 'button', label:"Discord login", cls:"discord-btn", action: () => loginDiscord()}}
            // Standard rute (vanlig lenke)
            return {
                type: 'router',
                path: route.path,
                label: route.name || route.path,
            };
    });
    });

    async function  loginDiscord(){window.location.href = discordAPI;}
    async function  handleLogout ()
    { 
        await authStore.logout();
        await router.replace('/');
    }

</script>