<template>
    <h2>Dashboard</h2>
    <p> 
        Velkommen til Kodeklubben, <b>{{ data.name }}</b> - <b>{{ data.email }}</b>
    </p>
    {{ data }}

    <ProfileBar :data="data"/>

    <section>
        <h2> Dine Teams </h2>
    </section>

    <section>
        <h2> Discover new Teams</h2>
    </section>
</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { computed } from 'vue';

    // --- Props Definition Logic
    interface DashboardProps
    {
        data: any;
    }
    const props = defineProps<DashboardProps>();
    const data = computed(() => props.data);
    console.log(data.value)

</script>
