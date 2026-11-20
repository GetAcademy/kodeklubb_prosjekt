import { apiRequest } from './apiClient';

// TEMPORARY: discordId in the query string.
export function getNotifications(discordId: string) {
  return apiRequest<any>(
    `/api/discover/notifications?discordId=${encodeURIComponent(discordId)}`
  );
}