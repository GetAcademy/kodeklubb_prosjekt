<template>
  <div class="notif-wrapper">
    <button class="bell-btn" @click="toggleOpen" :aria-expanded="isOpen">
      <span class="bell-icon">🔔</span>
      <span v-if="totalCount > 0" class="bell-badge">{{ totalCount }}</span>
    </button>

    <div v-if="isOpen" class="notif-panel">
      <header class="notif-header">
        <h3>Varsler</h3>
        <button class="close-btn" @click="isOpen = false">✕</button>
      </header>

      <p v-if="loading" class="notif-loading">Laster varsler...</p>
      <p v-else-if="error" class="notif-error">{{ error }}</p>
      <p v-else-if="totalCount === 0" class="notif-empty">Ingen nye varsler.</p>

      <div v-else class="notif-list">
        <section v-if="pendingApprovals.length" class="notif-section">
          <h4>Venter på din godkjenning</h4>
          <RouterLink
            v-for="item in pendingApprovals"
            :key="item.id"
            :to="`/teams/${item.teamId}`"
            class="notif-item"
            @click="isOpen = false"
          >
            <span class="notif-icon">🙋</span>
            <span class="notif-text">
              <strong>{{ item.fromUsername }}</strong> vil bli med i
              <strong>{{ item.teamName }}</strong>
            </span>
          </RouterLink>
        </section>

        <section v-if="myUpdates.length" class="notif-section">
          <h4>Oppdateringer på dine forespørsler</h4>
          <RouterLink
            v-for="item in myUpdates"
            :key="item.id"
            :to="`/teams/${item.teamId}`"
            class="notif-item"
            @click="isOpen = false"
          >
          <span class="notif-icon">{{ item.type === 'accepted' ? '✅' : '❌' }}</span>
          <span class="notif-text">
          Forespørsel til <strong>{{ item.teamName }}</strong> ble
          {{ item.type === 'accepted' ? 'godkjent' : 'avslått' }}
          </span>
          </RouterLink>
        </section>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

interface PendingApproval {
  id: string;
  teamId: string;
  teamName: string;
  fromUsername: string;
  createdAt: string;
}

interface MyUpdate {
  id: string;
  teamId: string;
  teamName: string;
  type: string;
  createdAt: string;
}

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

const isOpen = ref(false);
const loading = ref(false);
const error = ref<string | null>(null);
const pendingApprovals = ref<PendingApproval[]>([]);
const myUpdates = ref<MyUpdate[]>([]);

const totalCount = computed(() => pendingApprovals.value.length + myUpdates.value.length);

let pollHandle: ReturnType<typeof setInterval> | null = null;

async function fetchNotifications() {
  const discordId = user.value?.id;
  if (!discordId) return;

  loading.value = true;
  error.value = null;

  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const response = await fetch(
      `${baseApi}/api/discover/notifications?discordId=${encodeURIComponent(discordId)}`
    );

    if (!response.ok) throw new Error('Kunne ikke hente varsler.');

    const payload = await response.json();

    pendingApprovals.value = (payload.pendingApprovals ?? []).map((row: any) => ({
      id: row.Id ?? row.id,
      teamId: row.TeamId ?? row.teamId ?? row.teamid,
      teamName: row.TeamName ?? row.teamName ?? row.teamname,
      fromUsername: row.FromUsername ?? row.fromUsername ?? row.fromusername ?? 'Ukjent bruker',
      createdAt: row.CreatedAt ?? row.createdAt ?? row.createdat,
    }));

    myUpdates.value = (payload.myUpdates ?? []).map((row: any) => ({
      id: row.Id ?? row.id,
      teamId: row.TeamId ?? row.teamId ?? row.teamid,
      teamName: row.TeamName ?? row.teamName ?? row.teamname,
      type: row.Type ?? row.type,
      createdAt: row.CreatedAt ?? row.createdAt ?? row.createdat,
    }));
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Ukjent feil.';
  } finally {
    loading.value = false;
  }
}

function toggleOpen() {
  isOpen.value = !isOpen.value;
  if (isOpen.value) fetchNotifications();
}

onMounted(() => {
  fetchNotifications();
  pollHandle = setInterval(fetchNotifications, 30000);
});

onBeforeUnmount(() => {
  if (pollHandle) clearInterval(pollHandle);
});
</script>

<style scoped>
.notif-wrapper {
  position: relative;
  display: inline-block;
}

.bell-btn {
  position: relative;
  background: none;
  border: none;
  cursor: pointer;
  font-size: 1.4rem;
  padding: 0.4rem;
  border-radius: 50%;
  transition: background 0.15s;
  line-height: 1;
}

.bell-btn:hover {
  background: #f0f0f0;
}

.bell-badge {
  position: absolute;
  top: 0;
  right: 0;
  background: #dc3545;
  color: white;
  font-size: 0.7rem;
  font-weight: 700;
  border-radius: 999px;
  padding: 0.05rem 0.4rem;
  min-width: 1.1rem;
  text-align: center;
}

.notif-panel {
  position: absolute;
  right: 0;
  top: calc(100% + 0.5rem);
  width: 320px;
  max-height: 420px;
  overflow-y: auto;
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  z-index: 100;
}

.notif-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid #f0f0f0;
}

.notif-header h3 {
  margin: 0;
  font-size: 1rem;
}

.close-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 0.9rem;
  color: #888;
}

.notif-loading,
.notif-error,
.notif-empty {
  padding: 1rem;
  font-size: 0.9rem;
  color: #666;
  text-align: center;
}

.notif-error {
  color: #842029;
}

.notif-section {
  padding: 0.5rem 0;
}

.notif-section h4 {
  margin: 0.5rem 1rem;
  font-size: 0.8rem;
  color: #888;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.notif-item {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  padding: 0.6rem 1rem;
  text-decoration: none;
  color: #222;
  transition: background 0.15s;
}

.notif-item:hover {
  background: #f6f9ff;
}

.notif-icon {
  font-size: 1.1rem;
  flex-shrink: 0;
}

.notif-text {
  font-size: 0.9rem;
  line-height: 1.3;
}
</style>