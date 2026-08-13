<template>
  <div class="tag-tree">
    <button v-if="canGoBack" class="back-btn" @click="goBack">⬅ Tilbake</button>

    <p v-if="tagsStore.loading && !tagsStore.tags.length" class="loading">
      ⏳ Laster tagger...
    </p>
    <p v-else-if="tagsStore.error" class="error">{{ tagsStore.error }}</p>

    <ul v-else class="tree-list">
      <li v-for="node in currentNodes" :key="node.id" class="tree-item">
        <div class="tree-row" :style="{ paddingLeft: `${parentStack.length * 16}px` }">
          <span class="tree-icon">{{ hasChildren(node.id) ? '📁' : '🏷️' }}</span>
          <span class="tree-label">{{ node.name }}</span>

          <div class="tree-actions">
            <template v-if="isRootLevel">
              <button class="btn btn-navigate" @click="navigate(node.id)">Åpne ▶</button>
            </template>
            <template v-else>
              <button v-if="hasChildren(node.id)" class="btn btn-navigate" @click="navigate(node.id)">Gå inn ▶</button>
              <button v-if="canAdd(node)" class="btn btn-add" @click="emitAddTag(node.id)">+ Legg til</button>
            </template>
          </div>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useTagsStore } from '@/stores/tagsStore';
import type { Tag } from '@/stores/tagsStore';

const emit = defineEmits<{ (e: 'add-tag', tagId: string): void }>();

const tagsStore = useTagsStore();

// Stack of parent ids navigated into so far. Empty = root level.
const parentStack = ref<string[]>([]);

const currentParentId = computed<string | null>(() =>
  parentStack.value.length ? parentStack.value[parentStack.value.length - 1] : null
);

const isRootLevel = computed(() => parentStack.value.length === 0);
const canGoBack = computed(() => parentStack.value.length > 0);

const currentNodes = computed<Tag[]>(() => tagsStore.getChildren(currentParentId.value));

function hasChildren(tagId: string): boolean {
  return tagsStore.getChildren(tagId).length > 0;
}

function canAdd(node: Tag): boolean {
  return !hasChildren(node.id) || node.openForChildSuggestions === true;
}

function navigate(tagId: string) {
  parentStack.value.push(tagId);
}

function goBack() {
  parentStack.value.pop();
}

function emitAddTag(tagId: string) {
  emit('add-tag', tagId);
}
</script>

<style scoped>
.tag-tree {
  font-family: sans-serif;
  padding: 8px;
}

.back-btn {
  margin-bottom: 12px;
  padding: 6px 16px;
  background: #e0e0e0;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  color: #333;
}

.back-btn:hover {
  background: #ccc;
}

.loading, .error {
  padding: 12px;
  font-size: 14px;
}

.error {
  color: #842029;
}

.tree-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.tree-item {
  border-bottom: 1px solid #f0f0f0;
}

.tree-item:last-child {
  border-bottom: none;
}

.tree-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 8px;
  border-radius: 6px;
  transition: background 0.15s;
}

.tree-row:hover {
  background: #eef4ff;
}

.tree-icon {
  font-size: 16px;
  flex-shrink: 0;
}

.tree-label {
  flex: 1;
  font-size: 15px;
  font-weight: 500;
  color: #222;
}

.tree-actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
  padding-left: 16px;
}

.btn {
  padding: 5px 14px;
  border: none;
  border-radius: 5px;
  cursor: pointer;
  font-size: 13px;
  white-space: nowrap;
  font-weight: 500;
}

.btn-navigate {
  background: #e8f0fe;
  color: #1a73e8;
}

.btn-navigate:hover {
  background: #c5d8fb;
}

.btn-add {
  background: #0077cc;
  color: white;
}

.btn-add:hover {
  background: #005fa3;
}
</style>