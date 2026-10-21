<script setup lang="ts">
import { ref } from 'vue';
import axios from 'axios';
import { useAuth0 } from '@auth0/auth0-vue';

// Test page for the Auth0 login. It is not linked from the menu.
// Flow: Discord -> Auth0 -> this page -> access token -> /api/users/me
const { isAuthenticated, isLoading, error, user, loginWithRedirect, logout, getAccessTokenSilently } = useAuth0();

const baseApi = import.meta.env.VITE_BASE_API ?? '';
const output = ref('Nothing yet.');
const auth0Keys = Object.keys(localStorage).filter(k => k.startsWith('@@auth0spajs@@')).length;
const pageUrl = window.location.href;

function show(value: unknown) {
    output.value = typeof value === 'string' ? value : JSON.stringify(value, null, 2);
}

function login() {
    // connection: 'discord' skips the Auth0 login box and goes straight to Discord
    loginWithRedirect({
        authorizationParams: { connection: 'discord', redirect_uri: window.location.origin + '/auth-test' },
        appState: { target: '/auth-test' },
    });
}

function logoutNow() {
    logout({ logoutParams: { returnTo: window.location.origin } });
}

async function callMeWithoutToken() {
    try {
        const res = await axios.get(`${baseApi}/api/users/me`);
        show({ status: res.status, data: res.data });
    } catch (e: any) {
        show({ status: e.response?.status ?? 'no response', note: 'Expected: 401 (no token)' });
    }
}

async function callMeWithToken() {
    try {
        const token = await getAccessTokenSilently();
        const res = await axios.get(`${baseApi}/api/users/me`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        show({ status: res.status, data: res.data });
    } catch (e: any) {
        show({ status: e.response?.status ?? 'error', error: e.response?.data ?? e.message });
    }
}

async function showTokenClaims() {
    try {
        const token = await getAccessTokenSilently();
        const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
        show(JSON.parse(atob(payload)));
    } catch (e: any) {
        show(e.message);
    }
}
</script>

<template>
    <main class="container">
        <h1>Auth0 test</h1>
        <p v-if="error" style="color: red">Auth0 error: {{ error.message }}</p>
        <p style="font-size: 0.8em; color: gray">Debug: url = {{ pageUrl }} | auth0 items in browser = {{ auth0Keys }} | isAuthenticated = {{ isAuthenticated }} | isLoading = {{ isLoading }}</p>
        <p v-if="isLoading">Loading...</p>
        <template v-else>
            <p v-if="isAuthenticated">Logged in as {{ user?.name }}</p>
            <p v-else>Not logged in.</p>

            <p>
                <button v-if="!isAuthenticated" @click="login">Log in with Discord</button>
                <button v-else @click="logoutNow">Log out</button>
            </p>

            <p>
                <button @click="callMeWithoutToken">GET /api/users/me without token</button>
                <button :disabled="!isAuthenticated" @click="callMeWithToken">GET /api/users/me with token</button>
                <button :disabled="!isAuthenticated" @click="showTokenClaims">Show access token claims</button>
            </p>

            <pre>{{ output }}</pre>
        </template>
    </main>
</template>
