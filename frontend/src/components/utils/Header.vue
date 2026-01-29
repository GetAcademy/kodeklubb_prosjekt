<template>
    <p> Logo</p>
    <NavigationMenu v-if="isAuthenticated" :data="authMenu"/>
    <NavigationMenu v-else :data="menu"/>
    <h1> GET - Kode Klubb</h1>
    

</template>
<script setup lang="ts">

    // --- Importing Dependencies & Types
    import { storeToRefs } from 'pinia';    
    import { useAuthStore } from '@/stores/authStore';

    // --- State Management
    const authStore = useAuthStore();
    const { isAuthenticated } = storeToRefs(authStore);

    const meta = import.meta.env;
    const BASE_API = meta.VITE_C_SERVER;
    const discordAPI = `${BASE_API}${meta.VITE_LOGIN_API}`;

    const menu = 
    [
        { type: 'button', label:"Logg inn with Discord", action: () => loginDiscord()}
    ]

    const authMenu =
    [
        { type: 'router', path:"/profile", label:"Min Side"},
        { type: 'button', path:"/logout", label:"Logg deg ut", action: () => handleLogout() }
    ];
 
    async function  loginDiscord() { window.location.href = discordAPI;}
    function handleLogout () { authStore.logout();}

</script>