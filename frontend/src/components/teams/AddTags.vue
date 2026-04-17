<template>
  <div>
    <h2>Legg til tags</h2>
    <TagTree
      v-if="tagHierarchy"
      :nodes="tagHierarchy"
      :path="[]"
      @add-tag="addTag"
    />
    <div v-else>Laster tagger...</div>
    <div v-if="selectedTags.length">
      <h3>Valgte tags:</h3>
      <ul>
        <li v-for="tag in selectedTags" :key="tag">{{ tag }}</li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import axios from 'axios';
import TagTree from './TagTree.vue';

const tagHierarchy = ref<any>(null);
const selectedTags = ref<string[]>([]);

onMounted(async () => {
  const res = await axios.get('/api/discover/tags/hierarchy');
  tagHierarchy.value = res.data;
});

function addTag(tagPath: string) {
  if (!selectedTags.value.includes(tagPath)) {
    selectedTags.value.push(tagPath);
    // TODO: Send to backend to save for team
  }
}
</script>
