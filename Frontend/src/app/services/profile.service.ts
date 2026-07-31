import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { MyProfile, UpdateMyProfileCommand } from '../shared/models/update-my-profile.model';

/**
 * Mirrors Safarni.API.Common.ApiResponse<T> on the backend.
 */
interface ApiError {
  code: string;
  message: string;
}

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: ApiError[];
  statusCode: number;
  timestamp: string;
}

/**
 * Mirrors Safarni.Application.Features.Profile.GetMyProfile.GetMyProfileResponse.
 */
interface GetMyProfileResponse {
  userId: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  dateOfBirth?: string | null;
  country?: string | null;
  city?: string | null;
  bio?: string | null;
  profilePhotoUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/**
 * Mirrors Safarni.Application.Features.Profile.UploadProfilePhoto.UploadProfilePhotoResponse.
 */
interface UploadProfilePhotoResponse {
  userId: string;
  profilePhotoUrl: string;
  updatedAt: string;
}

/**
 * Extracts the first backend error message, with a fallback.
 */
export function getProfileErrorMessage(error: unknown, fallback: string): string {
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
  }

  return fallback;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly baseUrl = '/api/profile';

  constructor(private http: HttpClient) {}

  getMyProfile(): Observable<MyProfile> {
    return this.http
      .get<ApiResponse<GetMyProfileResponse>>(`${this.baseUrl}/me`)
      .pipe(map((response) => this.toMyProfile(response.data!)));
  }

  updateMyProfile(command: UpdateMyProfileCommand): Observable<MyProfile> {
    // The backend PUT /api/profile/me accepts exactly these fields
    // (userId and email are not part of UpdateMyProfileRequest).
    const payload = {
      fullName: command.fullName,
      phoneNumber: command.phoneNumber ?? null,
      dateOfBirth: command.dateOfBirth ?? null,
      country: command.country ?? null,
      city: command.city ?? null,
      bio: command.bio ?? null,
    };

    return this.http
      .put<ApiResponse<GetMyProfileResponse>>(`${this.baseUrl}/me`, payload)
      .pipe(map((response) => this.toMyProfile(response.data!)));
  }

  uploadAvatar(file: File): Observable<string> {
    const formData = new FormData();
    // Backend: UploadProfilePhotoRequest(IFormFile File) — field name "file".
    formData.append('file', file);

    return this.http
      .post<ApiResponse<UploadProfilePhotoResponse>>(`${this.baseUrl}/me/photo`, formData)
      .pipe(map((response) => response.data!.profilePhotoUrl));
  }

  private toMyProfile(data: GetMyProfileResponse): MyProfile {
    return {
      userId: data.userId,
      fullName: data.fullName,
      email: data.email,
      phoneNumber: data.phoneNumber ?? null,
      dateOfBirth: data.dateOfBirth ?? null,
      country: data.country ?? null,
      city: data.city ?? null,
      bio: data.bio ?? null,
      avatarUrl: data.profilePhotoUrl ?? null,
    };
  }
}
