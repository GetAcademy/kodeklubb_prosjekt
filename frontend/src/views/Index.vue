

<template>
    <section v-if="!!isAuthenticated && user">
        <p v-if="userTeamsLoading" class="loading">Laster teams...</p>
        <p v-else-if="userTeamsError" class="error">{{ userTeamsError }}</p>
        <UtilsDashboard v-else :data="user" :teams="userTeams" />
    </section>

</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { storeToRefs } from 'pinia';
    import { onMounted, ref, watch } from 'vue';
    import { useAuthStore } from '@/stores/authStore';

    const authStore = useAuthStore();
    const { user, isAuthenticated } = storeToRefs(authStore);

    const userTeams = ref<any[]>([]);
    const userTeamsLoading = ref(false);
    const userTeamsError = ref('');
    const lastFetchedDiscordId = ref<string | null>(null);

    const fetchUserTeams = async () => {
        userTeamsLoading.value = true;
        userTeamsError.value = '';
        try {
            const baseApi = import.meta.env.VITE_BASE_API || '';
            const response = await fetch(`${baseApi}/api/discover/my-teams?discordId=${user.value?.id}`);
            if (!response.ok) {
                throw new Error('Failed to fetch user teams');
            }
            const payload = await response.json();
           const rows = Array.isArray(payload) ? payload : (payload?.value ?? []);
userTeams.value = rows.map((team: any) => ({
    id: team.Id ?? team.id,
    name: team.Name ?? team.name,
    description: team.Description ?? team.description,
    isOpenToJoinRequests: team.IsOpenToJoinRequests ?? team.isOpenToJoinRequests,
    createdBy: team.CreatedBy ?? team.createdBy,
    createdAt: team.CreatedAt ?? team.createdAt,
    tags: team.Tags ?? team.tags ?? [],
}));
        } catch (error) {
            userTeamsError.value = error instanceof Error ? error.message : 'An error occurred';
        } finally {
            userTeamsLoading.value = false;
        }
    };

    onMounted(async () => {
        if (isAuthenticated.value && user.value?.id) {
            await fetchUserTeams();
        }
    });

    watch([isAuthenticated, user], async () => {
        const discordId = user.value?.id || null;
        if (!isAuthenticated.value || !discordId) return;
        if (lastFetchedDiscordId.value === discordId) return;

        lastFetchedDiscordId.value = discordId;
        await fetchUserTeams();
    }, { immediate: true });
</script>

<style scoped>
    .teams-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 1rem;
        margin-top: 1rem;
    }

    .team-card {
        border: 1px solid #ddd;
        border-radius: 8px;
        padding: 1rem;
        transition: box-shadow 0.2s;
    }

    .team-card:hover {
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .team-card a {
        text-decoration: none;
        color: inherit;
    }

    .team-card h3 {
        margin: 0 0 0.5rem 0;
    }

    .team-card p {
        margin: 0 0 0.5rem 0;
        font-size: 0.9rem;
        color: #666;
    }

    .tags {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
    }

    .tag {
        display: inline-block;
        background-color: #f0f0f0;
        padding: 0.25rem 0.5rem;
        border-radius: 4px;
        font-size: 0.85rem;
    }

    .empty {
        color: #999;
        padding: 2rem;
        text-align: center;
    }

    .error {
        color: #d32f2f;
        padding: 1rem;
        background-color: #ffebee;
        border-radius: 4px;
    }

    .loading {
        padding: 1rem;
        text-align: center;
        color: #666;
    }
</style>