<template>
  <section class="edit-team">
    <div class="edit-team__header">
      <router-link :to="`/teams/${teamId}`" class="back-link">← Tilbake til team</router-link>
      <h1>Rediger team</h1>
    </div>

    <p v-if="pageLoading" class="muted">Laster teamdetaljer…</p>
    <p v-else-if="pageError" class="error">{{ pageError }}</p>

    <template v-else>

      <!-- General info -->
      <section class="card">
        <h2>Generell informasjon</h2>
        <div v-if="saveError" class="alert alert--error">{{ saveError }}</div>
        <div v-if="saveSuccess" class="alert alert--success">{{ saveSuccess }}</div>

        <label for="teamName">Teamnavn</label>
        <input id="teamName" v-model="name" type="text" placeholder="Navn på teamet" />

        <label for="teamDesc">Beskrivelse</label>
        <textarea id="teamDesc" v-model="description" rows="4" placeholder="Hva handler teamet om?" />

        <label for="meetingTime">Møtetidspunkt</label>
        <input id="meetingTime" v-model="meetingSchedule" type="text" placeholder="f.eks. Onsdager kl. 18:00" />

        <button class="btn btn--primary" @click="saveGeneral" :disabled="saving">
          {{ saving ? 'Lagrer…' : 'Lagre endringer' }}
        </button>
      </section>

      <!-- Discord settings -->
      <section class="card card--discord">
        <div class="card__title-row">
          <img
            src="https://cdn.jsdelivr.net/gh/edent/SuperTinyIcons/images/svg/discord.svg"
            alt="Discord"
            class="discord-icon"
          />
          <h2>Discord-innstillinger</h2>
        </div>
        <p class="muted">
          Koble teamet til en Discord-server. Boten legger automatisk til godkjente brukere i serveren.
        </p>

        <!-- Discord invite link -->
        <div v-if="linkError" class="alert alert--error">{{ linkError }}</div>
        <div v-if="linkSuccess" class="alert alert--success">{{ linkSuccess }}</div>

        <label for="discordLink">Invitasjonslenke</label>
        <div class="input-row">
          <input
            id="discordLink"
            v-model="discordInviteLink"
            type="url"
            placeholder="https://discord.gg/XXXX"
            class="input-row__field"
          />
          <button class="btn btn--discord" @click="saveInviteLink" :disabled="savingLink">
            {{ savingLink ? 'Lagrer…' : 'Lagre' }}
          </button>
        </div>

        <p v-if="discordInviteLink" class="link-preview">
          Forhåndsvisning:
          <a :href="discordInviteLink" target="_blank" rel="noopener">{{ discordInviteLink }}</a>
        </p>

        <!-- Discord server ID -->
        <div v-if="serverIdError" class="alert alert--error">{{ serverIdError }}</div>
        <div v-if="serverIdSuccess" class="alert alert--success">{{ serverIdSuccess }}</div>

        <label for="discordServerId">Discord Server ID</label>
        <p class="muted">
          Høyreklikk på serveren i Discord → Kopier server-ID (krever utviklermodus aktivert i Discord-innstillinger).
        </p>
        <div class="input-row">
          <input
            id="discordServerId"
            v-model="discordServerId"
            type="text"
            placeholder="f.eks. 1506214293354451067"
            class="input-row__field"
          />
          <button class="btn btn--discord" @click="saveServerId" :disabled="savingServerId">
            {{ savingServerId ? 'Lagrer…' : 'Lagre' }}
          </button>
        </div>
      </section>

    </template>
  </section>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { storeToRefs } from 'pinia';
import { setTeamDiscordInviteLink, setTeamDiscordConfig } from '@/services/discordApi';

const route = useRoute();
const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

const teamId = computed(() => route.params.teamId as string);

// Page state
const pageLoading = ref(false);
const pageError = ref<string | null>(null);

// General info fields
const name = ref('');
const description = ref('');
const meetingSchedule = ref('');
const saving = ref(false);
const saveError = ref<string | null>(null);
const saveSuccess = ref<string | null>(null);

// Discord invite link
const discordInviteLink = ref('');
const savingLink = ref(false);
const linkError = ref<string | null>(null);
const linkSuccess = ref<string | null>(null);

// Discord server ID
const discordServerId = ref('');
const savingServerId = ref(false);
const serverIdError = ref<string | null>(null);
const serverIdSuccess = ref<string | null>(null);

async function loadTeam() {
  pageLoading.value = true;
  pageError.value = null;
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const discordId = user.value?.id;
    const url = `${baseApi}/api/discover/${teamId.value}` + (discordId ? `?discordId=${discordId}` : '');
    const res = await fetch(url);
    if (!res.ok) {
      pageError.value = res.status === 404 ? 'Team ikke funnet.' : 'Kunne ikke laste teamdetaljer.';
      return;
    }
    const payload = await res.json();
    const team = payload.team ?? payload;

    name.value = team.name ?? '';
    description.value = team.description ?? '';
    meetingSchedule.value = team.meetingSchedule ?? team.meeting_schedule ?? '';
    discordInviteLink.value = team.DiscordLink ?? team.discordLink ?? team.discord_link ?? '';
    discordServerId.value = team.DiscordServerId ?? team.discordServerId ?? team.discord_server_id ?? '';
  } catch {
    pageError.value = 'Noe gikk galt under lasting.';
  } finally {
    pageLoading.value = false;
  }
}

