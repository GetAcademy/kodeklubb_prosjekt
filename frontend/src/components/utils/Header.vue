<template>

    <button v-if="!isLoggedIn" @click="loginWithDiscord()" class = 'discord-btn'> This is a login button for discord</button>

</template>
<script setup lang="ts">

    // --- Importing Dependencies
    import axios from 'axios';
import { computed } from 'vue';
    import { useRoute, useRouter } from 'vue-router';

    // --- Discord client API Logic
    const apiUrl =  import.meta.env.VITE_DISCORD_API_LOGIN;
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
        const meta = import.meta.env;
        const BASE_API = meta.VITE_C_SERVER;
        const LOGIN_API = `${BASE_API}${meta.VITE_LOGIN_API}`;
        //const API_CLIENT = `${BASE_API}`.join(import.meta.env.VITE_LOGIN_API);

        const apiUrl =  meta.VITE_DISCORD_API_LOGIN;
        

        const payload =
        {
            header:
            {
                clientId: meta.VITE_DISCORD_CLIENT_ID,
                clientSecret: meta.VITE_DISCORD_CLIENT_SECRET
            }
        }

        try {
            const response = await axios.get(LOGIN_API);
            //discordCallBack();
            isLoggedIn.value = true

            //console.log(LOGIN_API, apiUrl);
        } catch (e){
            console.error('Innlogging feilet :', e);
        }

    }
    // -- Debugging Logic
    console.log("connecting to :", apiUrl);
</script>>