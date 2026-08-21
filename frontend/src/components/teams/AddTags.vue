<template>
  <div class="tag-page">
    <h2 class="page-title">🏷️ Legg til tags</h2>

    <div class="tag-container">
      <div class="tree-section">
        <p v-if="loading" class="loading-existing">Laster tags...</p>
        <TagTree @add-tag="addTag" :disabled-tag-ids="savedTagIds" />
      </div>

      <div class="selected-section">
        <template v-if="savedTags.length">
          <h3 class="selected-title">✅ Dine tags</h3>
          <ul class="selected-list">
            <li v-for="tag in savedTags" :key="tag.id" class="selected-item">
              <span class="tag-badge">🏷️ {{ tag.name }}</span>
              <button class="remove-btn" @click="removeTag(tag.id)" title="Fjern">✕</button>
            </li>
          </ul>
        </template>

        <p v-else class="no-tags">
          Ingen tags lagt til ennå. Velg fra treet til venstre — de lagres med en gang.
        </p>
      </div>
    </div>

    <p v-if="statusMessage" :class="statusType === 'error' ? 'error-msg' : 'success-msg'" class="status-msg">
      {{ statusMessage }}
    </p>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import axios from 'axios';
import TagTree from './TagTree.vue';
import { useAuthStore } from '@/stores/authStore';
import { useTagsStore } from '@/stores/tagsStore';
import { storeToRefs } from 'pinia';

// Optional: when set, tags are saved against this team instead of the
// logged-in user. AddTagsPage.vue supplies this from the route param
// when managing a team's tags.
const props = defineProps<{ teamId?: string }>();

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);
const tagsStore = useTagsStore();

interface SavedTag {
  id: string;
  name: string;
}

const savedTags = ref<SavedTag[]>([]);
const savedTagIds = computed(() => savedTags.value.map(t => t.id));
const loading = ref(false);

const statusMessage = ref('');
const statusType = ref<'idle' | 'error' | 'success'>('idle');

function tagName(tagId: string): string {
  return tagsStore.getById(tagId)?.name ?? tagId;
}

async function fetchExistingTags() {
  loading.value = true;
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const url = props.teamId
      ? `${baseApi}/api/discover/${props.teamId}/tags`
      : (user.value?.id ? `${baseApi}/api/users/${user.value.id}/tags` : null);
    if (!url) return;

    const response = await axios.get(url);
    savedTags.value = (response.data ?? []).map((t: any) => ({
      id: t.id ?? t.Id,
      name: t.name ?? t.Name,
    }));
  } catch (err) {
    console.error('Failed to load existing tags', err);
  } finally {
    loading.value = false;
  }
}

// Every node in the tree behaves identically — there is no special
// handling for any particular tag, category, or subtree. Clicking a tag
// saves it immediately; there is no pending/staging step.
async function addTag(tagId: string) {
  if (savedTagIds.value.includes(tagId)) return;

  // Mark as saved immediately (before the API call resolves) so a second
  // rapid click on the same tag — a real double-click, or a stray double
  // emit — sees it as already taken and bails out via the guard above,
  // instead of both calls racing past the check before either finishes.
  const optimisticTag = { id: tagId, name: tagName(tagId) };
  savedTags.value.push(optimisticTag);

  statusMessage.value = '';
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const payload = { selections: [{ tagId, levelTagId: null }] };

    if (props.teamId) {
      await axios.post(`${baseApi}/api/discover/${props.teamId}/tags`, {
        ...payload,
        discordId: user.value?.id ?? null,
      });
    } else {
      const discordId = user.value?.id;
      if (!discordId) throw new Error('Not logged in');
      await axios.post(`${baseApi}/api/users/${discordId}/tags`, payload);
    }

    statusType.value = 'success';
    statusMessage.value = 'Tag lagret!';
  } catch (err) {
    console.error('Failed to save tag', err);
    // Roll back the optimistic add — it was never actually saved.
    savedTags.value = savedTags.value.filter(t => t.id !== tagId);
    statusType.value = 'error';
    statusMessage.value = 'Kunne ikke lagre tag.';
  }
}

async function removeTag(tagId: string) {
  statusMessage.value = '';
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    if (props.teamId) {
      await axios.delete(`${baseApi}/api/discover/${props.teamId}/tags/${tagId}`);
    } else {
      const discordId = user.value?.id;
      if (!discordId) throw new Error('Not logged in');
      await axios.delete(`${baseApi}/api/users/${discordId}/tags/${tagId}`);
    }
    savedTags.value = savedTags.value.filter(t => t.id !== tagId);
  } catch (err) {
    console.error('Failed to remove tag', err);
    statusType.value = 'error';
    statusMessage.value = 'Kunne ikke fjerne tag.';
  }
}

onMounted(async () => {
  await tagsStore.ensureLoaded();
  await fetchExistingTags();
});
</script>

<style scoped>
.tag-page {
  padding: 24px;
  font-family: sans-serif;
  max-width: 860px;
  margin: 0 auto;
}

.page-title {
  font-size: 22px;
  font-weight: 700;
  margin-bottom: 20px;
  color: #1a1a1a;
}

.tag-container {
  display: flex;
  gap: 32px;
  align-items: flex-start;
}

.tree-section {
  flex: 1;
  background: #ffffff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 16px;
  box-shadow: 0 2px 6px rgba(0,0,0,0.05);
}

.loading-existing {
  color: #888;
  font-size: 13px;
  margin: 0 0 10px;
}

.selected-section {
  width: 280px;
  background: #f0f7ff;
  border: 1px solid #c5d8fb;
  border-radius: 10px;
  padding: 16px;
  box-shadow: 0 2px 6px rgba(0,0,0,0.05);
}

.selected-title {
  font-size: 16px;
  font-weight: 600;
  margin: 0 0 12px;
  color: #1a73e8;
}

.selected-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.selected-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: white;
  border: 1px solid #d0e4ff;
  border-radius: 6px;
  padding: 6px 10px;
}

.tag-badge {
  font-size: 13px;
  color: #333;
}

.remove-btn {
  background: none;
  border: none;
  color: #cc0000;
  cursor: pointer;
  font-size: 14px;
  padding: 2px 6px;
  border-radius: 4px;
  flex-shrink: 0;
  margin-left: 12px;
}

.remove-btn:hover {
  background: #ffe0e0;
}

.no-tags {
  color: #999;
  font-size: 13px;
  padding: 16px;
  background: #fafafa;
  border: 1px dashed #ddd;
  border-radius: 10px;
  text-align: center;
}

.success-msg { color: #0f5132; margin-top: 8px; font-size: 13px; }
.error-msg { color: #842029; margin-top: 8px; font-size: 13px; }
.status-msg { margin-top: 16px; font-size: 14px; text-align: center; }
</style>