async function saveGeneral() {
  saving.value = true;
  saveError.value = null;
  saveSuccess.value = null;
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const res = await fetch(`${baseApi}/api/discover/${teamId.value}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: name.value,
        description: description.value,
        meetingSchedule: meetingSchedule.value,
      }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Kunne ikke lagre endringer.');
    }
    saveSuccess.value = '✅ Endringer lagret!';
  } catch (err) {
    saveError.value = err instanceof Error ? err.message : 'Ukjent feil.';
  } finally {
    saving.value = false;
  }
}

async function saveInviteLink() {
  savingLink.value = true;
  linkError.value = null;
  linkSuccess.value = null;
  try {
    await setTeamDiscordInviteLink(teamId.value, discordInviteLink.value.trim() || null);
    linkSuccess.value = '✅ Discord invitasjonslenke lagret!';
  } catch (err) {
    linkError.value = err instanceof Error ? err.message : 'Kunne ikke lagre lenken.';
  } finally {
    savingLink.value = false;
  }
}

async function saveServerId() {
  if (!discordServerId.value.trim()) {
    serverIdError.value = 'Server ID kan ikke være tom.';
    return;
  }
  savingServerId.value = true;
  serverIdError.value = null;
  serverIdSuccess.value = null;
  try {
    await setTeamDiscordConfig(teamId.value, {
      discordServerId: discordServerId.value.trim(),
      discordChannelId: '',
      discordRoleId: '',
      discordLink: discordInviteLink.value.trim() || null,
    });
    serverIdSuccess.value = '✅ Discord server ID lagret!';
  } catch (err) {
    serverIdError.value = err instanceof Error ? err.message : 'Kunne ikke lagre server ID.';
  } finally {
    savingServerId.value = false;
  }
}

onMounted(loadTeam);
</script>

<style scoped>
.edit-team {
  max-width: 680px;
  margin: 0 auto;
  padding: 2rem 1.25rem;
}

.edit-team__header {
  margin-bottom: 1.75rem;
}

.back-link {
  font-size: 0.9rem;
  color: #555;
  text-decoration: none;
  display: inline-block;
  margin-bottom: 0.5rem;
}

.back-link:hover {
  color: #222;
  text-decoration: underline;
}

h1 {
  margin: 0;
  font-size: 1.6rem;
}

.card {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.05);
}

.card--discord {
  border-color: #5865f2;
}

.card__title-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 0.4rem;
}

.card__title-row h2 {
  margin: 0;
}

.discord-icon {
  width: 1.6rem;
  height: 1.6rem;
}

h2 {
  font-size: 1.15rem;
  margin-top: 0;
  margin-bottom: 1rem;
}

label {
  display: block;
  font-weight: 600;
  font-size: 0.9rem;
  margin-bottom: 0.3rem;
  margin-top: 0.9rem;
}

label:first-of-type {
  margin-top: 0;
}

input[type="text"],
input[type="url"],
textarea {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid #ccc;
  border-radius: 6px;
  font-size: 0.95rem;
  box-sizing: border-box;
  font-family: inherit;
  margin-bottom: 0.25rem;
}

textarea {
  resize: vertical;
}

.input-row {
  display: flex;
  gap: 0.5rem;
  align-items: stretch;
}

.input-row__field {
  flex: 1;
  margin-bottom: 0;
}

.btn {
  display: inline-block;
  padding: 0.55rem 1.25rem;
  border: none;
  border-radius: 6px;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
  margin-top: 1rem;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn--primary {
  background: #1a73e8;
  color: #fff;
}

.btn--primary:hover:not(:disabled) {
  background: #1558b0;
}

.btn--discord {
  background: #5865f2;
  color: #fff;
  margin-top: 0;
  white-space: nowrap;
}

.btn--discord:hover:not(:disabled) {
  background: #4752c4;
}

.alert {
  padding: 0.6rem 0.9rem;
  border-radius: 6px;
  font-size: 0.9rem;
  margin-bottom: 0.75rem;
}

.alert--error {
  background: #fdecea;
  color: #b00020;
  border: 1px solid #f5c6cb;
}

.alert--success {
  background: #e6f4ea;
  color: #1e6e38;
  border: 1px solid #b7dfbf;
}

.link-preview {
  font-size: 0.85rem;
  color: #555;
  margin-top: 0.5rem;
  margin-bottom: 1rem;
}

.link-preview a {
  color: #5865f2;
}

.muted {
  color: #777;
  font-size: 0.9rem;
}

.error {
  color: #b00020;
}
</style>