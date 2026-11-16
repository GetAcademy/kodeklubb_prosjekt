import { apiRequest } from './apiClient';

// NOTE for the backend step: these endpoints change a team's Discord setup.
// They must require that the current user is admin of the team.

export function getTeamDiscordInfo(teamId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/discord/info`);
}

export function setTeamDiscordConfig(teamId: string, payload: {
  discordServerId: string;
  discordChannelId: string;
  discordRoleId: string;
  discordLink?: string | null;
}) {
  return apiRequest<any>(`/api/discover/${teamId}/discord`, {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function updateTeamDiscordConfig(teamId: string, payload: {
  discordServerId?: string | null;
  discordChannelId?: string | null;
  discordRoleId?: string | null;
  discordLink?: string | null;
}) {
  return apiRequest<any>(`/api/discover/${teamId}/discord`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  });
}

export function removeTeamDiscordConfig(teamId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/discord`, { method: 'DELETE' });
}

export function syncTeamWithDiscord(teamId: string) {
  return apiRequest<any>(`/api/discover/${teamId}/discord/sync`, { method: 'POST' });
}