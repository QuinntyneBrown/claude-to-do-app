export interface MyProfile {
  readonly email: string;
  readonly displayName: string;
  readonly pendingEmail: string | null;
}

export interface UpdateDisplayNameRequest {
  readonly displayName: string;
}

export interface ChangePasswordRequest {
  readonly currentPassword: string;
  readonly newPassword: string;
}

export interface RequestEmailChangeRequest {
  readonly newEmail: string;
}

export interface ConfirmEmailChangeRequest {
  readonly token: string;
}
