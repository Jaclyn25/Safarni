import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { MyProfile, UpdateMyProfileCommand } from '../shared/models/update-my-profile.model';

// Demo data so the page renders with something real before your backend
// is wired up. Swap the bodies of the three methods below for the
// commented-out HTTP calls once /api/profile exists.
const MOCK_PROFILE: MyProfile = {
  userId: 'usr_julian_walters',
  fullName: 'Julian Walters',
  email: 'julian.walters@voyage.ai',
  phoneNumber: '+1 (555) 000-1234',
  dateOfBirth: '1990-04-12',
  country: 'United States',
  city: 'New York',
  bio: 'Digital nomad and luxury travel enthusiast. Always looking for the next hidden gem in the Mediterranean.',
  avatarUrl: 'https://placehold.co/96x96',
  membershipTier: 'Premium Member',
};

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly baseUrl = '/api/profile';
  private mockState: MyProfile = { ...MOCK_PROFILE };

  constructor(private http: HttpClient) {}

  getMyProfile(): Observable<MyProfile> {
    return of(this.mockState).pipe(delay(300));
    // return this.http.get<MyProfile>(`${this.baseUrl}/me`);
  }

  updateMyProfile(command: UpdateMyProfileCommand): Observable<MyProfile> {
    this.mockState = { ...this.mockState, ...command };
    return of(this.mockState).pipe(delay(400));
    // return this.http.put<MyProfile>(`${this.baseUrl}/me`, command);
  }

  uploadAvatar(file: File): Observable<{ avatarUrl: string }> {
    const localUrl = URL.createObjectURL(file);
    this.mockState = { ...this.mockState, avatarUrl: localUrl };
    return of({ avatarUrl: localUrl }).pipe(delay(200));
    // const formData = new FormData();
    // formData.append('file', file);
    // return this.http.post<{ avatarUrl: string }>(`${this.baseUrl}/me/avatar`, formData);
  }
}
