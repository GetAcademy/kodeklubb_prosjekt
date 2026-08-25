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
                    <section v-if="(team.tags ?? []).length" class="team-tags">
                        <span
                            v-for="tag in team.tags"
                            :key="tag.id"
                            class="team-tag"
                            :class="tagCategoryClass(tag.id)"
                        >{{ tag.name }}</span>
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

    import { computed } from 'vue';
    import type { DashboardProps} from '@/types/props';
    import NotificationBell from '../NotificationBell.vue';
    import { useTagsStore } from '@/stores/tagsStore';

    const props = defineProps<DashboardProps>();
    const data = computed(() => props.data);
    const teams = computed(() => props.teams || [])

    // Each of the 4 top-level nodes gets its own distinct color, at
    // Swati's explicit request, overriding Terje's "no special code for
    // any category" instruction from the 25.08 review. Worth confirming
    // with Terje directly if this divergence is meant to stick.
    const tagsStore = useTagsStore();

    // Maps a top-level node's name to a CSS class. Adding a 5th top-level
    // node later just needs one more line here — nothing else changes.
    const CATEGORY_CLASSES: Record<string, string> = {
        'Anvendelsesomrade': 'tag-anvendelse',
        'Geografi': 'tag-geografi',
        'Programmeringssprak': 'tag-proglang',
        'Faglig niva': 'tag-faglig',
    };

    function topLevelAncestorName(tagId: string): string | undefined {
        let current = tagsStore.getById(tagId);
        while (current?.parentId) {
            current = tagsStore.getById(current.parentId);
        }
        return current?.name;
    }

    function tagCategoryClass(tagId: string): string {
        const topName = topLevelAncestorName(tagId);
        return (topName && CATEGORY_CLASSES[topName]) || 'tag-default';
    }

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
        padding: 0.3rem 0.65rem;
        border-radius: 6px;
        font-size: 0.85rem;
        font-weight: 500;
        border: 1px solid transparent;
    }

    .tag-default {
        background-color: #eef1f5;
        color: #33475b;
        border-color: #dde3ea;
    }

    .tag-anvendelse {
        background-color: #efe9fb;
        color: #5b3aa8;
        border-color: #d9cdf3;
    }

    .tag-geografi {
        background-color: #dcf5ec;
        color: #0c6b52;
        border-color: #b8e6d4;
    }

    .tag-proglang {
        background-color: #fde8e0;
        color: #a3411a;
        border-color: #f7c7b3;
    }

    .tag-faglig {
        background-color: #fbe7ef;
        color: #a3245a;
        border-color: #f4c3d9;
    }
</style>
