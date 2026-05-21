import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface IwxUser {
  id: string;
  username: string;
  email: string;
  tenantId: string;
  enabled: boolean;
  createdAtUtc: string;
  lastLoginAtUtc?: string | null;
  roles: string[];
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  tenantId?: string;
  roles?: string[];
}

export interface LoginRequest { username: string; password: string; }
export interface LoginResponse {
  token: string;
  expiresUtc: string;
  roles: string[];
  tenant: string;
  subject: string;
}

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/api/security/auth`;

  login(req: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/login`, req);
  }
  listUsers(): Observable<IwxUser[]> {
    return this.http.get<IwxUser[]>(`${this.base}/users`);
  }
  createUser(req: CreateUserRequest): Observable<unknown> {
    return this.http.post(`${this.base}/users`, req);
  }
  me(): Observable<unknown> {
    return this.http.get(`${this.base}/me`);
  }
}
