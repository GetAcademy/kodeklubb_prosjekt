<template>
    <button @click="loginWithDiscord()" class = 'discord-btn'> This is a login button for discord</button>
</template>
<script setup lang="ts">

    // --- Importing Dependencies
    import axios from 'axios';
    import { useRoute, useRouter } from 'vue-router';

    // --- Discord client API Logic
    const apiUrl =  import.meta.env.VITE_DISCORD_API_LOGIN;

    const router = useRouter();


    async function discordCallBack()
    {
        try{
            const response = await axios.get('/auth/discord/callback');
            return response;
        } catch (e) {console.log(e)}
    }
    async function loginWithDiscord()
    {
        const meta = import.meta.env;
        const BASE_API = meta.C_SERVER;
        //const API_CLIENT = `${BASE_API}`.join(import.meta.env.VITE_LOGIN_API);

        const apiUrl =  import.meta.env.VITE_DISCORD_API_LOGIN;
        const clientID = import.meta.env.VITE_DISCORD_CLIENT_ID;
        const clientSecret = import.meta.env.VITE_DISCORD_CLIENT_SECRET;
        

        const payload =
        {
            header:
            {
                clientId:clientID,
                clientSecret: clientSecret
            }
            
        }

        try {
            const response = await axios.post("http://localhost:5154/auth/discord/login", payload);
            discordCallBack();
            localStorage.setItem('user_token', response.data.token);
            router.push('/dashboard');

            console.log("test")
        } catch (e){
            console.error('Innlogging feilet', e);
        }

    }
    // -- Debugging Logic
    console.log("connecting to :", apiUrl);
</script>>