<template>
    <h2>Dashboard</h2>
    {{ data }}

    <ProfileBar :data="data"/>

    <section>
        <h2> Mine Teams</h2>
    </section>

    <section>
        <h2> Discover new Teams</h2>
    </section>
</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { computed } from 'vue';

    import type { DashboardProps} from '@/types/props';
    // --- Props Definition Logic
    const props = defineProps<DashboardProps>();
    const data = computed(() => props.data);
    console.log(data.value)

</script>
