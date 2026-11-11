import { apiRequest } from './apiClient';

// TEMPORARY: the backend still expects ?discordId= here.
// It is removed when the backend reads the user from the token.
export function getMyTeams(discordId: string) {
  return apiRequest<any>(
    `/api/discover/my-teams?discordId=${encodeURIComponent(discordId)}`
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

// TEMPORARY: discordId in the query string.
export function getMyRequests(discordId: string, history = false) {
  const historyParam = history ? '&history=true' : '';
  return apiRequest<any>(
    `/api/discover/my-requests?discordId=${encodeURIComponent(discordId)}${historyParam}`
  );
}

// TEMPORARY: discordId in the query string.
export function cancelJoinRequest(teamId: string, requestId: string, discordId: string) {
  return apiRequest<any>(
    `/api/discover/${teamId}/requests/${requestId}?discordId=${encodeURIComponent(discordId)}`,
    { method: 'DELETE' }
  );
}