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
            <h2> Mine Intresser </h2>
            <ul v-if="userInfo.interests">
                <li v-for="interest in userInfo.interests">
                    {{ interest }}
                </li>
            </ul>
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
    import { computed } from 'vue';
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';

    import type { User } from '@/types/stores/userAuth';

    // --- State Management
    const authStore = useAuthStore();
    const { user } = storeToRefs(authStore);

    const userInfo = computed<User | null>(() => user.value)

    //  --- Debug Logic
    //console.log(userName)

    //console.log(user)
</script>