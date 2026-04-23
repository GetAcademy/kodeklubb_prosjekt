<template>
  <div class="tag-tree">
    <button v-if="canGoBack" class="back-btn" @click="goBack">⬅ Tilbake</button>

    <ul class="tree-list">
      <li v-for="(node, key) in currentNodes" :key="key" class="tree-item">
        <div class="tree-row" :style="{ paddingLeft: `${(props.path.length + navStack.length) * 16}px` }">
          <span class="tree-icon">{{ hasChildren(node) ? '📁' : '🏷️' }}</span>
          <span class="tree-label">{{ key }}</span>

          <div class="tree-actions">
            <template v-if="isRootLevel">
              <button class="btn btn-navigate" @click="navigate(key)">Åpne ▶</button>
            </template>
            <template v-else>
              <button v-if="hasChildren(node)" class="btn btn-navigate" @click="navigate(key)">Gå inn ▶</button>
              <button class="btn btn-add" @click="emitAddTag(fullPath(key))">+ Legg til</button>
            </template>
          </div>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';

const props = defineProps<{ nodes: any, path: string[] }>();
const emit = defineEmits(['add-tag']);

const navStack = ref<string[]>([]);

const isRootLevel = computed(() => (props.path.length + navStack.value.length) === 0);

const currentNodes = computed(() => {
  let cur = props.nodes;
  for (const key of navStack.value) {
    if (!cur) break;
    if (cur[key] && typeof cur[key] === 'object') {
      if ('children' in cur[key]) {
        if (Array.isArray(cur[key].children)) {
          cur = Object.fromEntries(cur[key].children.map((v) => [v, {}]));
        } else if (typeof cur[key].children === 'object') {
          cur = cur[key].children;
        } else {
          cur = {};
        }
      } else if (Object.keys(cur[key]).length > 0) {
        cur = cur[key];
      } else {
        cur = {};
      }
    } else {
      cur = {};
    }
  }
  if (Array.isArray(cur)) {
    cur = Object.fromEntries(cur.map((v) => [v, {}]));
  }
  return cur && typeof cur === 'object' ? cur : {};
});

const canGoBack = computed(() => navStack.value.length > 0);

function hasChildren(node: any) {
  if (!node) return false;
  if (node.children && typeof node.children === 'object') {
    return Object.keys(node.children).length > 0;
  }
  if (!node.children && typeof node === 'object' && Object.keys(node).length > 0) {
    return true;
  }
  return false;
}

function navigate(key: string) {
  navStack.value.push(key);
}

function goBack() {
  navStack.value.pop();
}

function fullPath(key: string) {
  return [...props.path, ...navStack.value, key].join('/');
}

function emitAddTag(tagPath: string) {
  emit('add-tag', tagPath);
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
  padding-left: 16px;       /* space between label and buttons */
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