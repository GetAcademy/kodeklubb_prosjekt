<template>
  <div class="members">
    <h1>Team Members</h1>
    <p v-if="loading">Laster medlemmer…</p>
    <p v-else-if="error" class="error">{{ error }}</p>
    <ul v-else>
      <li v-for="member in members" :key="member.UserId" class="member-item">
  <img v-if="member.AvatarUrl" :src="member.AvatarUrl" alt="Profilbilde" class="avatar" />
  <div class="member-info">
    <strong>{{ member.Username || member.UserId }}</strong>
    <span v-if="member.Email" class="email">{{ member.Email }}</span>
    <span v-if="member.Role">({{ member.Role }})</span>
    <span v-if="member.Status">- {{ member.Status }}</span>
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

async function fetchMembers() {
  loading.value = true;
  error.value = null;
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const res = await fetch(`${baseApi}/api/discover/${teamId}/members`);
    if (!res.ok) throw new Error('Kunne ikke hente medlemmer.');
    members.value = await res.json();
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
}
.member-item {
  display: flex;
  align-items: center;
  margin-bottom: 1rem;
}
.avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  margin-right: 1rem;
  object-fit: cover;
  background: #eee;
}
.member-info {
  display: flex;
  flex-direction: column;
}
.email {
  color: #666;
  font-size: 0.95em;
  margin-bottom: 0.2em;
}
.error {
  color: red;
}
</style>