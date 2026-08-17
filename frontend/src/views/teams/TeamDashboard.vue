<template>
    <div class="dashboard">
        <NavigationMenu :data="menu" :cls="teamMenuCls" />

        <section class="dashboard-body">
            <header class="team-header">
                <div class="team-header-top">
                    <h2 class="team-name">{{ teamDetails?.name ?? 'Team' }}</h2>
                    <span class="team-id-chip">
                        <span class="team-id-hash">#</span>{{ teamId }}
                    </span>
                </div>

                <p v-if="teamLoading" class="status-line muted">
                    <span class="dot pulse"></span>Laster teamdetaljer…
                </p>
                <p v-else-if="teamError" class="status-line error">
                    <span class="dot error-dot"></span>{{ teamError }}
                </p>
                <p v-else class="team-description">{{ teamDetails?.description }}</p>
            </header>

            <section v-if="teamDetails?.discordLink" class="discord-section">
                <div class="discord-icon">◆</div>
                <div class="discord-copy">
                    <h3>Discord Community</h3>
                    <p>Bli med i vår Discord-server for å chatte og samarbeide med teamet</p>
                </div>
                <a :href="teamDetails.discordLink" target="_blank" rel="noopener noreferrer" class="btn-discord">
                    Åpne Discord →
                </a>
            </section>

            <section class="requests">
                <div class="requests-heading">
                    <h3>Forespørsler</h3>
                    <span v-if="requests.length" class="requests-count">{{ requests.length }}</span>
                </div>

                <p v-if="requestsLoading" class="status-line muted">
                    <span class="dot pulse"></span>Laster forespørsler…
                </p>
                <p v-else-if="requestsError" class="status-line error">{{ requestsError }}</p>
                <p v-else-if="requestsSuccess" class="status-line success">{{ requestsSuccess }}</p>
                <p v-else-if="requests.length === 0" class="empty-state">
                    Ingen ventende forespørsler akkurat nå.
                </p>

                <ul v-else class="requests-list">
                    <li v-for="request in requests" :key="request.id" class="request-item">
                        <div class="request-avatar">
                            {{ (request.invitedUser?.username ?? '?').charAt(0).toUpperCase() }}
                        </div>
                        <div class="request-info">
                            <strong>{{ request.invitedUser?.username ?? 'Ukjent bruker' }}</strong>
                            <span class="request-meta">Discord ID · {{ request.invitedUser?.discordId }}</span>
                        </div>
                        <div class="request-actions">
                            <button
                                class="btn-approve"
                                @click="approveRequest(request.id)"
                                :disabled="actionRequestId === request.id"
                            >
                                {{ actionRequestId === request.id ? 'Godkjenner…' : 'Godkjenn' }}
                            </button>
                            <button
                                class="btn-decline"
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
    </div>
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

    const teamLinkOrder: Record<string, number> = {
        '/teams/:teamId': 0,
        '/teams/:teamId/members': 1,
        '/teams/:teamId/news': 2
    };

    const teamMenuCls = [
        ['nav-bar'],
        ['nav-list', 'flex-wrap-row-align-content-start-justify-space-between'],
        ['nav-item'],
        ['nav-link']
    ];

    const menu = computed(() => {
        return router.getRoutes()
            .filter(route => route.meta?.isTeam)
            .sort((a, b) => (teamLinkOrder[a.path] ?? Number.MAX_SAFE_INTEGER) - (teamLinkOrder[b.path] ?? Number.MAX_SAFE_INTEGER))
            .map(route => {
                const routeName = route.name?.toString() || 'Unknown';
                // Replace :teamId in path with actual teamId
                let path = route.path;
                if (teamId.value) {
                    path = path.replace(':teamId', teamId.value);
                }
                return { type: 'router', path, label: toTitleCase(routeName), cls: 'router-btn' };
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

    // Extracts a human-readable error message from a failed fetch Response.
    // Falls back to the provided default if the body isn't JSON or has no message field.
    async function extractErrorMessage(response: Response, fallback: string): Promise<string> {
        try {
            const errorPayload = await response.json();
            return errorPayload?.Message ?? errorPayload?.message ?? fallback;
        } catch {
            return fallback;
        }
    }

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
        const url = `${baseApi}/api/discover/${teamId.value}/requests`;

        const response = await fetch(url);

        if (!response.ok) {
            const message = await extractErrorMessage(response, 'Kunne ikke hente forespørsler.');
            throw new Error(message);
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
            const message = await extractErrorMessage(response, 'Kunne ikke godkjenne forespørsel.');
            throw new Error(message);
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
            const message = await extractErrorMessage(response, 'Kunne ikke avslå forespørsel.');
            throw new Error(message);
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
        const res = await fetch(url);
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

<style scoped>
/* ---- Design tokens ---- */
.dashboard {
  --surface: #ffffff;
  --panel: #f7f8fa;
  --panel-border: #e2e5eb;
  --text-primary: #1a1d24;
  --text-muted: #6b7280;
  --accent: #3b5fd9;
  --accent-soft: rgba(59, 95, 217, 0.08);
  --amber: #d9862f;
  --success: #1d9a6c;
  --success-soft: rgba(29, 154, 108, 0.1);
  --danger: #d9433d;
  --danger-soft: rgba(217, 67, 61, 0.08);
  --discord: #5865f2;

  font-family: 'Inter', 'Segoe UI', system-ui, sans-serif;
  background: var(--surface);
  color: var(--text-primary);
  min-height: 100vh;
  padding-bottom: 4rem;
}

/* ---- Nav (targets NavigationMenu's rendered markup) ---- */
.dashboard :deep(.nav-bar) {
  background: var(--panel);
  border-bottom: 1px solid var(--panel-border);
  padding: 0 2rem;
}

.dashboard :deep(.nav-list) {
  list-style: none;
  display: flex;
  gap: 0.75rem;
  margin: 0;
  padding: 1rem 0;
}

.dashboard :deep(.nav-item) {
  list-style: none;
}

.dashboard :deep(.nav-link) {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.55rem 1.15rem;
  border-radius: 8px;
  color: var(--text-muted);
  text-decoration: none;
  font-size: 0.92rem;
  font-weight: 700;
  letter-spacing: 0.01em;
  transition: background 0.15s ease, color 0.15s ease;
}

.dashboard :deep(.nav-link:hover) {
  background: var(--accent-soft);
  color: var(--text-primary);
}

.dashboard :deep(.router-link-active) {
  background: var(--accent-soft);
  color: var(--accent);
}

/* ---- Body ---- */
.dashboard-body {
  max-width: 780px;
  margin: 0 auto;
  padding: 2.5rem 1.5rem 0;
}

.team-header {
  margin-bottom: 2rem;
}

.team-header-top {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  flex-wrap: wrap;
  margin-bottom: 0.75rem;
}

.team-name {
  font-family: 'IBM Plex Mono', 'Consolas', monospace;
  font-size: 1.9rem;
  font-weight: 700;
  margin: 0;
  letter-spacing: -0.01em;
}

.team-id-chip {
  font-family: 'IBM Plex Mono', 'Consolas', monospace;
  font-size: 0.78rem;
  color: var(--text-muted);
  background: var(--panel);
  border: 1px solid var(--panel-border);
  border-radius: 999px;
  padding: 0.3rem 0.75rem;
}

.team-id-hash {
  color: var(--accent);
  margin-right: 0.15rem;
}

.team-description {
  color: var(--text-muted);
  font-size: 1rem;
  line-height: 1.6;
  margin: 0;
}

.status-line {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.92rem;
  margin: 0;
}

.status-line.muted { color: var(--text-muted); }
.status-line.error { color: var(--danger); }
.status-line.success { color: var(--success); }

.dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--accent);
  flex-shrink: 0;
}

.dot.pulse {
  animation: pulse 1.4s ease-in-out infinite;
}

.dot.error-dot {
  background: var(--danger);
}

@keyframes pulse {
  0%, 100% { opacity: 0.35; }
  50% { opacity: 1; }
}

@media (prefers-reduced-motion: reduce) {
  .dot.pulse { animation: none; opacity: 1; }
}

/* ---- Discord panel ---- */
.discord-section {
  display: flex;
  align-items: center;
  gap: 1.25rem;
  margin: 0 0 2rem;
  padding: 1.25rem 1.5rem;
  border: 1px solid var(--panel-border);
  border-radius: 12px;
  background: var(--panel);
}

.discord-icon {
  flex-shrink: 0;
  width: 44px;
  height: 44px;
  border-radius: 10px;
  background: rgba(88, 101, 242, 0.15);
  color: var(--discord);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
}

.discord-copy {
  flex: 1;
  min-width: 180px;
}

.discord-copy h3 {
  margin: 0 0 0.2rem;
  font-size: 1rem;
  font-weight: 700;
}

.discord-copy p {
  margin: 0;
  color: var(--text-muted);
  font-size: 0.88rem;
}

.btn-discord {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  padding: 0.6rem 1.1rem;
  background: var(--discord);
  color: white;
  text-decoration: none;
  border-radius: 8px;
  font-weight: 600;
  font-size: 0.88rem;
  transition: background 0.15s ease, transform 0.15s ease;
}

.btn-discord:hover {
  background: #4752c4;
  transform: translateY(-1px);
}

/* ---- Requests ---- */
.requests {
  border-top: 1px solid var(--panel-border);
  padding-top: 1.75rem;
}

.requests-heading {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 1.1rem;
}

.requests-heading h3 {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
}

.requests-count {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 0.75rem;
  background: var(--accent-soft);
  color: var(--accent);
  padding: 0.1rem 0.5rem;
  border-radius: 999px;
}

.empty-state {
  color: var(--text-muted);
  font-size: 0.92rem;
  padding: 1.5rem;
  text-align: center;
  border: 1px dashed var(--panel-border);
  border-radius: 10px;
}

.requests-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.request-item {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  padding: 0.85rem 1rem;
  background: var(--panel);
  border: 1px solid var(--panel-border);
  border-radius: 10px;
}

.request-avatar {
  flex-shrink: 0;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: var(--accent-soft);
  color: var(--accent);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 0.9rem;
}

.request-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}

.request-info strong {
  font-size: 0.94rem;
  font-weight: 600;
}

.request-meta {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 0.72rem;
  color: var(--text-muted);
}

.request-actions {
  flex-shrink: 0;
  display: flex;
  gap: 0.5rem;
}

.btn-approve, .btn-decline {
  border: 1px solid transparent;
  border-radius: 7px;
  padding: 0.45rem 0.85rem;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s ease, opacity 0.15s ease;
}

.btn-approve {
  background: var(--success-soft);
  color: var(--success);
  border-color: rgba(52, 211, 153, 0.3);
}

.btn-approve:hover:not(:disabled) {
  background: rgba(52, 211, 153, 0.22);
}

.btn-decline {
  background: var(--danger-soft);
  color: var(--danger);
  border-color: rgba(243, 114, 114, 0.25);
}

.btn-decline:hover:not(:disabled) {
  background: rgba(243, 114, 114, 0.18);
}

.btn-approve:disabled, .btn-decline:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

@media (max-width: 560px) {
  .request-item {
    flex-wrap: wrap;
  }
  .request-actions {
    width: 100%;
    justify-content: flex-end;
  }
}
</style>