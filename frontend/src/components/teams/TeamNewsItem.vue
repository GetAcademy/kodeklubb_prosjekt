<template>
  <article class="announcement-item">
    <header>
      <h2>{{ announcement.title }}</h2>
      <p class="meta">Publisert {{ formatDate(announcement.createdAt) }}</p>
    </header>
    <p>{{ announcement.body }}</p>
  </article>
</template>

<script setup lang="ts">
import type { TeamAnnouncement } from '@/types/teams/announcement';

const props = defineProps<{ announcement: TeamAnnouncement }>();

function formatDate(value: string) {
  return new Intl.DateTimeFormat('nb-NO', {
    year: 'numeric',
    month: 'long',
    day: '2-digit'
  }).format(new Date(value));
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
