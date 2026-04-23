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
        <button class="save-btn" :disabled="saveStatus === 'saving'" @click="saveTags">
          {{ saveStatus === 'saving' ? '⏳ Lagrer...' : '💾 Lagre tags' }}
        </button>
        <p v-if="saveStatus === 'saved'" class="status-success">✅ Tags lagret!</p>
        <p v-if="saveStatus === 'error'" class="status-error">❌ Noe gikk galt. Prøv igjen.</p>
      </div>

      <div v-else class="no-tags">
        Ingen tags valgt ennå. Velg fra treet til venstre.
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import axios from 'axios';
import TagTree from './TagTree.vue';

// Get discordId from localStorage
const userData = JSON.parse(localStorage.getItem('user_data') || '{}');
const discordId = userData.id;

const tagHierarchy = ref<any>(null);
const selectedTags = ref<string[]>([]);
const saveStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle');

onMounted(async () => {
  try {
    const res = await axios.get('/api/discover/tags/hierarchy');
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

// Only show the last part of the path e.g. "Category/Sub/Easy" → "Easy"
function formatTag(tagPath: string) {
  const parts = tagPath.split('/');
  return parts[parts.length - 1];
}

async function saveTags() {
  if (!discordId) {
    alert('Bruker ikke funnet. Logg inn på nytt.');
    return;
  }

  saveStatus.value = 'saving';
  try {
    await axios.post(`/api/users/${discordId}/tags`, {
      tagIds: [],
      tagPaths: selectedTags.value
    });
    saveStatus.value = 'saved';
  } catch (err) {
    console.error('Failed to save tags', err);
    saveStatus.value = 'error';
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

.save-btn:disabled {
  background: #99c9e8;
  cursor: not-allowed;
}

.save-btn:hover:not(:disabled) {
  background: #005fa3;
}

.status-success {
  margin-top: 8px;
  font-size: 13px;
  color: #2e7d32;
  text-align: center;
}

.status-error {
  margin-top: 8px;
  font-size: 13px;
  color: #cc0000;
  text-align: center;
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
</style>