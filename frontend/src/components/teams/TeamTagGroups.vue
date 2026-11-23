<script setup lang="ts">
defineProps<{
    groups: {
        rootName: string;
        label: string;
        kind: string;
        tags: { id: string; name: string }[];
    }[];
}>();
</script>

<template>
    <section
        v-for="group in groups"
        :key="group.rootName"
        class="tag-group"
        :class="group.kind"
    >
        <h4>{{ group.label }}</h4>
        <ul>
            <li v-for="tag in group.tags" :key="tag.id">{{ tag.name }}</li>
        </ul>
    </section>
</template>

<style scoped>
.tag-group {
    margin-bottom: 0.75rem;
}

.tag-group h4 {
    margin: 0 0 0.25rem;
    color: var(--tag-text);
}

.tag-group ul {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
    list-style: none;
    margin: 0;
    padding: 0;
}

.tag-group li {
    padding: 0.2rem 0.6rem;
    border-radius: 1rem;
    background: var(--tag-bg);
    color: var(--tag-text);
}

.language    { --tag-bg: #e0ecff; --tag-text: #173d7a; }
.application { --tag-bg: #f1e6ff; --tag-text: #5b2b83; }
.level       { --tag-bg: #fff0cc; --tag-text: #654500; }
.geography   { --tag-bg: #dcf5ec; --tag-text: #155f48; }
</style>