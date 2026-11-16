import { apiRequest } from './apiClient';

export function getAnnouncements(teamId: string) {
  return apiRequest<any[]>(`/api/discover/${teamId}/announcements`);
}

// TEMPORARY: createdBy is removed when the backend reads the user from the token.
export function createAnnouncement(teamId: string, createdBy: string, title: string, body: string) {
  return apiRequest<any>(`/api/discover/${teamId}/announcements`, {
    method: 'POST',
    body: JSON.stringify({ createdBy, title, body }),
  });
}

// TEMPORARY: updatedBy is removed when the backend reads the user from the token.
export function updateAnnouncement(
  teamId: string, id: string, updatedBy: string, title: string, body: string
) {
  return apiRequest<any>(`/api/discover/${teamId}/announcements/${id}`, {
    method: 'PATCH',
    body: JSON.stringify({ updatedBy, title, body }),
  });
}

export function deleteAnnouncement(teamId: string, id: string) {
  return apiRequest<any>(`/api/discover/${teamId}/announcements/${id}`, {
    method: 'DELETE',
  });
}