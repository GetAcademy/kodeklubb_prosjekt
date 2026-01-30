<template>
{{ user }}
</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';

    // --- State Management
    const authStore = useAuthStore();
    const { user, isAuthenticated, userName } = storeToRefs(authStore);


</script>