<template>
  <section>
    <h2>Discord-konto</h2>

    <p v-if="loading">Henter Discord-status...</p>

    <template v-else>
      <p v-if="error" class="error">{{ error }}</p>
      <p v-if="success" class="success">{{ success }}</p>

      <template v-if="isLinked">
        <p><strong>Status:</strong> Discord-konto er koblet til &#10003;</p>
        <p><strong>Discord ID:</strong> {{ discordId }}</p>
        <p><strong>Brukernavn:</strong> {{ discordUsername }}</p>
        <button @click="handleUnlink" :disabled="isProcessing">
          {{ isProcessing ? 'Fjerner kobling...' : 'Fjern kobling til Discord' }}
        </button>
      </template>

      <template v-else>
        <p><strong>Status:</strong> Ingen Discord-konto koblet til</p>
        <p>Koble til Discord-kontoen din for a bli med i team og delta i fellesskapet.</p>
        <button @click="handleLink" :disabled="isProcessing">
          {{ isProcessing ? 'Kobler til...' : 'Koble til Discord-konto' }}
        </button>
      </template>
    </template>
  </section>
</template>

<script lang="ts" setup>
import { onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/authStore';
import {
  getDiscordAccountStatus,
  linkDiscordAccount,
  unlinkDiscordAccount,
} from '@/services/discordLinkingApi';

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

const loading = ref(true);
const isProcessing = ref(false);
const error = ref<string | null>(null);
const success = ref<string | null>(null);
const isLinked = ref(false);
const discordId = ref<string | null>(null);
const discordUsername = ref<string | null>(null);

const fetchStatus = async () => {
  const currentDiscordId = user.value?.id;
  
  if (!currentDiscordId) {
    isLinked.value = false;
    loading.value = false;
    return;
  }
  
  loading.value = true;
  error.value = null;
  
  try {
    console.log('Fetching status for Discord ID:', currentDiscordId);
    const status = await getDiscordAccountStatus(currentDiscordId);
    isLinked.value = status.isLinked;
    discordId.value = status.discordId;
    discordUsername.value = status.username;
  } catch (err) {
    if (err instanceof Error && err.message.includes('404')) {
      isLinked.value = false;
      discordId.value = null;
      discordUsername.value = null;
    } else {
      error.value = err instanceof Error ? err.message : 'Failed to fetch Discord status';
    }
  } finally {
    loading.value = false;
  }
};

const handleLink = async () => {
  const currentDiscordId = user.value?.id;
  if (!currentDiscordId) return;
  
  isProcessing.value = true;
  error.value = null;
  success.value = null;
  
  try {
    console.log('Linking Discord ID:', currentDiscordId);
    await linkDiscordAccount(currentDiscordId);
    success.value = 'Discord account linked successfully!';
    
    await new Promise(resolve => setTimeout(resolve, 500));
    await fetchStatus();
    
    setTimeout(() => {
      success.value = null;
    }, 3000);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to link Discord account';
  } finally {
    isProcessing.value = false;
  }
};

const handleUnlink = async () => {
  const currentDiscordId = user.value?.id;
  if (!currentDiscordId) return;
  
  if (!confirm('Are you sure you want to unlink your Discord account?')) return;
  
  isProcessing.value = true;
  
  try {
    await unlinkDiscordAccount(currentDiscordId);
    success.value = 'Account unlinked. Logging out...';
    
    setTimeout(() => {
      authStore.logout();
      window.location.href = '/';
    }, 2000);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to unlink account';
    isProcessing.value = false;
  }
};
onMounted(fetchStatus);
</script>

<style scoped>
/* Per review feedback: let Pico style the section, paragraphs, and
   button by default. Only two tiny, genuinely necessary overrides
   remain — a color cue for error vs success text, since Pico's
   classless mode has no built-in concept of "this text means bad
   news" vs "this text means good news". Everything else (backgrounds,
   boxes, button colors) that used to live here is gone; Pico's
   defaults are used as-is. */
.error {
  color: #842029;
}

.success {
  color: #0f5132;
}
</style>