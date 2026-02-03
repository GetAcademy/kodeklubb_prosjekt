

<template>
    <section v-if="!!isAuthenticated && user">
        <UtilsDashboard :data="user" />
    </section>
    <section v-else>
        {{ isAuthenticated }}
    </section>
</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';

    // --- State Management
    const authStore = useAuthStore();
    const { user, isAuthenticated } = storeToRefs(authStore);

    //console.log(localStorage.getItem('user_data'))
</script>