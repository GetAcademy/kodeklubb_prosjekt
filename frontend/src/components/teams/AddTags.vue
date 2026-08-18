<template>
  <div class="tag-page">
    <h2 class="page-title">🏷️ Legg til tags</h2>

    <div class="tag-container">
      <div class="tree-section">
        <p v-if="existingLoading" class="loading-existing">Laster eksisterende tags...</p>
        <TagTree @add-tag="addTag" :disabled-tag-ids="disabledTagIds" :exclude-ids="specialSectionIds" />

        <div v-if="geografiId" class="tree-section-block">
          <h3 class="tree-section-heading">🌍 Geografi</h3>
          <TagTree
            :parent-id="geografiId"
            @add-tag="addTag"
            :disabled-tag-ids="disabledTagIds"
          />
        </div>
      </div>

      <div class="selected-section">
        <template v-if="savedSelections.length">
          <h3 class="selected-title">✅ Allerede lagt til</h3>
          <ul class="selected-list">
            <li v-for="sel in savedSelections" :key="sel.tagId" class="selected-item saved">
              <span class="tag-badge">
                🏷️ {{ tagName(sel.tagId) }}
                <span v-if="sel.levelName" class="level-pill">{{ sel.levelName }}</span>
              </span>
            </li>
          </ul>
        </template>

        <template v-if="pendingSelections.length">
          <h3 class="selected-title pending-title">🆕 Nye valg (ikke lagret enda)</h3>
          <ul class="selected-list">
            <li v-for="sel in pendingSelections" :key="sel.tagId" class="selected-item">
              <span class="tag-badge">
                🏷️ {{ tagName(sel.tagId) }}
                <span v-if="sel.levelTagId" class="level-pill">{{ tagName(sel.levelTagId) }}</span>
              </span>
              <button class="remove-btn" @click="removeSelection(sel.tagId)">✕</button>
            </li>
          </ul>
          <button class="save-btn" @click="saveTags" :disabled="saveStatus === 'saving'">
            {{ saveStatus === 'saving' ? 'Lagrer...' : '💾 Lagre tags' }}
          </button>
        </template>

        <p v-if="!savedSelections.length && !pendingSelections.length" class="no-tags">
          Ingen tags lagt til ennå. Velg fra treet til venstre.
        </p>
      </div>
    </div>

    <!-- Status message always visible -->
    <p v-if="saveMessage" :class="saveStatus === 'error' ? 'error-msg' : 'success-msg'" class="status-msg">
      {{ saveMessage }}
    </p>

    <!-- Experience level popup, shown after picking a technical tag -->
    <div v-if="pendingLevelPrompt" class="modal-overlay" @click.self="chooseLevel(null)">
      <div class="modal-box">
        <h3 class="modal-title">Erfaringsnivå for «{{ tagName(pendingLevelPrompt) }}»</h3>
        <p class="modal-subtitle">Hvor erfaren er du med dette?</p>
        <div class="level-options">
          <button
            v-for="level in levelOptions"
            :key="level.id"
            class="level-btn"
            @click="chooseLevel(level.id)"
          >{{ level.name }}</button>
        </div>
        <button class="skip-btn" @click="chooseLevel(null)">Hopp over</button>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import axios from 'axios';
import TagTree from './TagTree.vue';
import { useAuthStore } from '@/stores/authStore';
import { useTagsStore } from '@/stores/tagsStore';
import { storeToRefs } from 'pinia';

// Optional: when set, tags are saved against this team instead of the
// logged-in user. AddTagsPage.vue supplies this from the route param
// when managing a team's tags.
const props = defineProps<{ teamId?: string }>();

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);
const tagsStore = useTagsStore();

// A "selection" pairs a tag with an optional experience level, matching
// the level_tag_id column on team_tags/user_tags — a tag and its level
// are stored as ONE linked row, not two separate unrelated tag rows.
interface Selection {
  tagId: string;
  levelTagId?: string;
  levelName?: string; // only populated for savedSelections, from the API
}

const savedSelections = ref<Selection[]>([]);
const existingLoading = ref(false);

const pendingSelections = ref<Selection[]>([]);
const saveStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle');
const saveMessage = ref('');

// Every tag id that should render as locked/non-addable in the tree: both
// already-saved tags and ones selected-but-not-yet-saved this session.
const disabledTagIds = computed(() => [
  ...savedSelections.value.map(s => s.tagId),
  ...pendingSelections.value.map(s => s.tagId),
]);

