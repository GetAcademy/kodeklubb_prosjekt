<template>
    <section class="dashboard">
        <h2>Dashboard</h2>

        <!-- Safe check for data -->
        <section v-if="data">
            Velkommen, <b>{{ data.username }}</b>
        </section>

        <ProfileBar v-if="data" :data="data" />

        <!-- Safe check for teams -->
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

        <!-- Safe fallback -->
        <section v-else>
            <p>Du er ikke medlem av noen teams enda.</p>
        </section>
    </section>
</template>

<script lang="ts" setup>
import { computed } from 'vue';

// ⭐ FIX: Make props optional
const props = defineProps({
    data: { type: Object, required: false },
    teams: { type: Array, required: false, default: () => [] }
});

// Safe computed values
const data = computed(() => props.data || null);
const teams = computed(() => props.teams || []);
</script>
