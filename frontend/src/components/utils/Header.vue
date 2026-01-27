<template>
    <button @click="loginWithDiscord" class = 'discord-btn'> This is a login button for discord</button>
</template>
<script setup lang="ts">

    // --- Importing Dependencies
    import axios from 'axios';
    import { useRoute, useRouter } from 'vue-router';

    // --- Discord client API Logic
    const apiUrl =  import.meta.env.DISCORD_API_LOGIN;
    const clientID = import.meta.env.DISCORD_CLIENT_ID;
    const clientSecret = import.meta.env.DISCORD_CLIENT_SECRET;

    const route = useRoute();
    const router = useRouter();


    async function loginWithDiscord()
    {
        const code = route.query.code;

        if (code)
        {
            try {
                const response = await axios.post('/auth/discord/login', {code});
                localStorage.setItem('user_token', response.data.token);
                router.push('/dashboard');
            } catch (e){
                console.error('Innlogging feilet', e);
            }
        }
        console.log(code)
    }
    // -- Debugging Logic
    console.log("connecting to :", apiUrl);
</script>>