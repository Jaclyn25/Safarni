import { HttpErrorResponse } from '@angular/common/http';

export interface ApiError {
  code: string;
  message: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: ApiError[];
  statusCode: number;
  timestamp: string;
}

/**
 * Extracts the first backend error message from the standard envelope,
 * with a local fallback. Shared by every service; avoid reimplementing it.
 */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const apiResponse = error.error as Partial<ApiResponse<unknown>> | null;
    const firstError = apiResponse?.errors?.[0]?.message;

    if (firstError) {
      return firstError;
    }

    if (apiResponse?.message) {
      return apiResponse.message;
    }

    if (error.status === 0) {
      return 'The API is unavailable. Make sure the backend is running on http://localhost:5110.';
    }

    if (error.status === 401) {
      return 'Your session has expired. Please log out and log in again.';
    }
  }

  return fallback;
}