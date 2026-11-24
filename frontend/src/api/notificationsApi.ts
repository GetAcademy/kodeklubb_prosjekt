import { apiRequest } from './apiClient';

export function getNotifications() {
  return apiRequest<any>('/api/discover/notifications');
}