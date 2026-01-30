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
        
        const token = urlParams.get('token');
        const userDataEncoded = urlParams.get('user');
        const code = urlParams.get('code');
        const error = urlParams.get('error');

        console.log('Index onMounted - URL Params:', {
            hasToken: !!token,
            hasUserData: !!userDataEncoded,
            hasCode: !!code,
            error: error,
            fullURL: window.location.href
        });

        // Handle errors from backend
        if (error) {
            console.error('Discord auth error:', error);
            alert('Login failed: ' + error);
            return;
        }

        // Handle Discord OAuth callback with token and user data
        if (token && userDataEncoded) {
            try {
                const parsedUserData = JSON.parse(decodeURIComponent(userDataEncoded));
                console.log('Setting token and user:', { token: token.substring(0, 20) + '...', user: parsedUserData });
                authStore.setToken(token);
                authStore.setUser(parsedUserData);
                console.log('After setUser - isAuthenticated:', authStore.isAuthenticated);
                console.log('Discord User Information:', parsedUserData);
                
                // Clean up URL
                window.history.replaceState({}, document.title, window.location.pathname);
            } catch (error) {
                console.error('Error parsing user data:', error);
            }
        } 
        // Handle authorization code from Discord
        else if (code) {
            console.log('Got code from Discord, redirecting to backend callback');
            const backendAPIBase = import.meta.env.VITE_C_SERVER;
            window.location.href = `${backendAPIBase}/auth/discord/callback?code=${encodeURIComponent(code)}`;
        }
    });
    //console.log(localStorage.getItem('user_data'))
</script>