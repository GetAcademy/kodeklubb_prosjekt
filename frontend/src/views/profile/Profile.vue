<template>
    <article class="flex-column-justify-space-evenly-items-center profile-container" v-if="!!userInfo">
        <h2> Profile Informasjon </h2><a href="/profile/edit">Rediger min side</a>
        <header class="flex-column-justify-space-evenly-items-center profile-content">
            <p>Bruker Navn : <a href="https://discordapp.com/users/{{user.id}}" target = "_blank">{{ userInfo.username }}</a></p>
            <p v-if="!!userInfo.email">Epost : <a href="mail:{{ user.email }}">{{ userInfo.email }}</a></p>
            <p v-if="!!userInfo.phone">Telefon : <a href="mail:{{ user.phone }}">{{ userInfo.phone }}</a></p>
            <address>
                <p>Fylke / Kommune: {{!!userInfo.location ? userInfo.location.county : 'Ikke lagt til'}},{{!!userInfo.location ? userInfo.location.city : 'Ikke lagt til'}}</p>
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
            <ul v-if="userInfo.interest">
                <li v-for="scope in userInfo.interest.scope">
                    {{ scope }}
                </li>
            </ul>
        </section>
    </main>

    <footer>

    </footer>
</article>

</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { computed, onMounted, ref, watch } from 'vue';
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';

    import type { User } from '@/types/stores/userAuth';

    // --- State Management
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
            if (!response.ok) {
                throw new Error('Kunne ikke hente interesser.');
            }
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
        if (user.value?.id) {
            await fetchUserTags();
        }
    });

    //  --- Debug Logic
    //console.log(userName)

    //console.log(user)
</script>