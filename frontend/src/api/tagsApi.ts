import { apiRequest } from './apiClient';

export interface RawTag {
  id: string;
  name: string;
  parentId: string | null;
  openForChildSuggestions: boolean;
}

export function getTags() {
  return apiRequest<RawTag[]>('/api/tags');
}