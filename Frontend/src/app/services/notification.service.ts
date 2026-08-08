import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { UserNotificationList } from '../models/notification.models';
import { ApiResponse } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly apiUrl = '/api/notifications';

  constructor(private readonly http: HttpClient) {}

  list(take = 20): Observable<UserNotificationList> {
    return this.http
      .get<ApiResponse<UserNotificationList>>(this.apiUrl, { params: { take } })
      .pipe(map((response) => this.requireData(response)));
  }

  markRead(notificationId: string): Observable<boolean> {
    return this.http
      .patch<ApiResponse<boolean>>(`${this.apiUrl}/${notificationId}/read`, {})
      .pipe(map((response) => this.requireData(response)));
  }

  markAllRead(): Observable<boolean> {
    return this.http
      .post<ApiResponse<boolean>>(`${this.apiUrl}/read-all`, {})
      .pipe(map((response) => this.requireData(response)));
  }

  private requireData<T>(response: ApiResponse<T>): T {
    if (!response.success || response.data === null) {
      throw new Error(response.errors?.[0]?.message || response.message || 'The request failed.');
    }
    return response.data;
  }
}
