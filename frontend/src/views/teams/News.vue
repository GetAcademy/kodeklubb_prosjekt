<template>
  <div class="news">
    <h1>Team News</h1>

    <section class="create-news" v-if="isAdmin">
      <h2>Legg til annonsering</h2>
      <form @submit.prevent="createAnnouncement">
        <div class="field">
          <label for="title">Tittel</label>
          <input id="title" v-model="title" type="text" required />
        </div>

        <div class="field">
          <label for="body">Innhold</label>
          <textarea id="body" v-model="body" rows="5" required></textarea>
        </div>

        <button type="submit" :disabled="isPosting">
          {{ isPosting ? 'Lagrer...' : 'Publiser' }}
        </button>
      </form>
    </section>

    <section class="announcements">
      <p v-if="loading">Laster annonseringer…</p>
      <p v-else-if="error" class="error">{{ error }}</p>
      <p v-else-if="announcements.length === 0">Ingen annonseringer funnet.</p>

      <TeamNewsItem
        v-for="announcement in announcements"
        :key="announcement.id"
        :announcement="announcement"
      />
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import TeamNewsItem from '@/components/teams/TeamNewsItem.vue';
import type { TeamAnnouncement } from '@/types/teams/announcement';

const route = useRoute();
const authStore = useAuthStore();

const teamId = computed(() => route.params.teamId as string);
const announcements = ref<TeamAnnouncement[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const title = ref('');
const body = ref('');
const isPosting = ref(false);

const isAdmin = computed(() => {
  return authStore.user?.id != null; // TODO: restrict further once admin info is available
});

const baseApi = import.meta.env.VITE_BASE_API || '';

function normalizeAnnouncement(item: any): TeamAnnouncement {
  return {
    id: item?.id ?? item?.Id ?? '',
    teamId: item?.teamId ?? item?.TeamId ?? teamId.value,
    createdBy: item?.createdBy ?? item?.CreatedBy ?? '',
    title: item?.title ?? item?.Title ?? '',
    body: item?.body ?? item?.Body ?? '',
    createdAt: item?.createdAt ?? item?.CreatedAt ?? '',
    updatedAt: item?.updatedAt ?? item?.UpdatedAt ?? ''
  };
}

async function fetchAnnouncements() {
  const currentTeamId = teamId.value;
  if (!currentTeamId) return;

  loading.value = true;
  error.value = null;
  try {
    const response = await fetch(`${baseApi}/api/discover/${currentTeamId}/announcements`);
    if (!response.ok) {
      const payload = await response.text();
      throw new Error(payload || 'Kunne ikke hente annonseringer.');
    }
    const payload = await response.json();
    announcements.value = Array.isArray(payload) ? payload.map(normalizeAnnouncement) : [];
  } catch (err: any) {
    error.value = err.message || 'Ukjent feil.';
  } finally {
    loading.value = false;
  }
}

async function createAnnouncement() {
  if (!authStore.user?.id) {
    error.value = 'Du må være logget inn for å publisere.';
    return;
  }

  isPosting.value = true;
  error.value = null;
  try {
    const currentTeamId = teamId.value;
    if (!currentTeamId) {
      throw new Error('Ingen team valgt.');
    }

    const response = await fetch(`${baseApi}/api/discover/${currentTeamId}/announcements`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        createdBy: authStore.user.id,
        title: title.value,
        body: body.value
      })
    });

    if (!response.ok) {
      const payload = await response.text();
      throw new Error(payload || 'Kunne ikke publisere annonseringen.');
    }

    title.value = '';
    body.value = '';
    await fetchAnnouncements();
  } catch (err: any) {
    error.value = err.message || 'Ukjent feil.';
  } finally {
    isPosting.value = false;
  }
}

watch(teamId, () => {
  void fetchAnnouncements();
}, { immediate: true });
</script>

<style scoped>
.news {
  padding: 2rem;
}

.create-news {
  margin-bottom: 2rem;
  padding: 1.5rem;
  border: 1px solid #ddd;
  border-radius: 12px;
  background: #fafafa;
}

.field {
  display: flex;
  flex-direction: column;
  margin-bottom: 1rem;
}

label {
  margin-bottom: 0.5rem;
  font-weight: 600;
}

input,
textarea {
  width: 100%;
  padding: 0.75rem;
  border-radius: 8px;
  border: 1px solid #ccc;
  font-size: 1rem;
}

button {
  padding: 0.75rem 1.25rem;
  border: none;
  border-radius: 8px;
  background-color: #0077cc;
  color: white;
  cursor: pointer;
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  color: #b00020;
}
</style>
