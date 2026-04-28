<template>
    <article class="flex-column-justify-space-evenly-items-center profile-container" v-if="!!userInfo">
        <h2> Profile Informasjon </h2>
        <div class="profile-links">
            <a href="/profile/edit">Rediger min side</a>
            <a href="/profile/my-requests">My Join Requests</a>
        </div>
        <header class="flex-column-justify-space-evenly-items-center profile-content">
            <p>Bruker Navn : <a :href="userInfo?.id ? `https://discordapp.com/users/${userInfo.id}` : '#'" target="_blank">{{ userInfo?.username }}</a></p>
            <p v-if="userInfo?.email">Epost : <a :href="`mailto:${userInfo.email}`">{{ userInfo.email }}</a></p>
            <p v-if="userInfo && 'phone' in userInfo && userInfo.phone">Telefon : <a :href="`tel:${userInfo.phone}`">{{ userInfo.phone }}</a></p>
            <address>
                <p>Fylke / Kommune: {{(userInfo as any)?.location?.county || 'Ikke lagt til'}},{{(userInfo as any)?.location?.city || 'Ikke lagt til'}}</p>
            </address>
        </header>
    
        <main class="flex-wrap-row-justify-space-evenly">
            <section>
                <h2> Mine interesser </h2>
                <p v-if="tagsLoading" class="muted">Laster interesser...</p>
                <p v-else-if="tagsError" class="error">{{ tagsError }}</p>
                <ul v-else-if="userTags.length">
                    <li v-for="tag in userTags" :key="tag.id">
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

    const authStore = useAuthStore();
    const { user } = storeToRefs(authStore);
    const userInfo = computed<User | null>(() => user.value)

    interface UserTag {
        id: string;
        name: string;
        description?: string;
        category?: string;
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
.profile-links {
    display: flex;
    gap: 1rem;
    margin-bottom: 1rem;
}

.profile-links a {
    padding: 0.4rem 0.75rem;
    border-radius: 6px;
    border: 1px solid #0f5ed8;
    color: #0f5ed8;
    text-decoration: none;
    font-size: 0.9rem;
    transition: background 0.2s;
}

.profile-links a:hover {
    background: #0f5ed8;
    color: #fff;
}
</style>