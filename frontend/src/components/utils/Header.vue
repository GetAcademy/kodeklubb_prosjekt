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


    async function discordCallBack()
    {
        try{
            const response = await axios.get('/auth/discord/callback');
        } catch (e) {console.log(e)}
    }
    async function loginWithDiscord()
    {
        const code = route.query.code;

        if (code)
        {
            try {
                const response = await axios.post('/auth/discord/login', {code});
                localStorage.setItem('user_token', response.data.token);
                router.push('/dashboard');
                discordCallBack();
            } catch (e){
                console.error('Innlogging feilet', e);
            }
        }
    }
    // -- Debugging Logic
    console.log("connecting to :", apiUrl);
</script>>