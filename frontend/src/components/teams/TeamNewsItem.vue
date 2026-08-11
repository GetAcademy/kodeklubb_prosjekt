<template>
  <article class="announcement-item" :data-testid="`announcement-${announcement.id}`">
    <header>
      <h2 v-if="!editing">{{ announcement.title }}</h2>
      <div v-else class="edit-fields">
        <input v-model="editTitle" />
      </div>

      <p class="meta">Publisert {{ formatDate(announcement.createdAt) }}</p>
    </header>

    <div v-if="!editing">
      <p>{{ announcement.body }}</p>
    </div>
    <div v-else class="edit-fields">
      <textarea v-model="editBody" rows="4"></textarea>
    </div>

    <footer class="actions" v-if="isAdmin">
      <button v-if="!editing" @click="startEdit">Rediger</button>
      <button v-if="!editing" @click="onDelete" class="danger">Slett</button>

      <div v-if="editing" class="edit-actions">
        <button @click="saveEdit">Lagre</button>
        <button @click="cancelEdit">Avbryt</button>
      </div>
    </footer>
  </article>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { TeamAnnouncement } from '@/types/teams/announcement';

const props = defineProps<{ announcement: TeamAnnouncement; isAdmin?: boolean }>();
const emit = defineEmits<{
  (e: 'update', payload: { id: string; title: string; body: string }): void;
  (e: 'delete', id: string): void;
}>();

const editing = ref(false);
const editTitle = ref(props.announcement.title);
const editBody = ref(props.announcement.body);

const isAdmin = computed(() => props.isAdmin ?? false);

function formatDate(value: string) {
  return new Intl.DateTimeFormat('nb-NO', {
    year: 'numeric',
    month: 'long',
    day: '2-digit'
  }).format(new Date(value));
}

function startEdit() {
  console.log('startEdit called for', props.announcement.id);
  editTitle.value = props.announcement.title;
  editBody.value = props.announcement.body;
  editing.value = true;
}

function cancelEdit() {
  editing.value = false;
}

function saveEdit() {
  emit('update', { id: props.announcement.id, title: editTitle.value, body: editBody.value });
  editing.value = false;
}

function onDelete() {
  if (confirm('Er du sikker på at du vil slette denne annonseringen?')) {
    emit('delete', props.announcement.id);
  }
}
</script>

<style scoped>
.announcement-item {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 1rem;
}

.meta {
  color: #555;
  font-size: 0.9rem;
  margin-bottom: 0.75rem;
}
</style>