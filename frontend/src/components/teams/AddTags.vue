<template>
  <div class="tag-page">
    <h2 class="page-title">🏷️ Legg til tags</h2>

    <div class="tag-container">
      <div class="tree-section">
        <TagTree
          v-if="tagHierarchy"
          :nodes="tagHierarchy"
          :path="[]"
          @add-tag="addTag"
        />
        <div v-else class="loading">
          ⏳ Laster tagger...
        </div>
      </div>

      <div v-if="selectedTags.length" class="selected-section">
        <h3 class="selected-title">✅ Valgte tags</h3>
        <ul class="selected-list">
          <li v-for="tag in selectedTags" :key="tag" class="selected-item">
            <span class="tag-badge">🏷️ {{ formatTag(tag) }}</span>
            <button class="remove-btn" @click="removeTag(tag)">✕</button>
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
import { ref, onMounted } from 'vue';
import axios from 'axios';
import TagTree from './TagTree.vue';
import { useAuthStore } from '@/stores/authStore';
import { storeToRefs } from 'pinia';

const props = defineProps<{ teamId?: string }>();

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

const tagHierarchy = ref<any>(null);
const selectedTags = ref<string[]>([]);
const saveStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle');
const saveMessage = ref('');

onMounted(async () => {
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const res = await axios.get(`${baseApi}/api/discover/tags/hierarchy`);
    tagHierarchy.value = res.data;
  } catch (err) {
    console.error('Failed to load tags', err);
  }
});

function addTag(tagPath: string) {
  if (!selectedTags.value.includes(tagPath)) {
    selectedTags.value.push(tagPath);
  }
}

function removeTag(tagPath: string) {
  selectedTags.value = selectedTags.value.filter(t => t !== tagPath);
}

function formatTag(tagPath: string) {
  const parts = tagPath.split('/');
  return parts[parts.length - 1];
}

async function saveTags() {
  if (!selectedTags.value.length) return;
  saveStatus.value = 'saving';
  saveMessage.value = '';

  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';

    if (props.teamId) {
      // Save tags for a team
      await axios.post(`${baseApi}/api/discover/${props.teamId}/tags`, {
        tagPaths: selectedTags.value
      });
    } else {
      // Save tags for a user
      const discordId = user.value?.id;
      if (!discordId) throw new Error('Not logged in');
      await axios.post(`${baseApi}/api/users/${discordId}/tags`, {
        tagIds: [],
        tagPaths: selectedTags.value
      });
    }

    saveStatus.value = 'saved';
    saveMessage.value = 'Tags lagret!';
    selectedTags.value = [];
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

.loading {
  color: #888;
  font-size: 14px;
  padding: 12px;
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