import { apiRequest } from './apiClient';

export interface TagSelection {
  tagId: string;
  levelTagId: string | null;
}

export function getCurrentUser() {
  return apiRequest<any>('/api/users/me');
}

// TEMPORARY: the user's tags are addressed by discordId in the URL.
// This becomes /api/users/me/tags when the backend reads the user from the token.
export function getUserTags(discordId: string) {
  return apiRequest<any>(`/api/users/${encodeURIComponent(discordId)}/tags`);
}

// TEMPORARY: discordId in the URL.
export function addUserTag(discordId: string, selections: TagSelection[]) {
  return apiRequest<any>(`/api/users/${encodeURIComponent(discordId)}/tags`, {
    method: 'POST',
    body: JSON.stringify({ selections }),
  });
}

// TEMPORARY: discordId in the URL.
export function removeUserTag(discordId: string, tagId: string) {
  return apiRequest<any>(
    `/api/users/${encodeURIComponent(discordId)}/tags/${encodeURIComponent(tagId)}`,
    { method: 'DELETE' }
  );
}