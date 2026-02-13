<template>
    <section class="flex-wrap-row-justify-space-evenly">
        <FormSchema v-for = "data in schemas":data="data"/>
    </section>
    <section class="tags-editor">
        <h2>Mine interesser</h2>
        <div class="tags-input">
            <select v-model="selectedTagId" :disabled="tagsLoading || tagsSaving">
                <option value="">Velg interesser...</option>
                <optgroup v-for="(tags, category) in groupedPredefinedTags" :key="category" :label="category || 'Annet'">
                    <option v-for="tag in tags" :key="tag.id" :value="tag.id">
                        {{ tag.name }}
                    </option>
                </optgroup>
            </select>
            <button type="button" @click="addTag" :disabled="tagsSaving || !selectedTagId">
                {{ tagsSaving ? 'Lagrer...' : 'Legg til' }}
            </button>
        </div>
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
import { computed, onMounted, ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/authStore';


const personalData = {
    method: "POST",
    encrypted: true,
    novalidate: true,
    action: "/profile",
    name: "profile-schema",
    title: "Profile Information",
    inputControl: 
    [
        { value: '', id: 'city', name: 'Kommune', placeholder: 'e.g Ålesund', cls: ['city-input'], autofocus: true },
        { value: '', id: 'county', name: 'Fylke', placeholder: 'e.g Møre og Romsdal', cls: ['county-input'], autofocus: true},
        { value: '', id: 'email', name: 'Email', placeholder: 'e.g ola.norman@outlook.com', cls: ['email-input'], autofocus: true },
        { value: '', id: 'bio', name: 'Biografi', placeholder: 'e.g Møre og Romsdal', cls: ['county-input'], type: 'textarea', autofocus: true}
    ]
}

const schemas = computed(() => {
    return [
        personalData
    ]
});

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

interface PredefinedTag {
    id: string;
    name: string;
    description?: string;
    category?: string;
}

interface UserTag {
    id: string;
    name: string;
    description?: string;
    category?: string;
}

const selectedTagId = ref('');
const predefinedTags = ref<PredefinedTag[]>([]);
const userTags = ref<UserTag[]>([]);
const tagsLoading = ref(false);
const tagsSaving = ref(false);
const tagsError = ref('');
const lastFetchedDiscordId = ref<string | null>(null);

const groupedPredefinedTags = computed(() => {
    const grouped: Record<string, PredefinedTag[]> = {};
    predefinedTags.value.forEach(tag => {
        const category = tag.category || 'Other';
        if (!grouped[category]) {
            grouped[category] = [];
        }
        grouped[category].push(tag);
    });
    return grouped;
});

const fetchPredefinedTags = async () => {
    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const response = await fetch(`${baseApi}/api/users/tags/predefined`);
        if (!response.ok) {
            throw new Error('Kunne ikke hente tilgjengelige interesser.');
        }
        const payload = await response.json();
        predefinedTags.value = Array.isArray(payload) ? payload : (payload?.tags ?? []);
    } catch (error) {
        tagsError.value = error instanceof Error ? error.message : 'Ukjent feil.';
    }
};

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

const addTag = async () => {
    if (!user.value?.id) {
        tagsError.value = 'Du må være logget inn.';
        return;
    }

    if (!selectedTagId.value) {
        tagsError.value = 'Velg en interesse.';
        return;
    }

    tagsSaving.value = true;
    tagsError.value = '';

    try {
        const baseApi = import.meta.env.VITE_BASE_API || '';
        const response = await fetch(`${baseApi}/api/users/${user.value.id}/tags`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ tagIds: [selectedTagId.value] })
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || 'Kunne ikke lagre interesse.');
        }

        selectedTagId.value = '';
        tagsError.value = '';
        tagsSaving.value = false;
        await fetchUserTags();
    } catch (error) {
        tagsError.value = error instanceof Error ? error.message : 'Ukjent feil.';
        tagsSaving.value = false;
    }
};

watch(user, async () => {
    const discordId = user.value?.id || null;
    if (!discordId || lastFetchedDiscordId.value === discordId) return;
    lastFetchedDiscordId.value = discordId;
    await fetchUserTags();
}, { immediate: true });

onMounted(async () => {
    await fetchPredefinedTags();
    if (user.value?.id) {
        await fetchUserTags();
    }
});
</script>