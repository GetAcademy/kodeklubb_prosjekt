import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_BASE_API || "",
});

export interface LinkDiscordRequest {
  discordId: string;
}

export interface DiscordAccountStatus {
  userId: string;
  isLinked: boolean;
  discordId: string | null;
  username: string;
}

export async function linkDiscordAccount(
  discordId: string
): Promise<{ message: string }> {
  const res = await api.post(`/api/users/${discordId}/discord/link`);
  return res.data;
}

export async function unlinkDiscordAccount(
  discordId: string
): Promise<{ message: string }> {
  try {
    const res = await api.delete(`/api/users/${discordId}/discord/unlink`);
    console.log('Unlink response:', res.status, res.data);
    if (!res.data || res.status !== 200) {
      throw new Error(`Unlink failed: ${res.status}`);
    }
    return res.data;
  } catch (error: any) {
    console.error('Unlink error response:', error.response?.status, error.response?.data);
    throw error.response?.data?.message || error.message || 'Failed to unlink Discord account';
  }
}

export async function getDiscordAccountStatus(
  discordId: string
): Promise<DiscordAccountStatus> {
  const res = await api.get(`/api/users/${discordId}/discord/status`);
  return res.data;
}
