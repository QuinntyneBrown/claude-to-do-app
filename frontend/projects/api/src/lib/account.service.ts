import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG, ApiConfig } from './api-config';
import { IAccountService } from './account.service.contract';
import { ChangePasswordRequest, ConfirmEmailChangeRequest, MyProfile, RequestEmailChangeRequest, UpdateDisplayNameRequest } from './my-profile';

@Injectable()
export class AccountService implements IAccountService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_CONFIG) private readonly config: ApiConfig
  ) {}

  getMyProfile(): Observable<MyProfile> {
    return this.http.get<MyProfile>(`${this.config.baseUrl}/api/account/me`);
  }

  updateDisplayName(request: UpdateDisplayNameRequest): Observable<MyProfile> {
    return this.http.put<MyProfile>(`${this.config.baseUrl}/api/account/display-name`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.config.baseUrl}/api/account/password`, request, { withCredentials: true });
  }

  requestEmailChange(request: RequestEmailChangeRequest): Observable<MyProfile> {
    return this.http.post<MyProfile>(`${this.config.baseUrl}/api/account/email-change/request`, request);
  }

  confirmEmailChange(request: ConfirmEmailChangeRequest): Observable<MyProfile> {
    return this.http.post<MyProfile>(`${this.config.baseUrl}/api/account/email-change/confirm`, request);
  }

  cancelEmailChange(): Observable<MyProfile> {
    return this.http.delete<MyProfile>(`${this.config.baseUrl}/api/account/email-change`);
  }

  deleteMyAccount(): Observable<void> {
    return this.http.delete<void>(`${this.config.baseUrl}/api/account`);
  }
}
