<template>
    <article class="flex-wrap-row-justify-space-evenly profile-container">
        <section>
            <h2> Profile Information </h2>
            <p>Bruker Navn : <a href="discord://-/users/{{ user.id }}">{{ userName }}</a></p>
            <!--p v-if="!!user.email">Bruker Email : <a href="mail:{{ user.email }}">{{ user.email }}</a></p>
            <p v-if="!!user.phone">Telefon : <a href="mail:{{ user.phone }}">{{ user.phone }}</a></p-->
            <address>
                <!--p v-if="!!user.location">komune: {{user.location.city}}, {{user.location.zip}}</p-->
            </address>
        </section>
    
    <!--section v-if="!!user.intrest">
        <h2> Mine Intresser </h2>
        <section>
            <ul>
                <li v-for="interest in user.interests">
                    {{ interest }}
                </li>
            </ul>
        </section>
    </section>
    <section v-if="!!user.intrest">
        <h2> Anvendelses Områder </h2>
        <section>
            <ul>
                <li v-for="interest in user.interests">
                    {{ interest }}
                </li>
            </ul>
        </section>
    </section-->
    <section>
        {{ user }}
    </section>
    </article>


</template>

<script lang="ts" setup>

    // --- Importing Dependencies & Types
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores/authStore';

    // --- State Management
    const authStore = useAuthStore();
    const { user, isAuthenticated, userName } = storeToRefs(authStore);


</script>