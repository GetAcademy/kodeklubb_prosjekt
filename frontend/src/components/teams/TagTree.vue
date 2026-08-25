<template>
  <ul class="tag-tree" :class="{ 'is-root': depth === 0 }">
    <li v-for="node in nodes" :key="node.id" class="tree-node">
      <div
        class="tree-row"
        :style="{ paddingLeft: `${depth * 18 + 8}px` }"
        @click="hasChildren(node.id) ? toggle(node.id) : null"
      >
        <span class="toggle" :class="{ expanded: isExpanded(node.id) }">
          <svg v-if="hasChildren(node.id)" viewBox="0 0 16 16" width="10" height="10">
            <path d="M4 2 L12 8 L4 14 Z" fill="currentColor" />
          </svg>
        </span>

        <span class="node-icon">
          <svg v-if="hasChildren(node.id)" viewBox="0 0 20 16" width="15" height="12">
            <path d="M1 2h6l2 2h10v10a1 1 0 0 1-1 1H1a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z" fill="currentColor" />
          </svg>
          <svg v-else viewBox="0 0 16 16" width="12" height="12">
            <circle cx="8" cy="8" r="5" fill="none" stroke="currentColor" stroke-width="1.6" />
          </svg>
        </span>

        <span class="node-label">{{ node.name }}</span>

        <span v-if="isDisabled(node.id)" class="node-status added">✓</span>
        <button
          v-else
          class="add-btn"
          @click.stop="emitAddTag(node.id)"
        >+ Legg til</button>
      </div>

      <TagTree
        v-if="hasChildren(node.id) && isExpanded(node.id)"
        :parent-id="node.id"
        :depth="depth + 1"
        :disabled-tag-ids="disabledTagIds"
        @add-tag="emitAddTag"
      />
    </li>
  </ul>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useTagsStore } from '@/stores/tagsStore';
import type { Tag } from '@/stores/tagsStore';
// No self-import needed: Vue's <script setup> compiler automatically lets
// a component reference itself by its own filename (TagTree.vue → <TagTree>
// in its own template), with no import statement required. Adding an
// explicit self-import was confusing the TypeScript language server.
//
// This recursion is SAFE, unlike the earlier AddTags.vue bug that caused a
// blank-page crash: that component rendered itself unconditionally with no
// stopping point. Here, the recursive <TagTree> below is guarded by
// v-if="hasChildren(node.id) && isExpanded(node.id)" — it only renders for
// a node that (a) actually has children and (b) the user has explicitly
// expanded, and each recursive call is scoped to a different, deeper
// parentId. A leaf node (no children) never recurses, so the chain always
// terminates.

const props = defineProps<{
  parentId?: string | null;
  depth?: number;
  disabledTagIds?: string[];
  // Tag ids to hide from this specific list (used to pull a category like
  // "Geografi" out of the main tree so it can be shown in its own box).
  excludeIds?: string[];
}>();

const emit = defineEmits<{ (e: 'add-tag', tagId: string): void }>();

const tagsStore = useTagsStore();
const depth = computed(() => props.depth ?? 0);
const nodes = computed<Tag[]>(() => {
  const all = tagsStore.getChildren(props.parentId ?? null);
  if (!props.excludeIds?.length) return all;
  return all.filter(tag => !props.excludeIds!.includes(tag.id));
});

const expandedIds = ref<Set<string>>(new Set());

function hasChildren(tagId: string): boolean {
  return tagsStore.getChildren(tagId).length > 0;
}

function isExpanded(tagId: string): boolean {
  return expandedIds.value.has(tagId);
}

function toggle(tagId: string) {
  const next = new Set(expandedIds.value);
  next.has(tagId) ? next.delete(tagId) : next.add(tagId);
  expandedIds.value = next;
}

function isDisabled(tagId: string): boolean {
  return props.disabledTagIds?.includes(tagId) ?? false;
}

function emitAddTag(tagId: string) {
  emit('add-tag', tagId);
}
</script>

<style scoped>
.tag-tree {
  list-style: none;
  margin: 0;
  padding: 0;
  font-family: sans-serif;
  --tree-accent: #1a73e8;
  --tree-success: #1d9a6c;
  --tree-muted: #6b7280;
  --tree-hover: #f2f6fc;
}

.tag-tree.is-root {
  border: 1px solid #e4e6eb;
  border-radius: 10px;
  padding: 6px 0;
  background: #fff;
}

.tree-row {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px 6px 0;
  cursor: default;
  border-radius: 6px;
  transition: background 0.12s ease;
}

.tree-row:hover {
  background: var(--tree-hover);
}

.toggle {
  width: 14px;
  height: 14px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--tree-muted);
  cursor: pointer;
  transition: transform 0.12s ease;
}

.toggle.expanded {
  transform: rotate(90deg);
}

.node-icon {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  color: var(--tree-accent);
}

.node-label {
  flex: 1;
  font-size: 13.5px;
  color: #1a1d24;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.node-status.added {
  color: var(--tree-success);
  font-weight: 700;
  font-size: 13px;
  flex-shrink: 0;
}

.add-btn {
  flex-shrink: 0;
  font-size: 12px;
  font-weight: 600;
  color: var(--tree-accent);
  background: #eaf2fe;
  border: none;
  border-radius: 6px;
  padding: 3px 8px;
  cursor: pointer;
  white-space: nowrap;
  opacity: 0;
  transition: opacity 0.12s ease, background 0.12s ease;
}

.tree-row:hover .add-btn {
  opacity: 1;
}

.add-btn:hover {
  background: #d5e6fc;
}
</style>