import { auth0 } from '@/auth0';

const baseApi = import.meta.env.VITE_BASE_API || '';

export class ApiError extends Error {
  constructor(public status: number, message: string, public body: string = '') {
    super(message);
  }
}

// The only place that gets the Auth0 token and builds the HTTP request.
export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = await auth0.getAccessTokenSilently();

  const response = await fetch(`${baseApi}${path}`, {
    ...options,
    headers: {
      ...(options.body ? { 'Content-Type': 'application/json' } : {}),
      ...options.headers,
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    let body = '';
    try { body = await response.text(); } catch { /* ignore */ }
    throw new ApiError(response.status, `API request failed: ${response.status}`, body);
  }

     if (response.status === 204) return undefined as T;
  const text = await response.text();
  if (!text) return undefined as T;
  try { return JSON.parse(text) as T; } catch { return text as unknown as T; }
}