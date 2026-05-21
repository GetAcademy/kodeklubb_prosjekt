<template>
  <AddTags :teamId="teamId" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import AddTags from '@/components/teams/AddTags.vue';

const route = useRoute();

// Present when accessed via /teams/:teamId/add-tags, undefined on /profile/add-tags
const teamId = computed(() => route.params.teamId as string | undefined);
</script>