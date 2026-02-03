<template>
  <section>
    <h2>Team</h2>
    <p>Team ID: {{ teamId }}</p>

    <section class="requests">
      <h3>Forespørsler</h3>

      <p v-if="requestsLoading">Laster forespørsler…</p>
      <p v-else-if="requestsError">{{ requestsError }}</p>
      <p v-else-if="requests.length === 0">Ingen forespørsler.</p>

      <ul v-else class="requests-list">
        <li v-for="request in requests" :key="request.id" class="request-item">
          <div class="request-info">
            <strong>{{ request.invitedUser?.username ?? 'Ukjent bruker' }}</strong>
            <span class="request-meta">ID: {{ request.invitedUserId }}</span>
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
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

type TeamRequest = {
  id: number;
  teamId: number;
  invitedUserId: number;
  status: string;
  invitedAt: string;
  invitedUser?: {
    id: number;
    username?: string | null;
    discordId?: string | null;
  } | null;
};

const route = useRoute();
const authStore = useAuthStore();
const { user } = storeToRefs(authStore);
const teamId = computed(() => Number(route.params.teamId));

const requests = ref<TeamRequest[]>([]);
const requestsLoading = ref(false);
const requestsError = ref<string | null>(null);
const actionRequestId = ref<number | null>(null);

async function fetchRequests() {
  requestsLoading.value = true;
  requestsError.value = null;

  try {
    const baseApi = import.meta.env.VITE_BASE_API;
    const response = await fetch(`${baseApi}/api/discover/${teamId.value}/requests`);
    if (!response.ok) {
      throw new Error('Kunne ikke hente forespørsler.');
    }

    requests.value = await response.json();
  } catch (err) {
    requestsError.value = err instanceof Error ? err.message : 'Ukjent feil.';
  } finally {
    requestsLoading.value = false;
  }
}

async function approveRequest(requestId: number) {
  if (!user.value?.id) {
    requestsError.value = 'Du må være logget inn som admin.';
    return;
  }

  actionRequestId.value = requestId;
  try {
    const baseApi = import.meta.env.VITE_BASE_API;
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
  } catch (err) {
    requestsError.value = err instanceof Error ? err.message : 'Ukjent feil.';
  } finally {
    actionRequestId.value = null;
  }
}

async function declineRequest(requestId: number) {
  if (!user.value?.id) {
    requestsError.value = 'Du må være logget inn som admin.';
    return;
  }

  actionRequestId.value = requestId;
  try {
    const baseApi = import.meta.env.VITE_BASE_API;
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
  } catch (err) {
    requestsError.value = err instanceof Error ? err.message : 'Ukjent feil.';
  } finally {
    actionRequestId.value = null;
  }
}

onMounted(fetchRequests);
</script>
