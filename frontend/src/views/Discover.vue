<template>
    <section class="teams-container">
        <h2>Teams</h2>
        <p v-if="userName">
            Hei, <b>{{ userName }}</b>. Her er teamene du kan bli med i.
        </p>
        <p v-else>
            Her er teamene du kan bli med i.
        </p>

        <section v-if="loading" class="teams-placeholder">
            <p>Laster teams…</p>
        </section>

        <section v-else-if="error" class="teams-placeholder">
            <p>{{ error }}</p>
        </section>

        <section v-else-if="teams.length === 0" class="teams-placeholder">
            <p>Ingen teams å vise enda.</p>
        </section>

       <ul v-else class="teams-list">
    <li v-for="team in teams" :key="team.id" class="team-card">
        <h3>{{ team.name }}</h3>
        <p v-if="team.description">{{ team.description }}</p>
        <div
            v-for="group in groupedTags(team)"
            :key="group.category"
            class="tag-group"
        >
            <span class="tag-group-label">{{ group.category }}</span>
            <ul class="team-tags">
                <li v-for="tag in group.tags" :key="tag.id">{{ tag.name }}</li>
            </ul>
        </div>
        <button
            v-if="team.isOpenToJoinRequests"
            @click="joinTeam(team.id)"
            :disabled="joiningTeamId === team.id">
            {{ joiningTeamId === team.id ? 'Blir med...' : 'Bli med' }}
        </button>
        <p v-else class="closed-team">Dette teamet er stengt for nye medlemmer</p>
    </li>
</ul>
    </section>
</template>

<script lang="ts" setup>
    import { onMounted, ref } from 'vue';
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';
    import { useTagsStore } from '@/stores/tagsStore';

    interface TeamTag {
        id: string;
        name: string;
    }

    interface TeamListItem {
        id: string;
        name: string;
        description?: string | null;
        isOpenToJoinRequests: boolean;
        createdBy: string;
        createdAt: string;
        tags: TeamTag[];
    };

    const authStore = useAuthStore();
    const { userName, user } = storeToRefs(authStore);
    const tagsStore = useTagsStore();

    // Tags are grouped into a separate row per top-level category (all
    // rows share one uniform color), at Swati's explicit request,
    // overriding Terje's "no special code for any category" instruction
    // from the 25.08 review. Worth confirming with Terje directly if this
    // divergence is meant to stick.

    function topLevelAncestorName(tagId: string): string {
        let current = tagsStore.getById(tagId);
        while (current?.parentId) {
            current = tagsStore.getById(current.parentId);
        }
        return current?.name ?? 'Annet';
    }

    interface TagGroup {
        category: string;
        tags: TeamTag[];
    }

    function groupedTags(team: TeamListItem): TagGroup[] {
        const byCategory = new Map<string, TeamTag[]>();

        for (const tag of team.tags) {
            const category = topLevelAncestorName(tag.id);
            if (!byCategory.has(category)) byCategory.set(category, []);
            byCategory.get(category)!.push(tag);
        }

        return Array.from(byCategory.entries()).map(([category, tags]) => ({ category, tags }));
    }

    const teams = ref<TeamListItem[]>([]);
    const loading = ref<boolean>(true);
    const error = ref<string | null>(null);
    const joiningTeamId = ref<string | null>(null);

    async function fetchTeams() {
    loading.value = true;
    error.value = null;

    try {
        await tagsStore.ensureLoaded();

        const baseApi = import.meta.env.VITE_BASE_API;
        const discordId = user.value?.id;
        const query = discordId ? `?discordId=${encodeURIComponent(discordId)}` : '';

        const response = await fetch(`${baseApi}/api/discover/available${query}`);
        if (!response.ok) throw new Error('Kunne ikke hente teams.');

        const payload = await response.json();
        const rows = payload.map((team: any) => ({
            id: team.Id ?? team.id,
            name: team.Name ?? team.name,
            description: team.Description ?? team.description,
            isOpenToJoinRequests: team.IsOpenToJoinRequests ?? team.isOpenToJoinRequests,
            createdBy: team.CreatedBy ?? team.createdBy,
            createdAt: team.CreatedAt ?? team.createdAt,
            tags: (team.Tags ?? team.tags ?? []).map((t: any) => ({
                id: t.Id ?? t.id,
                name: t.Name ?? t.name,
            })),
        }));

        teams.value = rows;
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Ukjent feil.';
    } finally {
        loading.value = false;
    }
}

    async function joinTeam(teamId: string) {
        if (!user.value?.id) {
            error.value = 'Du må være logget inn for å bli med i et team.';
            return;
        }

        joiningTeamId.value = teamId;
        error.value = null;

        try {
            const baseApi = import.meta.env.VITE_BASE_API;
            const response = await fetch(`${baseApi}/api/discover/${teamId}/request`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ discordId: user.value.id }),
            });

            if (!response.ok) {
                let message = 'Kunne ikke sende forespørsel til team.';
                try {
                    const errorBody = await response.json();
                    if (errorBody?.message) message = errorBody.message;
                } catch { /* response wasn't JSON, keep generic message */ }
                throw new Error(message);
            }

            await fetchTeams();
            error.value = 'Forespørsel sendt til team!';
        } catch (err) {
            error.value = err instanceof Error ? err.message : 'Ukjent feil.';
        } finally {
            joiningTeamId.value = null;
        }
    }

    onMounted(fetchTeams);
</script>

<style scoped>
    .teams-container {
        padding: 1rem 0;
    }

    .teams-list {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 1rem;
        margin-top: 1rem;
        list-style: none;
        padding: 0;
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

    .team-card h3 {
        margin: 0 0 0.5rem 0;
    }

    .team-card p {
        margin: 0 0 0.5rem 0;
        font-size: 0.95rem;
        color: #666;
    }

    .tag-group {
        margin-bottom: 0.75rem;
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
        padding: 0;
        list-style: none;
        margin: 0;
    }

    .team-tags li {
        background-color: #eef1f5;
        color: #33475b;
        padding: 0.3rem 0.65rem;
        border-radius: 6px;
        font-size: 0.85rem;
        font-weight: 500;
        border: 1px solid #dde3ea;
    }

    .team-card button {
        padding: 0.4rem 0.75rem;
        border-radius: 6px;
        border: 1px solid #0f5ed8;
        background: #0f5ed8;
        color: #fff;
    }

    .team-card button:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    .closed-team {
        color: #999;
        font-size: 0.9rem;
    }

    .teams-placeholder {
        padding: 1rem 0;
        color: #666;
    }
</style>
