<template>
    <NavigationMenu :data="menu" />
    <section>
    <h2>{{ teamDetails?.name ?? 'Team' }}</h2>
    <p class="muted">Team ID: {{ teamId }}</p>
    <p v-if="teamLoading">Laster teamdetaljer…</p>
    <p v-else-if="teamError">{{ teamError }}</p>
    <p v-else class="team-description">{{ teamDetails?.description }}</p>

    <section class="requests">
      <h3>Forespørsler</h3>

    <p v-if="requestsLoading">Laster forespørsler…</p>
    <p v-else-if="requestsError" class="error">{{ requestsError }}</p>
    <p v-else-if="requestsSuccess" class="success">{{ requestsSuccess }}</p>
      <p v-else-if="requests.length === 0">Ingen forespørsler.</p>

      <ul v-else class="requests-list">
        <li v-for="request in requests" :key="request.id" class="request-item">
          <div class="request-info">
            <strong>{{ request.invitedUser?.username ?? 'Ukjent bruker' }}</strong>
            <span class="request-meta">Discord: {{ request.invitedUser?.discordId }}</span>
          </div>
          <div class="request-actions">
            <button
              @click="approveRequest(request.id)"
              :disabled="actionRequestId === request.id"
            >
              {{ actionRequestId === request.id ? 'Godkjenner…' : 'Godkjenn' }}
            </button>
            <button
              @click="declineRequest(request.id)"
              :disabled="actionRequestId === request.id"
            >
              {{ actionRequestId === request.id ? 'Avslår…' : 'Avslå' }}
            </button>
          </div>
        </li>
      </ul>
    </section>
  </section>
</template>
<script setup lang="ts">

    // --- Importing Dependencies & Types
    import { storeToRefs } from 'pinia';
    import { useRoute, useRouter } from 'vue-router';
    import { computed, onMounted, ref } from 'vue';
    import { useAuthStore } from '@/stores/authStore';

    // --- Router Logic
    const router = useRouter()
    const route = useRoute()

    const menu = computed(() =>
    {
        return router.getRoutes().filter(route => route.meta?.isTeam).map(route =>
        {
            const routeName = route.name?.toString() || 'Unknown';
            return { type: 'router', path: route.path, label: toTitleCase(routeName), cls:'router-btn'};
        });
    });

    function toTitleCase(str: string) { return str.replace(/\w\S*/g, (txt) => { return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase(); } );}

    type TeamRequest = {
    id: string;
    teamId: string;
    invitedUserId: string;
    status: string;
    invitedAt: string;
    invitedUser?: {
        id: string;
        username?: string | null;
        discordId?: string | null;
    } | null;
    };
  
    // console.log(route)
    const authStore = useAuthStore();
    const { user } = storeToRefs(authStore);
    const teamId = computed(() => route.params.teamId as string);

    const requests = ref<TeamRequest[]>([]);
    const requestsLoading = ref(false);
    const requestsError = ref<string | null>(null);
    const actionRequestId = ref<string | null>(null);
    const requestsSuccess = ref<string | null>(null);
    const teamDetails = ref<any | null>(null);
    const teamLoading = ref(false);
    const teamError = ref<string | null>(null);

    async function fetchRequests() {
    requestsLoading.value = true;
    requestsError.value = null;
    requestsSuccess.value = null;

    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const response = await fetch(`${baseApi}/api/discover/${teamId.value}/requests`);
        if (!response.ok) {
        throw new Error('Kunne ikke hente forespørsler.');
        }

        const payload = await response.json();
        const rows = Array.isArray(payload) ? payload : (payload?.value ?? []);
        requests.value = rows.map((row: any) => ({
            id: row.id,
            teamId: row.team_id ?? row.teamId,
            invitedUserId: row.invited_user_id ?? row.invitedUserId,
            status: row.status,
            invitedAt: row.invited_at ?? row.invitedAt,
            invitedUser: {
                id: row.invited_user_id ?? row.invitedUserId,
                username: row.username ?? row.invitedUser?.username ?? null,
                discordId: row.discord_id ?? row.invitedUser?.discordId ?? null
            }
        }));
    } catch (err) {
        requestsError.value = err instanceof Error ? err.message : 'Ukjent feil.';
    } finally {
        requestsLoading.value = false;
    }
    }

    async function approveRequest(requestId: string) {
    if (!user.value?.id) {
        requestsError.value = 'Du må være logget inn som admin.';
        return;
    }

    actionRequestId.value = requestId;
    requestsError.value = null;
    requestsSuccess.value = null;
    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const response = await fetch(
        `${baseApi}/api/discover/${teamId.value}/requests/${requestId}/approve`,
        {
            method: 'PATCH',
            headers: {
            'Content-Type': 'application/json',
            },
            body: JSON.stringify({ discordId: user.value.id }),
        }
        );

        if (!response.ok) {
        throw new Error('Kunne ikke godkjenne forespørsel.');
        }

        await fetchRequests();
        requestsSuccess.value = 'Foresporsel godkjent.';
    } catch (err) {
        requestsError.value = err instanceof Error ? err.message : 'Ukjent feil.';
    } finally {
        actionRequestId.value = null;
    }
    }

    async function declineRequest(requestId: string) {
    if (!user.value?.id) {
        requestsError.value = 'Du må være logget inn som admin.';
        return;
    }

    actionRequestId.value = requestId;
    requestsError.value = null;
    requestsSuccess.value = null;
    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const response = await fetch(
        `${baseApi}/api/discover/${teamId.value}/requests/${requestId}/decline`,
        {
            method: 'PATCH',
            headers: {
            'Content-Type': 'application/json',
            },
            body: JSON.stringify({ discordId: user.value.id }),
        }
        );

        if (!response.ok) {
        throw new Error('Kunne ikke avslå forespørsel.');
        }

        await fetchRequests();
        requestsSuccess.value = 'Foresporsel avslatt.';
    } catch (err) {
        requestsError.value = err instanceof Error ? err.message : 'Ukjent feil.';
    } finally {
        actionRequestId.value = null;
    }
    }

    async function fetchTeamDetails() {
    teamLoading.value = true;
    teamError.value = null;
    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const discordId = user.value?.id;
        const url = `${baseApi}/api/discover/${teamId.value}` + (discordId ? `?discordId=${discordId}` : '');
        console.log(url)
        const res = await fetch(url);
        console.log(res)
        if (!res.ok) {
        if (res.status === 404) {
            teamError.value = 'Team ikke funnet.';
            return;
        }
        throw new Error('Kunne ikke hente teamdetaljer.');
        }
        const payload = await res.json();
        // payload may contain { team: {...}, isMember: bool } or just team
        teamDetails.value = payload.team ?? payload;
    } catch (err) {
        teamError.value = err instanceof Error ? err.message : 'Ukjent feil.';
    } finally {
        teamLoading.value = false;
    }
    }

    onMounted(async () => {
    await fetchTeamDetails();
    await fetchRequests();
    });
</script>
