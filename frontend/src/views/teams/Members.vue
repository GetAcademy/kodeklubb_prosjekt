<template>
  <NavigationMenu :data="menu" />
  <div class="members">
    <h1>Team Members</h1>
    <p class="muted">Team ID: {{ teamId }}</p>

    <p v-if="loading">Laster medlemmer…</p>
    <p v-else-if="error" class="error">{{ error }}</p>
    <p v-else-if="members.length === 0">Ingen medlemmer funnet.</p>

    <ul v-else class="members-list">
      <li v-for="member in members" :key="member.id" class="member-item">
        <img
          v-if="member.avatarUrl"
          :src="member.avatarUrl"
          :alt="`Avatar for ${member.username}`"
          class="avatar"
        />
        <div class="member-info">
          <strong>{{ member.username }}</strong>
          <span>Discord: {{ member.discordId }}</span>
          <span>Rolle: {{ member.role }}</span>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();

type TeamMember = {
  id: string;
  teamId: string;
  userId: string;
  role: string;
  status: string;
  joinedAt: string;
  username: string;
  discordId: string;
  avatarUrl: string | null;
};

const teamId = computed(() => route.params.teamId as string);

const menu = computed(() => {
  return router.getRoutes().filter(r => r.meta?.isTeam).map(r => {
    const routeName = r.name?.toString() || 'Unknown';
    const resolvedPath = r.path
      .replace(':teamId', teamId.value)
      .replace(':id', teamId.value);

    return {
      type: 'router',
      path: resolvedPath,
      label: toTitleCase(routeName),
      cls: 'router-btn'
    };
  });
});

const members = ref<TeamMember[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

function toTitleCase(str: string) {
  return str.replace(/\w\S*/g, txt => txt.charAt(0).toUpperCase() + txt.substring(1).toLowerCase());
}

async function fetchMembers() {
  loading.value = true;
  error.value = null;

  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const response = await fetch(`${baseApi}/api/discover/${teamId.value}/members`);

    if (!response.ok) {
      throw new Error('Kunne ikke hente medlemmer.');
    }

    const payload = await response.json();
    const rows = Array.isArray(payload) ? payload : [];

    members.value = rows.map((row: any) => ({
      id: row.id,
      teamId: row.teamId ?? row.team_id,
      userId: row.userId ?? row.user_id,
      role: row.role,
      status: row.status,
      joinedAt: row.joinedAt ?? row.joined_at,
      username: row.username,
      discordId: row.discordId ?? row.discord_id,
      avatarUrl: row.avatarUrl ?? row.avatar_url ?? null
    }));
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Ukjent feil.';
  } finally {
    loading.value = false;
  }
}

onMounted(fetchMembers);

watch(() => route.params.teamId, () => {
  fetchMembers();
});
</script>

<style scoped>
.members {
  padding: 2rem;
}

.members-list {
  list-style: none;
  padding: 0;
  margin-top: 1rem;
  display: grid;
  gap: 0.75rem;
}

.member-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem;
  border: 1px solid;
  border-radius: 8px;
}

.member-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 999px;
  object-fit: cover;
}

.muted {
  opacity: 0.75;
}

</style>