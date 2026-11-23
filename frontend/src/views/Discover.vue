<template>
    <section class="teams-container">
        <h2>Teams</h2>
        <p v-if="userName">
            Hei, <b>{{ userName }}</b>. Her er teamene du kan bli med i.
        </p>
        <p v-else>
            Her er teamene du kan bli med i.
        </p>

        <p v-if="error" class="error-banner">{{ error }}</p>

        <section v-if="loading" class="teams-placeholder">
            <p>Laster teams…</p>
        </section>

        <section v-else-if="teams.length === 0" class="teams-placeholder">
            <p>Ingen teams å vise enda.</p>
        </section>

        <ul v-else class="teams-list">
            <li v-for="team in teams" :key="team.id" class="team-card">
                <h3>{{ team.name }}</h3>
                <p v-if="team.description">{{ team.description }}</p>

                <TeamTagGroups :groups="getTeamTagGroups(team)" />

                <button v-if="team.isOpenToJoinRequests && !team.pendingRequestSentAt" @click="joinTeam(team.id)"
                    :disabled="joiningTeamId === team.id">
                    {{ joiningTeamId === team.id ? 'Blir med...' : 'Bli med' }}
                </button>
                <p v-else-if="team.pendingRequestSentAt" class="pending-request">
                    Forespørsel sendt {{ formatSentAt(team.pendingRequestSentAt) }}
                </p>
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
import { getAvailableTeams, requestToJoinTeam } from '@/api/teamsApi';
import { ApiError } from '@/api/apiClient';
import TeamTagGroups from '@/components/teams/TeamTagGroups.vue';

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
    pendingRequestSentAt?: string | null;
};

const authStore = useAuthStore();
const { userName, user } = storeToRefs(authStore);
const tagsStore = useTagsStore();

const tagGroups = [
    { rootName: 'Programmeringssprak', label: 'Programmeringsspråk', kind: 'language' },
    { rootName: 'Anvendelsesomrade', label: 'Anvendelse', kind: 'application' },
    { rootName: 'Faglig niva', label: 'Nivå', kind: 'level' },
    { rootName: 'Geografi', label: 'Geografi', kind: 'geography' },
];

function getTeamTagGroups(team: TeamListItem) {
    const tagsByRoot = new Map<string, TeamTag[]>();

    for (const tag of team.tags) {
        const root = tagsStore.getRootTag(tag.id);
        if (!root) continue;

        const groupTags = tagsByRoot.get(root.name) ?? [];
        groupTags.push(tag);
        tagsByRoot.set(root.name, groupTags);
    }

    return tagGroups
        .map(group => ({
            ...group,
            tags: tagsByRoot.get(group.rootName) ?? [],
        }))
        .filter(group => group.tags.length > 0);
}

function formatSentAt(iso: string): string {
    try {
        return new Date(iso).toLocaleString('nb-NO', { dateStyle: 'short', timeStyle: 'short' });
    } catch {
        return '';
    }
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

        const discordId = user.value?.id;

        let payload: any;
        try {
            payload = await getAvailableTeams(discordId ? String(discordId) : undefined);
        } catch {
            throw new Error('Kunne ikke hente teams.');
        }

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
            pendingRequestSentAt: team.PendingRequestSentAt ?? team.pendingRequestSentAt ?? null,
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
        try {
            await requestToJoinTeam(teamId, String(user.value.id));
        } catch (apiErr) {
            // Keep the message from the server if it sent one
            let message = 'Kunne ikke sende forespørsel til team.';
            if (apiErr instanceof ApiError) {
                try {
                    const errorBody = JSON.parse(apiErr.body);
                    if (errorBody?.message) message = errorBody.message;
                } catch { /* response wasn't JSON, keep generic message */ }
            }
            throw new Error(message);
        }

        await fetchTeams();
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
    display: flex;
    flex-direction: column;
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

.team-card button {
    margin-top: auto;
    align-self: flex-start;
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

.pending-request {
    color: #0f5ed8;
    font-size: 0.9rem;
    font-weight: 500;
}

.error-banner {
    color: #b3261e;
    font-weight: 500;
}

.teams-placeholder {
    padding: 1rem 0;
    color: #666;
}
</style>