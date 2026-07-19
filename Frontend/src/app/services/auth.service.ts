import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

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

export interface RegisterUserResponse {
  id: string;
  fullName: string;
  email: string;
  createdAt: string;
  isActive: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  userId: string;
  fullName: string;
  email: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5110/api/Auth';

  constructor(private http: HttpClient) {}

  register(command: any): Observable<ApiResponse<RegisterUserResponse>> {
    return this.http.post<ApiResponse<RegisterUserResponse>>(`${this.apiUrl}/register`, command);
  }

  login(command: any): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/login`, command).pipe(
      tap((res) => {
        if (res.success && res.data) {
          this.saveSession(res.data);
        }
      })
    );
  }

  logout(): void {
    const accessToken = localStorage.getItem('access_token');
    const refreshToken = localStorage.getItem('refresh_token');

    if (accessToken && refreshToken) {
      const headers = new HttpHeaders({
        'Authorization': `Bearer ${accessToken}`
      });
      // Try to inform backend about logout, but always clear local session
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }, { headers }).subscribe({
        next: () => this.clearSession(),
        error: () => this.clearSession()
      });
    } else {
      this.clearSession();
    }
  }

  private saveSession(data: LoginResponse): void {
    localStorage.setItem('access_token', data.accessToken);
    localStorage.setItem('refresh_token', data.refreshToken);
    localStorage.setItem('user', JSON.stringify({
      userId: data.userId,
      fullName: data.fullName,
      email: data.email
    }));
  }

  private clearSession(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
  }

  isAuthenticated(): boolean {
    return !!localStorage.getItem('access_token');
  }

  getCurrentUser(): any {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  }
}
