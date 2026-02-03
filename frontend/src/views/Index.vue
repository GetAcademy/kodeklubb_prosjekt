

<template>
    <section v-if="!!isAuthenticated && user">
        <h2>Dashboard</h2>
        <p> 
            Velkommen til Kodeklubben, <b>{{ userName }}</b> - <b>{{ user.email }}</b>
        </p>
            {{ user }}

        <ProfileBar :data="user"/>

        <section>
            <h2>Dine Teams</h2>
            <div v-if="userTeamsLoading" class="loading">Laster inn...</div>
            <div v-else-if="userTeamsError" class="error">{{ userTeamsError }}</div>
            <div v-else-if="userTeams.length === 0" class="empty">Du er ikke medlem av noen team ennå</div>
            <div v-else class="teams-grid">
                <div v-for="team in userTeams" :key="team.id" class="team-card">
                    <RouterLink :to="`/teams/team${team.id}`">
                        <h3>{{ team.name }}</h3>
                        <p>{{ team.description }}</p>
                        <div class="tags">
                            <span v-for="tag in team.tags" :key="tag" class="tag">{{ tag }}</span>
                        </div>
                    </RouterLink>
                </div>
            </div>
        </section>

        <section>
            <h2>Discover new Teams</h2>
            <RouterLink to="/discover">Finn team å bli med i</RouterLink>
        </section>
    </section>
    <section v-else>
        {{ isAuthenticated }}
    </section>
</template>


<script lang="ts" setup>
    import { ref, onMounted } from 'vue';
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';
    import { RouterLink } from 'vue-router';

    const authStore = useAuthStore();
    const { user, isAuthenticated, userName } = storeToRefs(authStore);

    const userTeams = ref<any[]>([]);
    const userTeamsLoading = ref(false);
    const userTeamsError = ref('');

    onMounted(async () => {
        if (isAuthenticated.value && user.value?.id) {
            await fetchUserTeams();
        }
    });

    const fetchUserTeams = async () => {
        userTeamsLoading.value = true;
        userTeamsError.value = '';
        try {
            const response = await fetch(`/api/discover/my-teams?discordId=${user.value?.id}`);
            if (!response.ok) {
                throw new Error('Failed to fetch user teams');
            }
            userTeams.value = await response.json();
        } catch (error) {
            userTeamsError.value = error instanceof Error ? error.message : 'An error occurred';
        } finally {
            userTeamsLoading.value = false;
        }
    };
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