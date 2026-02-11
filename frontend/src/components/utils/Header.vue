<template>
    <p> Logo</p>
    <NavigationMenu v-if="isAuthenticated" :data="authMenu"/>
    <NavigationMenu v-else :data="menu"/>
    <h1> GET - Kode Klubb</h1>

</template>
<script setup lang="ts">

    // --- Importing Dependencies & Types
    import { computed } from 'vue';
    import { storeToRefs } from 'pinia'; 
    import { useRouter } from 'vue-router';  
    import { useAuthStore } from '@/stores/authStore';

    //  --- Endpoint Logic
    const meta = import.meta.env;
    const BASE_API = meta.VITE_BASE_API;
    const discordAPI = `${BASE_API}${meta.VITE_LOGIN_API}`;

    // ---  State Logic
    const authStore = useAuthStore();
    const { isAuthenticated } = storeToRefs(authStore);

    // --- Router Logic
    const router = useRouter()

    const authMenu = computed(() =>
    {
        return router.getRoutes().filter(route => {
            const isTeam = route.meta?.isTeam;
            const isIndex = route.path === '/';
            const isHidden = route.meta?.isHidden;
            const isPrivate = route.meta?.requiresAuth;
            
            return !isTeam && isPrivate && !isHidden || isIndex;
        }).map(route =>
        {
            switch (route.path)
            {
                case '/': return { type: 'router', path: route.path, label: toTitleCase('dashboard'), cls:'router-btn'};
                case '/logout': return { type: 'router', path: route.path, cls:"logout-btn", label: toTitleCase(route.name?.toString()), action: async(navigate) => {await authStore.logout(); navigate();}, icon: 'logout' };
                default : return { type: 'router', path: route.path, label: toTitleCase(route.name.toString()), cls:'router-btn'};
            }
        });
    });

    const menu = computed(() =>
    {
        return router.getRoutes().filter(route => !route.meta?.requiresAuth).map(route => {
            if (route.path === '/') { return { type: 'button', 
            data :{label:toTitleCase("discord"), action: () => {window.location.href = discordAPI;}}, cls:"discord-btn"}}
            return { type: 'router', path: route.path, cls: "router-btn", label: toTitleCase(route.name.toString())};
        });
    });

    function toTitleCase(str: string) { return str.replace(/\w\S*/g, (txt) => { return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase(); } );}

</script>