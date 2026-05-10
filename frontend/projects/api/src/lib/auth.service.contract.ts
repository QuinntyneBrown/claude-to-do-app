import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { RegisterRequest } from './register-request';

export interface SignInRequest {
  readonly email: string;
  readonly password: string;
}

export interface SignInResponse {
  readonly userId: string;
  readonly accessToken: string;
}

export interface PasswordResetRequest {
  readonly email: string;
}

export interface CompletePasswordResetRequest {
  readonly token: string;
  readonly newPassword: string;
}

export interface IAuthService {
  register(request: RegisterRequest): Observable<SignInResponse>;
  signIn(request: SignInRequest): Observable<SignInResponse>;
  refreshAccessToken(): Observable<SignInResponse>;
  requestPasswordReset(request: PasswordResetRequest): Observable<void>;
  completePasswordReset(request: CompletePasswordResetRequest): Observable<SignInResponse>;
}

export const AUTH_SERVICE = new InjectionToken<IAuthService>('Tickbox.IAuthService');
