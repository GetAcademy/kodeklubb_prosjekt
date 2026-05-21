<template>
    <section class="flex-wrap-row-justify-space-evenly">
        <FormSchema v-for = "data in schemas":data="data"/>
    </section>
    <section class="tags-editor">
        <h2>Mine interesser</h2>
        <AddTags
          :user-mode="true"
          :existing-tags="userTags"
          @add-tag="addTagFromHierarchy"
        />
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
import AddTags from '@/components/teams/AddTags.vue';
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


// Remove fetchPredefinedTags, we now use the hierarchy system

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


async function addTagFromHierarchy(tagPath: string) {
    if (!user.value?.id) {
        tagsError.value = 'Du må være logget inn.';
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
            body: JSON.stringify({ tagPaths: [tagPath] })
        });
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || 'Kunne ikke lagre interesse.');
        }
        tagsError.value = '';
        tagsSaving.value = false;
        await fetchUserTags();
    } catch (error) {
        tagsError.value = error instanceof Error ? error.message : 'Ukjent feil.';
        tagsSaving.value = false;
    }
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