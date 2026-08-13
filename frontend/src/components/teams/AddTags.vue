<template>
  <div class="tag-page">
    <h2 class="page-title">🏷️ Legg til tags</h2>

    <div class="tag-container">
      <div class="tree-section">
        <TagTree @add-tag="addTag" />
      </div>

      <div v-if="selectedTagIds.length" class="selected-section">
        <h3 class="selected-title">✅ Valgte tags</h3>
        <ul class="selected-list">
          <li v-for="tagId in selectedTagIds" :key="tagId" class="selected-item">
            <span class="tag-badge">🏷️ {{ tagName(tagId) }}</span>
            <button class="remove-btn" @click="removeTag(tagId)">✕</button>
          </li>
        </ul>
        <button class="save-btn" @click="saveTags" :disabled="saveStatus === 'saving'">
          {{ saveStatus === 'saving' ? 'Lagrer...' : '💾 Lagre tags' }}
        </button>
      </div>

      <div v-else class="no-tags">
        Ingen tags valgt ennå. Velg fra treet til venstre.
      </div>
    </div>

    <!-- Status message always visible -->
    <p v-if="saveMessage" :class="saveStatus === 'error' ? 'error-msg' : 'success-msg'" class="status-msg">
      {{ saveMessage }}
    </p>
  </div>
</template>
<script setup lang="ts">
import { ref } from 'vue';
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

const selectedTagIds = ref<string[]>([]);
const saveStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle');
const saveMessage = ref('');

function tagName(tagId: string): string {
  return tagsStore.getById(tagId)?.name ?? tagId;
}

function addTag(tagId: string) {
  if (!selectedTagIds.value.includes(tagId)) {
    selectedTagIds.value.push(tagId);
  }
}

function removeTag(tagId: string) {
  selectedTagIds.value = selectedTagIds.value.filter(id => id !== tagId);
}

async function saveTags() {
  if (!selectedTagIds.value.length) return;
  saveStatus.value = 'saving';
  saveMessage.value = '';

  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';

    if (props.teamId) {
      // Save tags for a team
      await axios.post(`${baseApi}/api/discover/${props.teamId}/tags`, {
        tagIds: selectedTagIds.value
      });
    } else {
      // Save tags for the logged-in user
      const discordId = user.value?.id;
      if (!discordId) throw new Error('Not logged in');
      await axios.post(`${baseApi}/api/users/${discordId}/tags`, {
        tagIds: selectedTagIds.value
      });
    }

    saveStatus.value = 'saved';
    saveMessage.value = 'Tags lagret!';
    selectedTagIds.value = [];
  } catch (err) {
    console.error('Failed to save tags', err);
    saveStatus.value = 'error';
    saveMessage.value = 'Kunne ikke lagre tags.';
  }
}
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
  margin-bottom: 12px;
  color: #1a73e8;
}

.selected-list {
  list-style: none;
  padding: 0;
  margin: 0 0 16px 0;
  display: flex;
  flex-wrap: wrap;
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
  white-space: nowrap;
  overflow: hidden;
  width: 100%;
}

.tag-badge {
  font-size: 13px;
  color: #333;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
  text-align: center;
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

.save-btn {
  width: 100%;
  padding: 8px;
  background: #0077cc;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  cursor: pointer;
}

.save-btn:hover {
  background: #005fa3;
}

.no-tags {
  width: 260px;
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