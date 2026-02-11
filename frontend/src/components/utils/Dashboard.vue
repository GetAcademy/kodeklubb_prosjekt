<template>
    <section class="dashboard">
        <h2>Dashboard</h2>
        <section v-if="data">
            Velkommen, <b>{{ data.username }}</b>
        </section>

        <ProfileBar v-if="data" :data="data"/>

        <section v-if="teams && teams.length > 0" class="teams-section">
            <h2>Mine Teams</h2>
            <section class="teams-grid">
                <article v-for="team in teams" :key="team.id" class="team-card">
                    <header class="team-card-header">
                        <h3>{{ team.name }}</h3>
                        <RouterLink class="team-link" :to="`/teams/${team.id}`">Open</RouterLink>
                    </header>
                    <p v-if="team.description">{{ team.description }}</p>
                    <section v-if="team.tags && team.tags.length" class="team-tags">
                        <span v-for="tag in team.tags" :key="tag" class="team-tag">{{ tag }}</span>
                    </section>
                </article>
            </section>
        </section>
        <section v-else>
            <p>Du er ikke medlem av noen teams enda.</p>
        </section>
    </section>
</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { computed } from 'vue';
    import type { DashboardProps} from '@/types/props';

    // --- Props Definition Logic
    const props = defineProps<DashboardProps>();
    const data = computed(() => props.data);
    const teams = computed(() => props.teams || [])

    //  --  Debug Logic
    //console.log(data.value)

    
</script>

<style scoped>
    .teams-section {
        margin-top: 1.5rem;
    }

    .teams-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 1rem;
        margin-top: 1rem;
    }

    .team-card {
        border: 1px solid #ddd;
        border-radius: 10px;
        padding: 1rem;
        background: #fff;
        transition: box-shadow 0.2s ease;
    }

    .team-card:hover {
        box-shadow: 0 6px 16px rgba(0, 0, 0, 0.08);
    }

    .team-card-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 0.5rem;
    }

    .team-link {
        font-size: 0.85rem;
        text-decoration: none;
        color: #0f5ed8;
        border: 1px solid #0f5ed8;
        padding: 0.25rem 0.6rem;
        border-radius: 999px;
    }

    .team-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-top: 0.75rem;
    }

    .team-tag {
        display: inline-block;
        background-color: #f0f0f0;
        padding: 0.25rem 0.5rem;
        border-radius: 4px;
        font-size: 0.85rem;
    }
</style>
