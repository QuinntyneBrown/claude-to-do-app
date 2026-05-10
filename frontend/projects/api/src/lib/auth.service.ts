import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG, ApiConfig } from './api-config';
import { CompletePasswordResetRequest, IAuthService, PasswordResetRequest, SignInRequest, SignInResponse } from './auth.service.contract';
import { RegisterRequest } from './register-request';

@Injectable()
export class AuthService implements IAuthService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_CONFIG) private readonly config: ApiConfig
  ) {}

  register(request: RegisterRequest): Observable<SignInResponse> {
    return this.http.post<SignInResponse>(`${this.config.baseUrl}/api/auth/register`, request);
  }

  signIn(request: SignInRequest): Observable<SignInResponse> {
    return this.http.post<SignInResponse>(`${this.config.baseUrl}/api/auth/sign-in`, request);
  }

  refreshAccessToken(): Observable<SignInResponse> {
    return this.http.post<SignInResponse>(`${this.config.baseUrl}/api/auth/refresh`, {}, { withCredentials: true });
  }

  requestPasswordReset(request: PasswordResetRequest): Observable<void> {
    return this.http.post<void>(`${this.config.baseUrl}/api/auth/password-reset/request`, request);
  }

  completePasswordReset(request: CompletePasswordResetRequest): Observable<SignInResponse> {
    return this.http.post<SignInResponse>(`${this.config.baseUrl}/api/auth/password-reset/complete`, request);
  }
}
