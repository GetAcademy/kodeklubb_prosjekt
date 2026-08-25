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
                    <section
                        v-for="group in groupedTags(team)"
                        :key="group.category"
                        class="tag-group"
                    >
                        <span class="tag-group-label">{{ group.category }}</span>
                        <div class="team-tags">
                            <span v-for="tag in group.tags" :key="tag.id" class="team-tag">{{ tag.name }}</span>
                        </div>
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

    // Tags are grouped into a separate row per top-level category (all
    // rows share one uniform color), at Swati's explicit request,
    // overriding Terje's "no special code for any category" instruction
    // from the 25.08 review. Worth confirming with Terje directly if this
    // divergence is meant to stick.
    const tagsStore = useTagsStore();

    function topLevelAncestorName(tagId: string): string {
        let current = tagsStore.getById(tagId);
        while (current?.parentId) {
            current = tagsStore.getById(current.parentId);
        }
        return current?.name ?? 'Annet';
    }

    interface TagGroup {
        category: string;
        tags: { id: string; name: string }[];
    }

    function groupedTags(team: any): TagGroup[] {
        const tags = team.tags ?? [];
        const byCategory = new Map<string, { id: string; name: string }[]>();

        for (const tag of tags) {
            const category = topLevelAncestorName(tag.id);
            if (!byCategory.has(category)) byCategory.set(category, []);
            byCategory.get(category)!.push(tag);
        }

        return Array.from(byCategory.entries()).map(([category, tags]) => ({ category, tags }));
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

    .tag-group {
        margin-top: 0.75rem;
    }

    .tag-group-label {
        display: block;
        font-size: 0.72rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.03em;
        color: #8a97a8;
        margin-bottom: 0.3rem;
    }

    .team-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
    }

    .team-tag {
        display: inline-block;
        background-color: #eef1f5;
        color: #33475b;
        padding: 0.3rem 0.65rem;
        border-radius: 6px;
        font-size: 0.85rem;
        font-weight: 500;
        border: 1px solid #dde3ea;
    }
</style>
