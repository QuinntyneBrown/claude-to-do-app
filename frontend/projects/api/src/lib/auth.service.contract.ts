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

export interface IAuthService {
  register(request: RegisterRequest): Observable<SignInResponse>;
  signIn(request: SignInRequest): Observable<SignInResponse>;
}

export const AUTH_SERVICE = new InjectionToken<IAuthService>('Tickbox.IAuthService');
