<template>

    <button v-if="!isLoggedIn" @click="loginWithDiscord" class = 'discord-btn'> This is a login button for discord</button>

</template>
<script setup lang="ts">

    // --- Importing Dependencies
    import axios from 'axios';
    import { computed, onMounted } from 'vue';


    const meta = import.meta.env;
    // --- Discord client API Logic
    const apiUrl =  meta.VITE_DISCORD_API_LOGIN;
    const isLoggedIn = computed(() => false);

    onMounted(() => {
        // Check if user data is in URL (after Discord redirect)
        const urlParams = new URLSearchParams(window.location.search);
        const token = urlParams.get('token');
        const userDataEncoded = urlParams.get('user');
        
        if (token && userDataEncoded) {
            const userData = JSON.parse(decodeURIComponent(userDataEncoded));
            console.log('Discord User Information:', userData);
            localStorage.setItem('user_token', token);
            localStorage.setItem('user_data', JSON.stringify(userData));
            
            // Clean up URL
            window.history.replaceState({}, document.title, window.location.pathname);
        }
        
        // Check if user is already logged in
        const storedUserData = localStorage.getItem('user_data');
        if (storedUserData) {
            console.log('Logged in user:', JSON.parse(storedUserData));
        }
    });

    async function loginWithDiscord()
    {
        
        const BASE_API = meta.VITE_C_SERVER;
        const LOGIN_API = `${BASE_API}${meta.VITE_LOGIN_API}`;

        window.location.href = LOGIN_API;

    }
    // -- Debugging Logic
    console.log("connecting to :", apiUrl);
</script>