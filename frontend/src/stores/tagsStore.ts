import { defineStore } from 'pinia';
import { ref } from 'vue';

export interface Tag {
    id: string;
    name: string;
    parentId: string | null;
    openForChildSuggestions: boolean;
}

// Raw shape as returned by the API (PascalCase, since the backend's
// JSON serializer is configured with PropertyNamingPolicy = null).
interface RawTag {
    Id: string;
    Name: string;
    ParentId: string | null;
    OpenForChildSuggestions: boolean;
}

function normalize(raw: RawTag): Tag {
    return {
        id: raw.Id,
        name: raw.Name,
        parentId: raw.ParentId,
        openForChildSuggestions: raw.OpenForChildSuggestions,
    };
}

export const useTagsStore = defineStore('tags', () => {
    const tags = ref<Tag[]>([]);
    const loading = ref(false);
    const error = ref('');

    // Tracks the in-flight load so concurrent callers await the same
    // request instead of firing multiple fetches.
    let loadPromise: Promise<void> | null = null;
    let loaded = false;

    async function load() {
        loading.value = true;
        error.value = '';
        try {
            const baseApi = import.meta.env.VITE_BASE_API || '';
            const response = await fetch(`${baseApi}/api/tags`);
            if (!response.ok) {
                throw new Error('Kunne ikke hente tags.');
            }
            const payload: RawTag[] = await response.json();
            tags.value = Array.isArray(payload) ? payload.map(normalize) : [];
            loaded = true;
        } catch (err) {
            error.value = err instanceof Error ? err.message : 'Ukjent feil.';
            // Allow a retry on the next ensureLoaded() call since the load failed.
            loaded = false;
            throw err;
        } finally {
            loading.value = false;
        }
    }

    /**
     * Ensures the tag list has been loaded (or is currently loading).
     * Safe to call from any component; only triggers one network request
     * for the lifetime of the app, even if called many times concurrently.
     */
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

    /**
     * Returns only the direct children of the given parent.
     * Pass null to get top-level (root) tags.
     */
    function getChildren(parentId: string | null): Tag[] {
        return tags.value.filter(tag => tag.parentId === parentId);
    }

    function getById(id: string): Tag | undefined {
        return tags.value.find(tag => tag.id === id);
    }

    return { tags, loading, error, ensureLoaded, getChildren, getById };
});