<template>
    <section class="tags-editor">
        <h2>Mine interesser</h2>
        <AddTags @add-tag="addTagFromHierarchy" />
        <p v-if="tagsError" class="error">{{ tagsError }}</p>
        <p v-else-if="tagsLoading" class="muted">Laster tags...</p>
        <ul v-else-if="userTags.length" class="tags-list">
            <li v-for="tag in userTags" :key="tag.id" class="tag-item">
                {{ tag.name }}
            </li>
        </ul>
        <p v-else class="muted">Ingen interesser lagt til enda.</p>
    </section>
</template>

<script lang="ts" setup>
import { onMounted, ref, watch } from 'vue';
import AddTags from '@/components/teams/AddTags.vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

interface UserTag {
    id: string;
    name: string;
    description?: string;
    category?: string;
    parentTagId?: string;
    openForChildSuggestions?: boolean;
}

const userTags = ref<UserTag[]>([]);
const tagsLoading = ref(false);
const tagsError = ref('');
const lastFetchedDiscordId = ref<string | null>(null);

const fetchUserTags = async () => {
    if (!user.value?.id) return;

    tagsLoading.value = true;
    tagsError.value = '';

    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const response = await fetch(`${baseApi}/api/users/${user.value.id}/tags`);
        if (!response.ok) {
            throw new Error('Kunne ikke hente dine interesser.');
        }
        const payload = await response.json();
        userTags.value = Array.isArray(payload) ? payload : (payload?.tags ?? []);
    } catch (error) {
        tagsError.value = error instanceof Error ? error.message : 'Ukjent feil.';
    } finally {
        tagsLoading.value = false;
    }
};

async function addTagFromHierarchy() {
    // AddTags.vue now saves tags itself via TagTree; just refresh the list after.
    await fetchUserTags();
}

watch(user, async () => {
    const discordId = user.value?.id || null;
    if (!discordId || lastFetchedDiscordId.value === discordId) return;
    lastFetchedDiscordId.value = discordId;
    await fetchUserTags();
}, { immediate: true });

onMounted(async () => {
    if (user.value?.id) {
        await fetchUserTags();
    }
});
</script>

<style scoped>
.tags-editor {
    padding: 24px;
    max-width: 860px;
    margin: 0 auto;
}

.tags-list {
    list-style: none;
    padding: 0;
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-top: 0.5rem;
}

.tag-item {
    display: inline-block;
    background: #f0f4ff;
    border: 1px solid #c5d8fb;
    border-radius: 20px;
    padding: 0.2rem 0.6rem;
    font-size: 0.85rem;
    color: #1a73e8;
}

.muted {
    color: #999;
    font-size: 0.9rem;
}

.error {
    color: #842029;
}
</style>