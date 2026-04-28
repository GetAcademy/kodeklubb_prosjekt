<template>
  <section class="requests-container">
    <h2>My Join Requests</h2>

    <section v-if="loading" class="requests-placeholder">
      <p>Loading requests…</p>
    </section>

    <section v-else-if="error" class="requests-placeholder">
      <p class="error">{{ error }}</p>
    </section>

    <section v-else-if="requests.length === 0" class="requests-placeholder">
      <p>You have no join requests yet.</p>
    </section>

    <ul v-else class="requests-list">
      <li v-for="req in requests" :key="req.id" class="request-card">
        <div class="request-info">
          <h3>{{ req.teamName }}</h3>
          <p class="request-date">Sent: {{ formatDate(req.invitedAt) }}</p>
          <span class="status-badge" :class="req.status">{{ req.status }}</span>
        </div>

        <button
          v-if="req.status === 'pending'"
          class="cancel-btn"
          :disabled="cancellingId === req.id"
          @click="cancelRequest(req.teamId, req.id)"
        >
          {{ cancellingId === req.id ? 'Cancelling...' : 'Cancel Request' }}
        </button>
      </li>
    </ul>
  </section>
</template>

<script lang="ts" setup>
import { onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

interface JoinRequest {
  id: string;
  teamId: string;
  teamName: string;
  status: string;
  invitedAt: string;
}

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

const requests = ref<JoinRequest[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);
const cancellingId = ref<string | null>(null);

async function fetchMyRequests() {
  loading.value = true;
  error.value = null;

  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const discordId = user.value?.id;

    if (!discordId) {
      error.value = 'You must be logged in to view your requests.';
      return;
    }

    const response = await fetch(
      `${baseApi}/api/discover/my-requests?discordId=${encodeURIComponent(discordId)}`
    );

    if (!response.ok) throw new Error('Could not load your requests.');

    const payload = await response.json();
    const rows = Array.isArray(payload) ? payload : (payload?.value ?? []);

    requests.value = rows.map((row: any) => ({
      id: row.id,
      teamId: row.teamId ?? row.team_id,
      teamName: row.teamName ?? row.team_name ?? 'Unknown Team',
      status: row.status,
      invitedAt: row.invitedAt ?? row.invited_at,
    }));
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unknown error.';
  } finally {
    loading.value = false;
  }
}

async function cancelRequest(teamId: string, requestId: string) {
  cancellingId.value = requestId;
  error.value = null;

  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const discordId = user.value?.id;

    const response = await fetch(
      `${baseApi}/api/discover/${teamId}/requests/${requestId}?discordId=${encodeURIComponent(discordId!)}`,
      { method: 'DELETE' }
    );

    if (!response.ok) throw new Error('Could not cancel request.');

    requests.value = requests.value.filter(r => r.id !== requestId);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unknown error.';
  } finally {
    cancellingId.value = null;
  }
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });
}

onMounted(fetchMyRequests);
</script>

<style scoped>
.requests-container {
  padding: 1rem 0;
}

.requests-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
  margin-top: 1rem;
  list-style: none;
  padding: 0;
}

.request-card {
  border: 1px solid #ddd;
  border-radius: 10px;
  padding: 1rem;
  background: #fff;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  transition: box-shadow 0.2s ease;
}

.request-card:hover {
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.08);
}

.request-info h3 {
  margin: 0 0 0.25rem 0;
}

.request-date {
  font-size: 0.85rem;
  color: #888;
  margin: 0 0 0.5rem 0;
}

.status-badge {
  display: inline-block;
  padding: 0.2rem 0.6rem;
  border-radius: 20px;
  font-size: 0.8rem;
  font-weight: 600;
  text-transform: capitalize;
}

.status-badge.pending {
  background: #fff3cd;
  color: #856404;
}

.status-badge.approved {
  background: #d1e7dd;
  color: #0f5132;
}

.status-badge.declined {
  background: #f8d7da;
  color: #842029;
}

.cancel-btn {
  padding: 0.4rem 0.75rem;
  border-radius: 6px;
  border: 1px solid #dc3545;
  background: #fff;
  color: #dc3545;
  cursor: pointer;
  font-size: 0.9rem;
  transition: background 0.2s;
}

.cancel-btn:hover:not(:disabled) {
  background: #dc3545;
  color: #fff;
}

.cancel-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.requests-placeholder {
  padding: 1rem 0;
  color: #666;
}

.error {
  color: #842029;
}
</style>
