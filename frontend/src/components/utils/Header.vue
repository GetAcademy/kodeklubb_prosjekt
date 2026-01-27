<template>

    <button v-if="!isLoggedIn" @click="loginWithDiscord" class = 'discord-btn'> This is a login button for discord</button>

</template>
<script setup lang="ts">

    // --- Importing Dependencies
    import axios from 'axios';
    import { computed } from 'vue';


    const meta = import.meta.env;
    // --- Discord client API Logic
    const apiUrl =  meta.VITE_DISCORD_API_LOGIN;
    const isLoggedIn = computed(() => false);

    async function discordCallBack()
    {
        try{
            const response = await axios.get('/auth/discord/callback');

            localStorage.setItem('user_token', response.data.token);
            console.log(response)

        } catch (e) {console.error(e)}
    }
    async function loginWithDiscord()
    {
        
        const BASE_API = meta.VITE_C_SERVER;
        const LOGIN_API = `${BASE_API}${meta.VITE_LOGIN_API}`;

        window.location.href = LOGIN_API;

        try {
            
            discordCallBack();

        } catch (e){
            console.error('Innlogging feilet :', e);
        }

    }
    // -- Debugging Logic
    console.log("connecting to :", apiUrl);
</script>>