// "Geografi" is pulled out of the main tree into its own separate box
// below, rather than being mixed in with the tech categories.
const geografiId = computed<string | undefined>(() =>
  tagsStore.tags.find(t => t.name === 'Geografi' && t.parentId === null)?.id
);
const specialSectionIds = computed<string[]>(() => geografiId.value ? [geografiId.value] : []);

// "Erfaringsniva" (experience level) — picking any technical tag (i.e.
// anything outside Geografi and outside the level tags themselves)
// triggers a popup asking which level applies. ASCII spelling used here
// deliberately: the database value must match exactly, and piping "å"
// through some shells has corrupted it before (see backend seed notes).
const erfaringsnivaId = computed<string | undefined>(() =>
  tagsStore.tags.find(t => t.name === 'Erfaringsniva')?.id
);
const levelOptions = computed(() =>
  erfaringsnivaId.value ? tagsStore.getChildren(erfaringsnivaId.value) : []
);
const levelTagIds = computed<string[]>(() => levelOptions.value.map(t => t.id));

const pendingLevelPrompt = ref<string | null>(null);

function isDescendantOf(tagId: string, ancestorId: string | undefined): boolean {
  if (!ancestorId) return false;
  let current = tagsStore.getById(tagId);
  while (current?.parentId) {
    if (current.parentId === ancestorId) return true;
    current = tagsStore.getById(current.parentId);
  }
  return false;
}

function tagName(tagId: string): string {
  return tagsStore.getById(tagId)?.name ?? tagId;
}

async function fetchExistingTags() {
  existingLoading.value = true;
  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';

    const url = props.teamId
      ? `${baseApi}/api/discover/${props.teamId}/tags`
      : (user.value?.id ? `${baseApi}/api/users/${user.value.id}/tags` : null);
    if (!url) return;

    const response = await axios.get(url);
    savedSelections.value = (response.data ?? []).map((t: any) => ({
      tagId: t.id ?? t.Id,
      levelTagId: t.levelTagId ?? t.LevelTagId ?? undefined,
      levelName: t.levelName ?? t.LevelName ?? undefined,
    }));
  } catch (err) {
    console.error('Failed to load existing tags', err);
  } finally {
    existingLoading.value = false;
  }
}

function addTag(tagId: string) {
  // Guard against double-adding: skip anything already saved or already
  // pending, even if somehow triggered twice (e.g. a stray double click).
  if (disabledTagIds.value.includes(tagId)) return;

  const isGeography = tagId === geografiId.value || isDescendantOf(tagId, geografiId.value);
  const isLevelRelated = tagId === erfaringsnivaId.value || levelTagIds.value.includes(tagId);

  if (isGeography || isLevelRelated || !erfaringsnivaId.value) {
    // Geography tags, the level tags themselves, and the case where no
    // "Erfaringsniva" category exists at all: just add with no level.
    pendingSelections.value.push({ tagId });
    return;
  }

  // Any other (technical) tag: ask which experience level applies before
  // actually adding it — the answer gets attached to this same selection.
  pendingLevelPrompt.value = tagId;
}

function chooseLevel(levelTagId: string | null) {
  const tagId = pendingLevelPrompt.value;
  if (!tagId) return;

  pendingSelections.value.push({
    tagId,
    levelTagId: levelTagId ?? undefined,
  });
  pendingLevelPrompt.value = null;
}

function removeSelection(tagId: string) {
  // Only removes a not-yet-saved selection; already-saved tags aren't
  // removable from this view.
  pendingSelections.value = pendingSelections.value.filter(s => s.tagId !== tagId);
}

async function saveTags() {
  if (!pendingSelections.value.length) return;
  saveStatus.value = 'saving';
  saveMessage.value = '';

  try {
    const baseApi = import.meta.env.VITE_BASE_API || '';
    const payload = {
      selections: pendingSelections.value.map(s => ({
        tagId: s.tagId,
        levelTagId: s.levelTagId ?? null,
      })),
    };

    if (props.teamId) {
      await axios.post(`${baseApi}/api/discover/${props.teamId}/tags`, {
        ...payload,
        discordId: user.value?.id ?? null,
      });
    } else {
      const discordId = user.value?.id;
      if (!discordId) throw new Error('Not logged in');
      await axios.post(`${baseApi}/api/users/${discordId}/tags`, payload);
    }

    saveStatus.value = 'saved';
    saveMessage.value = 'Tags lagret!';
    pendingSelections.value = [];
    // Refresh from the server so "Allerede lagt til" reflects reality
    // (also catches the rare case where a tag was already saved elsewhere).
    await fetchExistingTags();
  } catch (err) {
    console.error('Failed to save tags', err);
    saveStatus.value = 'error';
    saveMessage.value = 'Kunne ikke lagre tags.';
  }
}

