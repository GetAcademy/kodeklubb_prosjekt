<template>
    
    <h2>Dashboard</h2>
    <section>
        Velkommen, <b>{{ data.username }}</b>
    </section>

    <ProfileBar :data="data"/>

    <section>
        <h2> Mine Teams</h2>
    </section>
</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { computed } from 'vue';
    import type { DashboardProps} from '@/types/props';

    // --- Props Definition Logic
    const props = defineProps<DashboardProps>();
    const data = computed(() => props.data);

    //  --  Debug Logic
    //console.log(data.value)

</script>
