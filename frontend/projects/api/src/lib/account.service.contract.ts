import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { ChangePasswordRequest, ConfirmEmailChangeRequest, MyProfile, RequestEmailChangeRequest, UpdateDisplayNameRequest } from './my-profile';

export interface IAccountService {
  getMyProfile(): Observable<MyProfile>;
  updateDisplayName(request: UpdateDisplayNameRequest): Observable<MyProfile>;
  changePassword(request: ChangePasswordRequest): Observable<void>;
  requestEmailChange(request: RequestEmailChangeRequest): Observable<MyProfile>;
  confirmEmailChange(request: ConfirmEmailChangeRequest): Observable<MyProfile>;
  cancelEmailChange(): Observable<MyProfile>;
  deleteMyAccount(): Observable<void>;
}

export const ACCOUNT_SERVICE = new InjectionToken<IAccountService>('Tickbox.IAccountService');
