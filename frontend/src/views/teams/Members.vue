<template>
  <div class="members">
    <h1>Medlemmer</h1>
    <p v-if="loading">Laster medlemmer…</p>
    <p v-else-if="error" class="error">{{ error }}</p>
    <p v-else-if="members.length === 0">Ingen medlemmer funnet.</p>
    <ul v-else>
      <li v-for="member in members" :key="member.userId" class="member-item">
        <img v-if="member.avatarUrl" :src="member.avatarUrl" alt="Profilbilde" class="avatar" />
        <div v-else class="avatar avatar--placeholder">{{ initials(member.username) }}</div>
        <div class="member-info">
          <strong>{{ member.username }}</strong>
          <span v-if="member.email" class="email">{{ member.email }}</span>
          <span class="role">{{ member.role }}</span>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const teamId = route.params.teamId as string;

const members = ref<any[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

function normalize(raw: any) {
  return {
    userId:    raw.UserId    ?? raw.user_id    ?? raw.userId,
    username:  raw.Username  ?? raw.username,
    email:     raw.Email     ?? raw.email,
    avatarUrl: raw.AvatarUrl ?? raw.avatar_url ?? raw.avatarUrl,
    role:      raw.Role      ?? raw.role,
    status:    raw.Status    ?? raw.status,
  };
}

function initials(name: string) {
  return (name ?? '?').slice(0, 2).toUpperCase();
}

async function fetchMembers() {
  loading.value = true;
  error.value = null;
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const res = await fetch(`${baseApi}/api/discover/${teamId}/members`);
    if (!res.ok) throw new Error(`Feil ${res.status}: Kunne ikke hente medlemmer.`);
    const data = await res.json();
    const rows = Array.isArray(data) ? data : (data?.value ?? []);
    members.value = rows.map(normalize);
  } catch (err: any) {
    error.value = err.message || 'Ukjent feil.';
  } finally {
    loading.value = false;
  }
}

onMounted(fetchMembers);
</script>

<style scoped>
.members {
  padding: 2rem;
  max-width: 600px;
}
ul {
  list-style: none;
  padding: 0;
  margin: 0;
}
.member-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem 0;
  border-bottom: 1px solid #eee;
}
.avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}
.avatar--placeholder {
  background: #5865f2;
  color: #fff;
  font-weight: 700;
  font-size: 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
}
.member-info {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}
.email {
  color: #666;
  font-size: 0.85rem;
}
.role {
  font-size: 0.8rem;
  color: #999;
  text-transform: capitalize;
}
.error {
  color: #b00020;
}
</style>