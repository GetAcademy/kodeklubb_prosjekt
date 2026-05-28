import axios from "axios";

const api = axios.create({
  baseURL: "/api/discover",
});

export async function getTeamDiscordInfo(teamId: string) {
  const res = await api.get(`/${teamId}/discord/info`);
  return res.data;
}

export async function setTeamDiscordConfig(teamId: string, payload: {
  discordServerId: string;
  discordChannelId: string;
  discordRoleId: string;
  discordLink?: string | null;
}) {
  const res = await api.post(`/${teamId}/discord`, payload);
  return res.data;
}

export async function updateTeamDiscordConfig(teamId: string, payload: {
  discordServerId?: string | null;
  discordChannelId?: string | null;
  discordRoleId?: string | null;
  discordLink?: string | null;
}) {
  const res = await api.patch(`/${teamId}/discord`, payload);
  return res.data;
}

export async function removeTeamDiscordConfig(teamId: string) {
  const res = await api.delete(`/${teamId}/discord`);
  return res.data;
}

export async function syncTeamWithDiscord(teamId: string) {
  const res = await api.post(`/${teamId}/discord/sync`);
  return res.data;
}

/**
 * Sets only the Discord invite link for a team.
 * This is the simple field shown on the team edit page.
 * Does NOT require a Discord bot token on the server.
 */
export async function setTeamDiscordInviteLink(teamId: string, discordInviteLink: string | null) {
  const res = await api.patch(`/${teamId}/discord/invite-link`, { discordInviteLink });
  return res.data;
}
