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
                <ul v-if="team.tags.length" class="team-tags">
                    <li v-for="tag in team.tags" :key="tag">{{ tag }}</li>
                </ul>
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

    interface TeamListItem {
        id: number;
        name: string;
        description?: string | null;
        isOpenToJoinRequests: boolean;
        createdBy: number;
        createdAt: string;
        tags: string[];
    };

    const authStore = useAuthStore();
    const { userName, user } = storeToRefs(authStore);

    const teams = ref<TeamListItem[]>([]);
    const loading = ref<boolean>(true);
    const error = ref<string | null>(null);
    const joiningTeamId = ref<number | null>(null);

    async function fetchTeams() {
        loading.value = true;
        error.value = null;

        try {
            const baseApi = import.meta.env.VITE_BASE_API;
            const discordId = user.value?.id;
            const query = discordId ? `?discordId=${encodeURIComponent(discordId)}` : '';

            const response = await fetch(`${baseApi}/api/discover/available${query}`);
            console.log(response)
            if (!response.ok) {
                throw new Error('Kunne ikke hente teams.');
            }

            teams.value = await response.json();
        } catch (err) {
            error.value = err instanceof Error ? err.message : 'Ukjent feil.';
        } finally {
            loading.value = false;
        }
    }

    async function joinTeam(teamId: number) {
        if (!user.value?.id) {
            error.value = 'Du må være logget inn for å bli med i et team.';
            return;
        }

        joiningTeamId.value = teamId;

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
                throw new Error('Kunne ikke send request til team.');
            }

            // Refresh teams list after requesting
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

    .team-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        padding: 0;
        list-style: none;
        margin: 0 0 0.75rem 0;
    }

    .team-tags li {
        background-color: #f0f0f0;
        padding: 0.25rem 0.5rem;
        border-radius: 4px;
        font-size: 0.85rem;
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
