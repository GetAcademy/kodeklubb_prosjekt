<template>
    <AddTags :team-id="teamId" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import AddTags from '@/components/teams/AddTags.vue';

const route = useRoute();

// Present when this page is reached via /teams/:teamId/add-tags.
// Undefined on /profile/add-tags, in which case AddTags falls back
// to saving tags against the logged-in user instead of a team.
const teamId = computed<string | undefined>(() => {
    const value = route.params.teamId;
    return typeof value === 'string' ? value : undefined;
});
</script>