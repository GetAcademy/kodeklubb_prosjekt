import { ApiError, apiRequest } from './apiClient';

export interface LinkDiscordRequest {
  discordId: string;
}

export interface DiscordAccountStatus {
  userId: string;
  isLinked: boolean;
  discordId: string | null;
  username: string;
}

// Keep the server's message if it sent one
function messageFrom(err: unknown, fallback: string): string {
  if (err instanceof ApiError && err.body) {
    try {
      const parsed = JSON.parse(err.body);
      if (parsed?.message) return parsed.message;
    } catch { /* not JSON */ }
    return err.body;
  }
  return err instanceof Error ? err.message : fallback;
}

// TEMPORARY: discordId in the URL. May be removed completely, see Terje's note
// about the old Discord linking.
export async function linkDiscordAccount(discordId: string): Promise<{ message: string }> {
  try {
    return await apiRequest<{ message: string }>(
      `/api/users/${encodeURIComponent(discordId)}/discord/link`,
      { method: 'POST' }
    );
  } catch (err) {
    throw new Error(messageFrom(err, 'Failed to link Discord account'));
  }
}

// TEMPORARY: discordId in the URL.
export async function unlinkDiscordAccount(discordId: string): Promise<{ message: string }> {
  try {
    const data = await apiRequest<{ message: string }>(
      `/api/users/${encodeURIComponent(discordId)}/discord/unlink`,
      { method: 'DELETE' }
    );
    if (!data) throw new Error('Unlink failed');
    return data;
  } catch (err) {
    console.error('Unlink error:', err);
    // The old code threw the message as a plain string, so we keep that
    throw messageFrom(err, 'Failed to unlink Discord account');
  }
}

// TEMPORARY: discordId in the URL.
export function getDiscordAccountStatus(discordId: string): Promise<DiscordAccountStatus> {
  return apiRequest<DiscordAccountStatus>(
    `/api/users/${encodeURIComponent(discordId)}/discord/status`
  );
}