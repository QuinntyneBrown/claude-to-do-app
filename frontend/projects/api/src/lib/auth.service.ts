import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG, ApiConfig } from './api-config';
import { IAuthService, SignInRequest, SignInResponse } from './auth.service.contract';
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
}
