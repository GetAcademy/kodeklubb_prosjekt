
import { apiRequest } from './apiClient';

export function getCurrentUser() {
  return apiRequest<any>('/api/users/me');
}