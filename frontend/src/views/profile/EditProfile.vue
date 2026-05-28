<template>
    <section class="flex-wrap-row-justify-space-evenly">
        <FormSchema v-for="data in schemas" :data="data"/>
    </section>

    <section class="tags-editor">
        <h2>Mine interesser</h2>

        <div class="tag-container">
            <div class="tree-section">
                <TagTree
                    v-if="tagHierarchy"
                    :nodes="tagHierarchy"
                    :path="[]"
                    @add-tag="addTagFromHierarchy"
                />
                <div v-else class="muted">⏳ Laster tagger...</div>
            </div>

            <div class="selected-section">
                <p v-if="tagsError" class="error">{{ tagsError }}</p>
                <p v-else-if="tagsLoading" class="muted">Laster tags...</p>
                <div v-else-if="userTags.length">
                    <h3 class="selected-title">✅ Mine tags</h3>
                    <ul class="tags-list">
                        <li v-for="tag in userTags" :key="tag.id" class="tag-item">
                            🏷️ {{ tag.name }}
                        </li>
                    </ul>
                </div>
                <p v-else class="muted">Ingen interesser lagt til enda.</p>
                <p v-if="saveMessage" :class="saveSuccess ? 'success-msg' : 'error-msg'">{{ saveMessage }}</p>
            </div>
        </div>
    </section>
</template>

<script lang="ts" setup>
import { computed, onMounted, ref, watch } from 'vue';
import TagTree from '@/components/teams/TagTree.vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

const personalData = {
    method: "POST",
    encrypted: true,
    novalidate: true,
    action: "/profile",
    name: "profile-schema",
    title: "Profile Information",
    inputControl: [
        { value: '', id: 'city', name: 'Kommune', placeholder: 'e.g Ålesund', cls: ['city-input'], autofocus: true },
        { value: '', id: 'county', name: 'Fylke', placeholder: 'e.g Møre og Romsdal', cls: ['county-input'], autofocus: true },
        { value: '', id: 'email', name: 'Email', placeholder: 'e.g ola.norman@outlook.com', cls: ['email-input'], autofocus: true },
        { value: '', id: 'bio', name: 'Biografi', placeholder: 'e.g Møre og Romsdal', cls: ['county-input'], type: 'textarea', autofocus: true }
    ]
};

const schemas = computed(() => [personalData]);

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

interface UserTag {
    id: string;
    name: string;
    description?: string;
    category?: string;
}

const tagHierarchy = ref<any>(null);
const userTags = ref<UserTag[]>([]);
const tagsLoading = ref(false);
const tagsError = ref('');
const saveMessage = ref('');
const saveSuccess = ref(false);

// Load tag hierarchy tree
const fetchTagHierarchy = async () => {
    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const res = await fetch(`${baseApi}/api/tags/hierarchy`);
        tagHierarchy.value = await res.json();
    } catch (err) {
        console.error('Failed to load tag hierarchy', err);
    }
};

// Load user's existing tags
const fetchUserTags = async () => {
    if (!user.value?.id) return;
    tagsLoading.value = true;
    tagsError.value = '';
    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const response = await fetch(`${baseApi}/api/users/${user.value.id}/tags`);
        if (!response.ok) throw new Error('Kunne ikke hente dine interesser.');
        const payload = await response.json();
        userTags.value = Array.isArray(payload) ? payload : (payload?.tags ?? []);
    } catch (error) {
        tagsError.value = error instanceof Error ? error.message : 'Ukjent feil.';
    } finally {
        tagsLoading.value = false;
    }
};

// Called when user clicks "+ Legg til" in the tree
async function addTagFromHierarchy(tagPath: string) {
    if (!user.value?.id) {
        tagsError.value = 'Du må være logget inn.';
        return;
    }
    tagsError.value = '';
    saveMessage.value = '';
    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const response = await fetch(`${baseApi}/api/users/${user.value.id}/tags`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ tagIds: [], tagPaths: [tagPath] })
        });
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || 'Kunne ikke lagre interesse.');
        }
        saveSuccess.value = true;
        saveMessage.value = '✅ Tag lagret!';
        await fetchUserTags();
    } catch (error) {
        saveSuccess.value = false;
        saveMessage.value = error instanceof Error ? error.message : 'Ukjent feil.';
    }
}

onMounted(async () => {
    await fetchTagHierarchy();
    if (user.value?.id) await fetchUserTags();
});

watch(user, async (newUser) => {
    if (newUser?.id) await fetchUserTags();
});
</script>

<style scoped>
.tags-editor {
    padding: 24px;
    max-width: 860px;
    margin: 0 auto;
}

.tag-container {
    display: flex;
    gap: 32px;
    align-items: flex-start;
    margin-top: 16px;
}

.tree-section {
    flex: 1;
    background: #ffffff;
    border: 1px solid #e0e0e0;
    border-radius: 10px;
    padding: 16px;
    box-shadow: 0 2px 6px rgba(0,0,0,0.05);
}

.selected-section {
    width: 280px;
    background: #f0f7ff;
    border: 1px solid #c5d8fb;
    border-radius: 10px;
    padding: 16px;
}

.selected-title {
    font-size: 16px;
    font-weight: 600;
    margin-bottom: 12px;
    color: #1a73e8;
}

.tags-list {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.tag-item {
    background: white;
    border: 1px solid #d0e4ff;
    border-radius: 6px;
    padding: 6px 10px;
    font-size: 13px;
    color: #333;
}

.muted {
    color: #999;
    font-size: 13px;
}

.error {
    color: #d32f2f;
}

.success-msg {
    color: #0f5132;
    margin-top: 8px;
    font-size: 13px;
}

.error-msg {
    color: #842029;
    margin-top: 8px;
    font-size: 13px;
}
</style>