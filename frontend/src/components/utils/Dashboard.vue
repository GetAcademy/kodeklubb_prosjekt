<template>
    
    <h2>Dashboard</h2>
    <section>
        Velkommen, <b>{{ data.username }}</b>
    </section>

    <ProfileBar :data="data"/>

    <section v-if="teams.length > 0">

        <h2> Mine Teams</h2>
        <section v-for="team in teams" :key="team.id">
            
            <NavigationMenu :data="menu" />
            <section v-if="team.tags && team.tags.length" class="team-tags">
                <span v-for="tag in team.tags" :key="tag" class="team-tag">{{ tag }}</span>
            </section>
            <p>{{ team.description }}</p>
        </section>
        
    </section>
    <section v-else>
        <p>Du er ikke medlem av noen teams enda.</p>
    </section>

</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { computed } from 'vue';
    import type { DashboardProps} from '@/types/props';

    // --- Props Definition Logic
    const props = defineProps<DashboardProps>();
    const data = computed(() => props.data);
    const teams = computed(() => props.teams)

    const menu = computed(() => { return props.teams.map(team => ({ type: 'router', path: `/teams/${team.id}`, label: toTitleCase(team.name), cls: 'dash-btn'})); });

    function toTitleCase(str: string) { return str.replace(/\w\S*/g, (txt) => { return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase(); } );}

    //  --  Debug Logic
    //console.log(data.value)

</script>
