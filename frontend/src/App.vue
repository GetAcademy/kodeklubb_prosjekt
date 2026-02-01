<template>
    <header>
        <UtilsHeader />
    </header>
    <main>
        <RouterView />
    </main>
    <footer>
        <UtilsFooter />
    </footer>
</template>
<script lang="ts">
    import { onMounted } from 'vue';


    onMounted(() => {
        cleanURL();
    });

    function cleanURL ()
    {
        const urlParams = new URLSearchParams(window.location.search);
        console.log("url parms", urlParams.get('error'))
        // Check if user data is in URL (after Discord redirect)

        const code = urlParams.get('code');
        if (code)
        {
            // Redirect to backend API
            const backendAPIBase = import.meta.env.VITE_BASE_API;
            window.location.href = `${backendAPIBase}/auth/discord/callback?code=${encodeURIComponent(code)}`;
        }
    }
</script>