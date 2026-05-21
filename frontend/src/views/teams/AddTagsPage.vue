<template>
  <AddTags :teamId="teamId ?? undefined" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import AddTags from '@/components/teams/AddTags.vue';

const route = useRoute();
const teamId = computed(() => route.params.teamId as string | null);
</script>