<template>
    <section v-if="!!isAuthenticated && user">
        <h2>Dashboard</h2>
        <p> 
            Velkommen til Kodeklubben, <b>{{ userName }}</b> - <b>{{ user.email }}</b>
        </p>
            {{ user }}

        <ProfileBar :data="user"/>

        <section>
        <h2> Dine Teams </h2>
        </section>

        <section>
        <h2> Discover new Teams</h2>
        </section>
    </section>
    <section v-else>
        {{ isAuthenticated }}
    </section>
</template>


<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { onMounted } from 'vue';
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';

    // --- State Management
    const authStore = useAuthStore();
    const { user, isAuthenticated, userName } = storeToRefs(authStore);

    // --- OnMounted Logic 
    onMounted(() => {
        // Check if user data is in URL (after Discord redirect)
        const urlParams = new URLSearchParams(window.location.search);
        console.log("url parms", urlParams.get('error'))

        const token = urlParams.get('token');
        const userDataEncoded = urlParams.get('user');

        if (token && userDataEncoded && (!authStore.isAuthenticated && authStore.user))
        {
            const userData = JSON.parse(decodeURIComponent(userDataEncoded));
            authStore.setToken(token);
            authStore.setUser(userData);

            //  --- Debug logic
            //console.log("AuthStore Information:", userData)

            // Clean up URL
            window.history.replaceState({}, document.title, window.location.pathname);
        }

        // Check if user data is in URL (after Discord redirect)
        
        const code = urlParams.get('code');
        if (token && userDataEncoded)
        {
            console.log("token & UserDataEncoded")
            try 
            {
                const parsedUserData = JSON.parse(decodeURIComponent(userDataEncoded));

                authStore.setToken(token);
                authStore.setUser(parsedUserData);
                //localStorage.setItem('user_data', parsedUserData)

                //  --- Debug logic
                //console.log('Discord User Information:', parsedUserData);

                // Clean up URL
                window.history.replaceState({}, document.title, window.location.pathname);
                } catch (error) {console.error('Error parsing user data:', error);}

        } else if (code)
        {
            // Redirect to backend API
            const backendAPIBase = import.meta.env.VITE_BASE_API;
            window.location.href = `${backendAPIBase}/auth/discord/callback?code=${encodeURIComponent(code)}`;
        }
    });
    //console.log(localStorage.getItem('user_data'))
</script>