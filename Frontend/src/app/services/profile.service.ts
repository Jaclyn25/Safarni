import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import { MyProfile, UpdateMyProfileCommand } from '../shared/models/update-my-profile.model';

export { getApiErrorMessage as getProfileErrorMessage } from '../models/api-response.model';

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

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly baseUrl = '/api/profile';
  private readonly currentProfileState = signal<MyProfile | null>(null);

  readonly currentProfile = this.currentProfileState.asReadonly();

  constructor(private http: HttpClient) {}

  getMyProfile(): Observable<MyProfile> {
    return this.http
      .get<ApiResponse<GetMyProfileResponse>>(`${this.baseUrl}/me`)
      .pipe(
        map((response) => this.toMyProfile(response.data!)),
        tap((profile) => this.currentProfileState.set(profile))
      );
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
      .pipe(
        map((response) => this.toMyProfile(response.data!)),
        tap((profile) => this.currentProfileState.set(profile))
      );
  }

  uploadAvatar(file: File): Observable<string> {
    const formData = new FormData();
    // Backend: UploadProfilePhotoRequest(IFormFile File) — field name "file".
    formData.append('file', file);

    return this.http
      .post<ApiResponse<UploadProfilePhotoResponse>>(`${this.baseUrl}/me/photo`, formData)
      .pipe(
        map((response) => response.data!.profilePhotoUrl),
        tap((avatarUrl) => {
          const profile = this.currentProfileState();
          if (profile) {
            this.currentProfileState.set({ ...profile, avatarUrl });
          }
        })
      );
  }

  clearCurrentProfile(): void {
    this.currentProfileState.set(null);
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
