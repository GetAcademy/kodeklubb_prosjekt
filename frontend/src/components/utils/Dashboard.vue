<template>
    <section class="dashboard">
        <header class="dashboard-header">
            <h2>Dashboard</h2>
            <NotificationBell />
        </header>
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
                    <section v-if="technicalTags(team).length" class="team-tags">
                        <span v-for="tag in technicalTags(team)" :key="tag.id" class="team-tag">{{ tag.name }}</span>
                    </section>
                    <section v-if="geografiTags(team).length" class="team-tags team-tags-geo">
                        <span v-for="tag in geografiTags(team)" :key="tag.id" class="team-tag geo">🌍 {{ tag.name }}</span>
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
    import NotificationBell from '../NotificationBell.vue';
    import { useTagsStore } from '@/stores/tagsStore';

    // --- Props Definition Logic
    const props = defineProps<DashboardProps>();
    const data = computed(() => props.data);
    const teams = computed(() => props.teams || [])

    // --- Split team tags into technical vs Geografi, same pattern as Discover.vue
    const tagsStore = useTagsStore();
    const geografiId = computed(() =>
        tagsStore.tags.find(t => t.name === 'Geografi' && t.parentId === null)?.id
    );

    function isDescendantOf(tagId: string, ancestorId: string | undefined): boolean {
        if (!ancestorId) return false;
        let current = tagsStore.getById(tagId);
        while (current?.parentId) {
            if (current.parentId === ancestorId) return true;
            current = tagsStore.getById(current.parentId);
        }
        return false;
    }

    function isGeografiTag(tagId: string): boolean {
        return tagId === geografiId.value || isDescendantOf(tagId, geografiId.value);
    }

    function technicalTags(team: any): { id: string; name: string }[] {
        return (team.tags ?? []).filter((t: any) => !isGeografiTag(t.id));
    }

    function geografiTags(team: any): { id: string; name: string }[] {
        return (team.tags ?? []).filter((t: any) => isGeografiTag(t.id));
    }

    //  --  Debug Logic
    //console.log(data.value)


</script>

<style scoped>
    .dashboard-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
    }

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

    .team-tag.geo {
        background-color: #e6fbf5;
        color: #0f766e;
    }
</style>