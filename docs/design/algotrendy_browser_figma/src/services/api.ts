import { API_BASE_URL } from '../config/api';
import type { ApiResponse } from '../types';

// Generic fetch wrapper with error handling
async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const url = `${API_BASE_URL}${endpoint}`;
  
  const defaultHeaders: HeadersInit = {
    'Content-Type': 'application/json',
  };

  // Add authentication token if available
  const token = localStorage.getItem('authToken');
  if (token) {
    defaultHeaders['Authorization'] = `Bearer ${token}`;
  }

  const config: RequestInit = {
    ...options,
    headers: {
      ...defaultHeaders,
      ...options.headers,
    },
    // Add timeout to prevent hanging
    signal: AbortSignal.timeout(5000), // 5 second timeout
  };

  try {
    const response = await fetch(url, config);
    
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    
    return {
      success: true,
      data,
    };
  } catch (error) {
    // Silently fail - components will handle with mock data
    // Only log if it's not a typical "backend not running" error
    if (error instanceof Error && 
        !error.message.includes('Failed to fetch') && 
        !error.message.includes('signal timed out') &&
        error.name !== 'AbortError' && 
        error.name !== 'TimeoutError') {
      console.warn('API request issue:', error.message);
    }
    
    return {
      success: false,
      data: {} as T,
      error: error instanceof Error ? error.message : 'An unknown error occurred',
    };
  }
}

// HTTP method helpers
export const api = {
  get: function<T>(endpoint: string) {
    return apiRequest<T>(endpoint, { method: 'GET' });
  },
  
  post: function<T>(endpoint: string, data?: any) {
    return apiRequest<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  },
  
  put: function<T>(endpoint: string, data?: any) {
    return apiRequest<T>(endpoint, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  },
  
  delete: function<T>(endpoint: string) {
    return apiRequest<T>(endpoint, { method: 'DELETE' });
  },
  
  patch: function<T>(endpoint: string, data?: any) {
    return apiRequest<T>(endpoint, {
      method: 'PATCH',
      body: data ? JSON.stringify(data) : undefined,
    });
  },
};

export default api;
