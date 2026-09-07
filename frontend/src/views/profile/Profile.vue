<template>
    <article v-if="!!userInfo">
        <h2>Profilinformasjon</h2>

        <dl>
            <dt>Brukernavn</dt>
            <dd>
                <a :href="userInfo?.id ? `https://discordapp.com/users/${userInfo.id}` : '#'" target="_blank">{{ userInfo?.username }}</a>
            </dd>

            <template v-if="userInfo?.email">
                <dt>E-post</dt>
                <dd><a :href="`mailto:${userInfo.email}`">{{ userInfo.email }}</a></dd>
            </template>

            <template v-if="userInfo && 'phone' in userInfo && userInfo.phone">
                <dt>Telefon</dt>
                <dd><a :href="`tel:${userInfo.phone}`">{{ userInfo.phone }}</a></dd>
            </template>

            <dt>Fylke / kommune</dt>
            <dd>{{(userInfo as any)?.location?.county || 'Ikke lagt til'}}, {{(userInfo as any)?.location?.city || 'Ikke lagt til'}}</dd>
        </dl>

        <DiscordLinking />

        <section>
            <h2>Mine interesser</h2>
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
            <h2>Anvendelsesomrader</h2>
            <ul v-if="(userInfo as any)?.interest && Array.isArray((userInfo as any).interest.scope)">
                <li v-for="scope in (userInfo as any).interest.scope" :key="scope">
                    {{ scope }}
                </li>
            </ul>
            <p v-else class="muted">Ingen omrader lagt til enda.</p>
        </section>
    </article>
</template>

<script lang="ts" setup>
    import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';
    import type { User } from '@/types/stores/userAuth';
    import DiscordLinking from '@/components/profile/DiscordLinking.vue';

    // Pico's classless stylesheet. Using the `?url` suffix gets Vite to
    // hand back just the built file's URL as a plain string, WITHOUT
    // automatically injecting it into the page — unlike a plain side-
    // effect import, which only ever runs once per session (ES modules
    // are cached after their first evaluation), meaning it would never
    // re-fire on a second visit to this page within the same session.
    // Managing the actual <link> tag ourselves, explicitly, in
    // onMounted/onUnmounted below, means it's correctly added and
    // removed every single time this page is entered and left — not
    // just the first time.
    import picoHref from '@picocss/pico/css/pico.classless.min.css?url';

    let picoLinkEl: HTMLLinkElement | null = null;

    onMounted(() => {
        picoLinkEl = document.createElement('link');
        picoLinkEl.rel = 'stylesheet';
        picoLinkEl.href = picoHref;
        picoLinkEl.setAttribute('data-pico-page', 'profile');
        document.head.appendChild(picoLinkEl);
    });

    onUnmounted(() => {
        picoLinkEl?.remove();
        picoLinkEl = null;
    });

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