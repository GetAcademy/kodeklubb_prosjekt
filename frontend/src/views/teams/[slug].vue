<template>
    <NavigationMenu :data="menu" />
    -- innhold
</template>
<script setup lang="ts">

    // --- Importing Dependencies & Types
    import { computed } from 'vue';
    import { useRouter } from 'vue-router';  

    // --- Router Logic
    const router = useRouter()

    const menu = computed(() =>
    {
        return router.getRoutes().filter(route => route.meta?.isTeam).map(route =>
        {return { type: 'router', path: route.path, label: toTitleCase(route.name.toString()), cls:'router-btn'};});
    });

    function toTitleCase(str: string)
    {
        return str.replace(/\w\S*/g, function(txt) { return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase();});
    }
</script>