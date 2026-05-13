<template>
  <section class="discord-linking-section">
    <h2>Discord Account</h2>
    
    <div v-if="loading" class="loading">Loading Discord status...</div>
    
    <div v-else>
      <div v-if="error" class="error-message">{{ error }}</div>
      <div v-if="success" class="success-message">{{ success }}</div>

      <div class="status-box">
        <div v-if="isLinked" class="linked">
          <p><strong>Status:</strong> Discord account is linked ✓</p>
          <p><strong>Discord ID:</strong> {{ discordId }}</p>
          <p><strong>Username:</strong> {{ discordUsername }}</p>
          <button class="btn-unlink" @click="handleUnlink" :disabled="isProcessing">
            {{ isProcessing ? 'Unlinking...' : 'Unlink Discord Account' }}
          </button>
        </div>
        <div v-else class="not-linked">
          <p><strong>Status:</strong> No Discord account linked</p>
          <p>Link your Discord account to join teams and participate in community features.</p>
          <button class="btn-link" @click="handleLink" :disabled="isProcessing">
            {{ isProcessing ? 'Linking...' : 'Link Discord Account' }}
          </button>
        </div>
      </div>
    </div>
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
    // Try the direct method first
    console.log('Fetching status for Discord ID:', currentDiscordId);
    const status = await getDiscordAccountStatus(currentDiscordId);
    isLinked.value = status.isLinked;
    discordId.value = status.discordId;
    discordUsername.value = status.username;
  } catch (err) {
    // If not found, user may have been unlinked
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
    
    // Wait a moment then fetch updated status
    await new Promise(resolve => setTimeout(resolve, 500));
    await fetchStatus();
    
    // Clear success message after 3 seconds
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
    
    // Log out and redirect after 2 seconds
    setTimeout(() => {
      authStore.logout();
      window.location.href = '/';  // Redirect to home (should show login)
    }, 2000);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to unlink account';
    isProcessing.value = false;
  }
};
onMounted(fetchStatus);
</script>

<style scoped>
.discord-linking-section {
  margin: 2rem 0;
  padding: 1.5rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  background: #f9f9f9;
}

.discord-linking-section h2 {
  margin-top: 0;
  margin-bottom: 1rem;
  font-size: 1.3rem;
}

.status-box {
  margin-top: 1rem;
  padding: 1rem;
  border-radius: 6px;
  background: #fff;
  border: 1px solid #e0e0e0;
}

.linked {
  color: #0f5132;
  background: #d1e7dd;
  padding: 1rem;
  border-radius: 6px;
  border: 1px solid #badbcc;
}

.linked p {
  margin: 0.5rem 0;
}

.not-linked {
  color: #664d03;
  background: #fff3cd;
  padding: 1rem;
  border-radius: 6px;
  border: 1px solid #ffecb5;
}

.not-linked p {
  margin: 0.5rem 0;
}

.btn-link,
.btn-unlink {
  margin-top: 1rem;
  padding: 0.6rem 1.2rem;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-link {
  background: #5865f2;
  color: #fff;
}

.btn-link:hover:not(:disabled) {
  background: #4752c4;
}

.btn-unlink {
  background: #dc3545;
  color: #fff;
}

.btn-unlink:hover:not(:disabled) {
  background: #bb2d3b;
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.loading {
  text-align: center;
  color: #666;
  padding: 1rem;
}

.error-message {
  color: #842029;
  background: #f8d7da;
  border: 1px solid #f5c2c7;
  padding: 0.75rem 1rem;
  border-radius: 6px;
  margin-bottom: 1rem;
}

.success-message {
  color: #0f5132;
  background: #d1e7dd;
  border: 1px solid #badbcc;
  padding: 0.75rem 1rem;
  border-radius: 6px;
  margin-bottom: 1rem;
}
</style>
