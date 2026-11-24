import { apiRequest } from './apiClient';

export function getMyTeams() {
  return apiRequest<any>(
    `/api/discover/my-teams`
  );
}

// TEMPORARY: discordId is removed when the backend reads the user from the token.
export function getAvailableTeams(discordId?: string) {
  const query = discordId ? `?discordId=${encodeURIComponent(discordId)}` : '';
  return apiRequest<any>(`/api/discover/available${query}`);
}

// TEMPORARY: the body still sends discordId.
export function requestToJoinTeam(teamId: string, discordId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/request`, {
    method: 'POST',
    body: JSON.stringify({ discordId }),
  });
}

export function getMyRequests(history = false) {
  const query = history ? '?history=true' : '';
  return apiRequest<any>(`/api/discover/my-requests${query}`);
}

// TEMPORARY: discordId in the query string.
export function cancelJoinRequest(teamId: string, requestId: string, discordId: string) {
  return apiRequest<any>(
    `/api/discover/${teamId}/requests/${requestId}?discordId=${encodeURIComponent(discordId)}`,
    { method: 'DELETE' }
  );
}

// TEMPORARY: discordId in the query string.
export function getTeam(teamId: string, discordId?: string) {
  const query = discordId ? `?discordId=${encodeURIComponent(discordId)}` : '';
  return apiRequest<any>(`/api/discover/${teamId}${query}`);
}

export function getTeamRequests(teamId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/requests`);
}

// TEMPORARY: the body sends the actor's discordId. The backend must read the
// admin from the token and check that the user is admin of the team.
export function approveJoinRequest(teamId: string, requestId: string, discordId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/requests/${requestId}/approve`, {
    method: 'PATCH',
    body: JSON.stringify({ discordId }),
  });
}

// TEMPORARY: same as approveJoinRequest.
export function declineJoinRequest(teamId: string, requestId: string, discordId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/requests/${requestId}/decline`, {
    method: 'PATCH',
    body: JSON.stringify({ discordId }),
  });
}

export function getTeamMembers(teamId: string) {
  return apiRequest<any[]>(`/api/discover/${teamId}/members`);
}

export function getTeamTags(teamId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/tags`);
}

// TEMPORARY: the body still sends the actor's discordId.
export function addTeamTag(
  teamId: string,
  selections: { tagId: string; levelTagId: string | null }[],
  discordId: string | null
) {
  return apiRequest<any>(`/api/discover/${teamId}/tags`, {
    method: 'POST',
    body: JSON.stringify({ selections, discordId }),
  });
}

export function removeTeamTag(teamId: string, tagId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/tags/${encodeURIComponent(tagId)}`, {
    method: 'DELETE',
  });
}