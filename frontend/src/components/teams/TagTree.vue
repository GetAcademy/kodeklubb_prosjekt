<template>
  <div class="tag-tree">
    <div class="tree-header">
      <button v-if="canGoBack" class="back-btn" @click="goBack">
        <span class="back-arrow">←</span> Tilbake
      </button>
      <span v-if="canGoBack" class="crumb">{{ currentParentName }}</span>
    </div>

    <p v-if="tagsStore.loading && !tagsStore.tags.length" class="loading">
      Laster tagger...
    </p>
    <p v-else-if="tagsStore.error" class="error">{{ tagsStore.error }}</p>

    <div v-else class="tag-grid">
      <div
        v-for="node in currentNodes"
        :key="node.id"
        class="tag-card"
        :class="{ 'is-folder': hasChildren(node.id), 'is-added': isDisabled(node.id) && !hasChildren(node.id) }"
        @click="hasChildren(node.id) ? navigate(node.id) : (canAdd(node) && !isDisabled(node.id) ? emitAddTag(node.id) : null)"
      >
        <div class="card-icon">
          <span v-if="hasChildren(node.id)" class="icon-folder"></span>
          <span v-else class="icon-tag"></span>
        </div>
        <span class="card-label">{{ node.name }}</span>

        <span v-if="hasChildren(node.id)" class="card-hint">Åpne →</span>
        <span v-else-if="isDisabled(node.id)" class="card-hint added">✓ Lagt til</span>
        <span v-else-if="canAdd(node)" class="card-hint add">+ Legg til</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useTagsStore } from '@/stores/tagsStore';
import type { Tag } from '@/stores/tagsStore';

const emit = defineEmits<{ (e: 'add-tag', tagId: string): void }>();

// Tag ids that are already added (either previously saved, or selected but
// not yet saved in this session) — these render as locked instead of addable,
// so the same tag can never be added twice.
const props = defineProps<{ disabledTagIds?: string[] }>();

const tagsStore = useTagsStore();

// Stack of parent ids navigated into so far. Empty = root level.
const parentStack = ref<string[]>([]);

const currentParentId = computed<string | null>(() =>
  parentStack.value.length ? parentStack.value[parentStack.value.length - 1] : null
);

const currentParentName = computed<string>(() =>
  currentParentId.value ? (tagsStore.getById(currentParentId.value)?.name ?? '') : ''
);

const canGoBack = computed(() => parentStack.value.length > 0);

const currentNodes = computed<Tag[]>(() => tagsStore.getChildren(currentParentId.value));

function hasChildren(tagId: string): boolean {
  return tagsStore.getChildren(tagId).length > 0;
}

function canAdd(node: Tag): boolean {
  return !hasChildren(node.id) || node.openForChildSuggestions === true;
}

function isDisabled(tagId: string): boolean {
  return props.disabledTagIds?.includes(tagId) ?? false;
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
  --tt-border: #e4e6eb;
  --tt-accent: #1a73e8;
  --tt-accent-soft: #eaf2fe;
  --tt-success: #1d9a6c;
  --tt-success-soft: #e8f8f0;
  --tt-text: #1a1d24;
  --tt-muted: #6b7280;

  font-family: sans-serif;
}

.tree-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 16px;
  min-height: 32px;
}

.back-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: #fff;
  border: 1px solid var(--tt-border);
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  color: var(--tt-text);
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
}

.back-btn:hover {
  background: #f7f8fa;
  border-color: #c7cad1;
}

.back-arrow {
  font-size: 14px;
}

.crumb {
  font-size: 13px;
  color: var(--tt-muted);
  font-weight: 600;
}

.loading, .error {
  padding: 12px 0;
  font-size: 14px;
}

.error {
  color: #842029;
}

.tag-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 14px;
}

.tag-card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 10px;
  padding: 20px 14px 16px;
  background: #fff;
  border: 1px solid var(--tt-border);
  border-radius: 12px;
  cursor: pointer;
  transition: box-shadow 0.15s ease, border-color 0.15s ease, transform 0.1s ease;
}

.tag-card:hover {
  border-color: var(--tt-accent);
  box-shadow: 0 4px 14px rgba(26, 115, 232, 0.1);
  transform: translateY(-2px);
}

.tag-card.is-added {
  border-color: var(--tt-success);
  background: var(--tt-success-soft);
  cursor: default;
}

.tag-card.is-added:hover {
  transform: none;
  box-shadow: none;
}

.card-icon {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--tt-accent-soft);
}

.tag-card.is-added .card-icon {
  background: var(--tt-success-soft);
}

.icon-folder, .icon-tag {
  width: 18px;
  height: 18px;
  display: inline-block;
}

.icon-folder {
  background: var(--tt-accent);
  clip-path: polygon(0% 15%, 40% 15%, 50% 30%, 100% 30%, 100% 85%, 0% 85%);
  border-radius: 2px;
}

.icon-tag {
  background: var(--tt-muted);
  clip-path: polygon(0% 40%, 40% 0%, 100% 0%, 100% 60%, 60% 100%, 0% 60%);
  border-radius: 2px;
}

.tag-card.is-added .icon-tag {
  background: var(--tt-success);
}

.card-label {
  font-size: 14px;
  font-weight: 600;
  color: var(--tt-text);
  line-height: 1.3;
}

.card-hint {
  font-size: 12px;
  font-weight: 600;
  color: var(--tt-accent);
}

.card-hint.added {
  color: var(--tt-success);
}

.card-hint.add {
  color: var(--tt-accent);
}
</style>