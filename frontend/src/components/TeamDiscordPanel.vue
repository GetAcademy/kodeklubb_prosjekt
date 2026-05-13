<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useRoute } from "vue-router";
import {
  getTeamDiscordInfo,
  setTeamDiscordConfig,
  updateTeamDiscordConfig,
  removeTeamDiscordConfig,
  syncTeamWithDiscord
} from "@/services/discordApi";

const props = defineProps<{
  isAdmin: boolean;
}>();

const route = useRoute();
const teamId = computed(() => route.params.teamId as string);

const loading = ref(true);
const saving = ref(false);
const syncing = ref(false);
const error = ref<string | null>(null);
const success = ref<string | null>(null);

const discordServerId = ref<string | null>(null);
const discordChannelId = ref<string | null>(null);
const discordRoleId = ref<string | null>(null);
const discordLink = ref<string | null>(null);

const hasDiscord = computed(() =>
  !!discordServerId.value &&
  !!discordChannelId.value &&
  !!discordRoleId.value
);

async function load() {
  loading.value = true;
  error.value = null;

  try {
    const data = await getTeamDiscordInfo(teamId.value);

    discordServerId.value = data.discord_server_id ?? null;
    discordChannelId.value = data.discord_channel_id ?? null;
    discordRoleId.value = data.discord_role_id ?? null;
    discordLink.value = data.discord_link ?? null;
  } catch (e: any) {
    if (e?.response?.status !== 404) {
      error.value = "Kunne ikke hente Discord-informasjon.";
    }
  }

  loading.value = false;
}

async function save() {
  saving.value = true;
  error.value = null;
  success.value = null;

  const payload = {
    discordServerId: discordServerId.value!,
    discordChannelId: discordChannelId.value!,
    discordRoleId: discordRoleId.value!,
    discordLink: discordLink.value
  };

  try {
    if (!hasDiscord.value) {
      await setTeamDiscordConfig(teamId.value, payload);
      success.value = "Discord-konfigurasjon lagret.";
    } else {
      await updateTeamDiscordConfig(teamId.value, payload);
      success.value = "Discord-konfigurasjon oppdatert.";
    }
  } catch {
    error.value = "Kunne ikke lagre Discord-konfigurasjon.";
  }

  saving.value = false;
}

async function removeConfig() {
  saving.value = true;
  error.value = null;
  success.value = null;

  try {
    await removeTeamDiscordConfig(teamId.value);

    discordServerId.value = null;
    discordChannelId.value = null;
    discordRoleId.value = null;
    discordLink.value = null;

    success.value = "Discord-konfigurasjon fjernet.";
  } catch {
    error.value = "Kunne ikke fjerne konfigurasjon.";
  }

  saving.value = false;
}

async function syncMembers() {
  syncing.value = true;
  error.value = null;
  success.value = null;

  try {
    const res = await syncTeamWithDiscord(teamId.value);
    success.value = `${res.message} (synced: ${res.synced}, failed: ${res.failed})`;
  } catch {
    error.value = "Sync feilet.";
  }

  syncing.value = false;
}

function openDiscord() {
  if (discordLink.value) {
    window.open(discordLink.value, "_blank");
  }
}

onMounted(load);
</script>

<template>
  <section class="discord-panel">
    <h2>Discord</h2>

    <div v-if="loading">Laster Discord-informasjon…</div>

    <div v-else>
      <div v-if="error" class="error">{{ error }}</div>
      <div v-if="success" class="success">{{ success }}</div>

      <!-- Public view -->
      <div class="status">
        <p v-if="hasDiscord">Discord er koblet til dette teamet.</p>
        <p v-else>Dette teamet har ikke koblet til Discord ennå.</p>

        <div v-if="discordLink">
          <p>Invitasjonslenke: <a :href="discordLink" target="_blank">{{ discordLink }}</a></p>
          <button class="discord-login-btn" @click="openDiscord">
            <img src="https://cdn.jsdelivr.net/gh/edent/SuperTinyIcons/images/svg/discord.svg" alt="Discord" class="discord-icon" />
            Åpne Discord
          </button>
        </div>
      </div>

      <!-- Admin view -->
      <div v-if="isAdmin" class="admin">
        <h3>Administrer Discord</h3>

        <label>Server ID</label>
        <input v-model="discordServerId" type="text" />

        <label>Channel ID</label>
        <input v-model="discordChannelId" type="text" />

        <label>Role ID</label>
        <input v-model="discordRoleId" type="text" />

        <label>Invitasjonslenke</label>
        <input v-model="discordLink" type="text" />

        <div class="buttons">
          <button @click="save" :disabled="saving">
            {{ hasDiscord ? "Oppdater" : "Lagre" }}
          </button>

          <button @click="removeConfig" :disabled="saving || !hasDiscord">
            Fjern konfigurasjon
          </button>

          <button @click="syncMembers" :disabled="syncing || !hasDiscord">
            Sync medlemmer
          </button>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.discord-panel {
  margin-top: 2rem;
  padding: 1rem;
  border: 1px solid #ddd;
  border-radius: 8px;
}
label {
  font-weight: bold;
  margin-top: 0.5rem;
}
input {
  width: 100%;
  padding: 0.4rem;
  margin-bottom: 0.5rem;
}
.buttons {
  display: flex;
  gap: 0.5rem;
  margin-top: 1rem;
}
.error {
  color: #b00020;
}
.success {
  color: #0a7a0a;
}
.discord-login-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #5865f2;
  color: #fff;
  border: none;
  border-radius: 6px;
  padding: 0.7rem 1.5rem;
  font-size: 1.1rem;
  font-weight: 600;
  cursor: pointer;
  margin-top: 0.5rem;
  transition: background 0.2s;
}
.discord-login-btn:hover {
  background: #4752c4;
}
.discord-icon {
  width: 1.5rem;
  height: 1.5rem;
}
</style>
