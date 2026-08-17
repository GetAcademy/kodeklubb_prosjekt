<template>
    <section class="tags-editor">
        <h2>Mine interesser</h2>
        <!-- AddTags now fetches, displays, and saves everything itself -->
        <AddTags />
    </section>
</template>

<script lang="ts" setup>
import AddTags from '@/components/teams/AddTags.vue';
</script>

<style scoped>
.tags-editor {
    padding: 24px;
    max-width: 860px;
    margin: 0 auto;
}
</style>