onMounted(fetchExistingTags);
</script>

<style scoped>
.tag-page {
  padding: 24px;
  font-family: sans-serif;
  max-width: 860px;
  margin: 0 auto;
}

.page-title {
  font-size: 22px;
  font-weight: 700;
  margin-bottom: 20px;
  color: #1a1a1a;
}

.tag-container {
  display: flex;
  gap: 32px;
  align-items: flex-start;
}

.tree-section {
  flex: 1;
  background: #ffffff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 16px;
  box-shadow: 0 2px 6px rgba(0,0,0,0.05);
}

.loading-existing {
  color: #888;
  font-size: 13px;
  margin: 0 0 10px;
}

.tree-section-block {
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid #e4e6eb;
}

.tree-section-heading {
  font-size: 14px;
  font-weight: 700;
  color: #1a1a1a;
  margin: 0 0 10px;
}

.selected-section {
  width: 280px;
  background: #f0f7ff;
  border: 1px solid #c5d8fb;
  border-radius: 10px;
  padding: 16px;
  box-shadow: 0 2px 6px rgba(0,0,0,0.05);
}

.selected-title {
  font-size: 16px;
  font-weight: 600;
  margin: 0 0 12px;
  color: #1a73e8;
}

.pending-title {
  margin-top: 16px;
  color: #b8860b;
}

.selected-list {
  list-style: none;
  padding: 0;
  margin: 0 0 16px 0;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.selected-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: white;
  border: 1px solid #d0e4ff;
  border-radius: 6px;
  padding: 6px 10px;
  white-space: nowrap;
  overflow: hidden;
  width: 100%;
}

.selected-item.saved {
  border-color: #bfe3cf;
  background: #f2fbf6;
}

.tag-badge {
  font-size: 13px;
  color: #333;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
  text-align: left;
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.level-pill {
  font-size: 11px;
  font-weight: 700;
  color: #1a73e8;
  background: #e8f0fe;
  border-radius: 999px;
  padding: 1px 8px;
  white-space: nowrap;
}

.remove-btn {
  background: none;
  border: none;
  color: #cc0000;
  cursor: pointer;
  font-size: 14px;
  padding: 2px 6px;
  border-radius: 4px;
  flex-shrink: 0;
  margin-left: 12px;
}

.remove-btn:hover {
  background: #ffe0e0;
}

.save-btn {
  width: 100%;
  padding: 8px;
  background: #0077cc;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  cursor: pointer;
}

.save-btn:hover {
  background: #005fa3;
}

.save-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.no-tags {
  color: #999;
  font-size: 13px;
  padding: 16px;
  background: #fafafa;
  border: 1px dashed #ddd;
  border-radius: 10px;
  text-align: center;
}
.success-msg { color: #0f5132; margin-top: 8px; font-size: 13px; }
.error-msg { color: #842029; margin-top: 8px; font-size: 13px; }
.status-msg { margin-top: 16px; font-size: 14px; text-align: center; }

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
}

.modal-box {
  background: #fff;
  border-radius: 12px;
  padding: 24px;
  width: 320px;
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.2);
}

.modal-title {
  font-size: 16px;
  font-weight: 700;
  margin: 0 0 4px;
  color: #1a1a1a;
}

.modal-subtitle {
  font-size: 13px;
  color: #666;
  margin: 0 0 16px;
}

.level-options {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 12px;
}

.level-btn {
  text-align: left;
  padding: 10px 14px;
  border: 1px solid #d0e4ff;
  background: #f0f7ff;
  border-radius: 8px;
  font-size: 13.5px;
  font-weight: 600;
  color: #1a73e8;
  cursor: pointer;
  transition: background 0.12s ease;
}

.level-btn:hover {
  background: #dceaff;
}

.skip-btn {
  width: 100%;
  padding: 8px;
  border: none;
  background: none;
  color: #999;
  font-size: 13px;
  cursor: pointer;
  text-decoration: underline;
}

.skip-btn:hover {
  color: #666;
}
</style>
