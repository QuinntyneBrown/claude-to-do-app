import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { ChangePasswordRequest, MyProfile, UpdateDisplayNameRequest } from './my-profile';

export interface IAccountService {
  getMyProfile(): Observable<MyProfile>;
  updateDisplayName(request: UpdateDisplayNameRequest): Observable<MyProfile>;
  changePassword(request: ChangePasswordRequest): Observable<void>;
}

export const ACCOUNT_SERVICE = new InjectionToken<IAccountService>('Tickbox.IAccountService');
