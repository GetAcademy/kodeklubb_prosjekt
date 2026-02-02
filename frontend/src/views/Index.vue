

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

    //console.log(localStorage.getItem('user_data'))
</script>