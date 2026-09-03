<template>
    <article class="flex-column-justify-space-evenly-items-center profile-container" v-if="!!userInfo">
        <h2> Profile Informasjon </h2>
        <header class="flex-column-justify-space-evenly-items-center profile-content">
            <p>Bruker Navn : <a :href="userInfo?.id ? `https://discordapp.com/users/${userInfo.id}` : '#'" target="_blank">{{ userInfo?.username }}</a></p>
            <p v-if="userInfo?.email">Epost : <a :href="`mailto:${userInfo.email}`">{{ userInfo.email }}</a></p>
            <p v-if="userInfo && 'phone' in userInfo && userInfo.phone">Telefon : <a :href="`tel:${userInfo.phone}`">{{ userInfo.phone }}</a></p>
            <address>
                <p>Fylke / Kommune: {{(userInfo as any)?.location?.county || 'Ikke lagt til'}},{{(userInfo as any)?.location?.city || 'Ikke lagt til'}}</p>
            </address>
        </header>

        <DiscordLinking />

        <main class="flex-wrap-row-justify-space-evenly">
            <section>
                <h2> Mine interesser </h2>
                <p v-if="tagsLoading" class="muted">Laster interesser...</p>
                <p v-else-if="tagsError" class="error">{{ tagsError }}</p>
                <ul v-else-if="userTags.length" class="tags-list">
                    <li v-for="tag in userTags" :key="tag.id" class="tag-badge">
                        {{ tag.name }}
                    </li>
                </ul>
                <p v-else class="muted">Ingen interesser lagt til enda.</p>
            </section>

            <section>
                <h2> Anvendelses Områder </h2>
                <ul v-if="(userInfo as any)?.interest && Array.isArray((userInfo as any).interest.scope)">
                    <li v-for="scope in (userInfo as any).interest.scope" :key="scope">
                        {{ scope }}
                    </li>
                </ul>
                <p v-else class="muted">Ingen områder lagt til enda.</p>
            </section>
        </main>
        <footer></footer>
    </article>
</template>

<script lang="ts" setup>
    import { computed, onMounted, ref, watch } from 'vue';
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';
    import type { User } from '@/types/stores/userAuth';
    import DiscordLinking from '@/components/profile/DiscordLinking.vue';

    // Pico's classless stylesheet, imported here rather than as a global
    // main.ts import. Since Profile.vue is lazy-loaded by the router,
    // Vite bundles this CSS into this component's own dynamic chunk —
    // it only loads when someone actually visits this page, and never
    // affects any other page in the app. Styles article/header/main/
    // section/a/h2 automatically; the existing scoped .tags-list/
    // .tag-badge/.muted/.error rules below still take precedence for
    // those specific classes, since Vue's scoped-style specificity wins.
    import '@picocss/pico/css/pico.classless.min.css';

    const authStore = useAuthStore();
    const { user } = storeToRefs(authStore);
    const userInfo = computed<User | null>(() => user.value)

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
            if (!response.ok) throw new Error('Kunne ikke hente interesser.');
            const payload = await response.json();
            userTags.value = Array.isArray(payload) ? payload : (payload?.tags ?? []);
        } catch (error) {
            tagsError.value = error instanceof Error ? error.message : 'Ukjent feil.';
        } finally {
            tagsLoading.value = false;
        }
    };

    watch(user, async () => {
        const discordId = user.value?.id || null;
        if (!discordId || lastFetchedDiscordId.value === discordId) return;
        lastFetchedDiscordId.value = discordId;
        await fetchUserTags();
    }, { immediate: true });

    onMounted(async () => {
        if (user.value?.id) await fetchUserTags();
    });
</script>

<style scoped>
.profile-container {
    max-width: 640px;
    margin: 0 auto;
    padding: 24px;
    font-size: 0.9rem;
}

.profile-container :deep(h2) {
    font-size: 1.3rem;
}

.profile-container :deep(p) {
    font-size: 0.9rem;
}

.tags-list {
    list-style: none;
    padding: 0;
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-top: 0.5rem;
}

.tag-badge {
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