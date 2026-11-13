import { getTags } from '@/api/tagsApi';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export interface Tag {
    id: string;
    name: string;
    parentId: string | null;
    openForChildSuggestions: boolean;
}

interface RawTag {
    id: string;
    name: string;
    parentId: string | null;
    openForChildSuggestions: boolean;
}

function normalize(raw: RawTag): Tag {
    return {
        id: raw.id,
        name: raw.name,
        parentId: raw.parentId,
        openForChildSuggestions: raw.openForChildSuggestions,
    };
}

export const useTagsStore = defineStore('tags', () => {
    const tags = ref<Tag[]>([]);
    const loading = ref(false);
    const error = ref('');

    let loadPromise: Promise<void> | null = null;
    let loaded = false;

    async function load() {
        loading.value = true;
        error.value = '';
                try {
            const payload: RawTag[] = await getTags();
            tags.value = Array.isArray(payload) ? payload.map(normalize) : [];
            loaded = true;
        } catch (err) {
            error.value = err instanceof Error ? err.message : 'Ukjent feil.';
            loaded = false;
            throw err;
        } finally {
            loading.value = false;
        }
    }

    function ensureLoaded(): Promise<void> {
        if (loaded) {
            return Promise.resolve();
        }
        if (!loadPromise) {
            loadPromise = load().finally(() => {
                loadPromise = null;
            });
        }
        return loadPromise;
    }

    function getChildren(parentId: string | null): Tag[] {
        return tags.value.filter(tag => tag.parentId === parentId);
    }

    function getById(id: string): Tag | undefined {
        return tags.value.find(tag => tag.id === id);
    }

    function hasChildren(tagId: string): boolean {
        return tags.value.some(tag => tag.parentId === tagId);
    }

    return { tags, loading, error, ensureLoaded, getChildren, getById, hasChildren };
});
