
<template>
  <div>
    <button v-if="canGoBack" @click="goBack">⬅ Tilbake</button>
    <ul>
      <li v-for="(node, key) in currentNodes" :key="key">
        <span>
          <template v-if="isRootLevel">
            <a href="#" @click.prevent="navigate(key)">{{ key }}</a>
          </template>
          <template v-else>
            <span>{{ key }}</span>
            <button v-if="hasChildren(node)" @click="navigate(key)">Gå inn</button>
            <button @click="emitAddTag(fullPath(key))">Legg til</button>
          </template>
        </span>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
const props = defineProps<{ nodes: any, path: string[] }>();
const emit = defineEmits(['add-tag']);

// Navigation stack
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
        // If the node is an object with keys but no 'children', treat keys as children
        cur = cur[key];
      } else {
        cur = {};
      }
    } else {
      cur = {};
    }
  }
  // If cur is an array, convert to object
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
  // Also treat as having children if node itself is an object with keys (for legacy/edge cases)
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
