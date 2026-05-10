import { Page, Locator, expect } from '@playwright/test';

export class ProfilePage {
  readonly emailValue: Locator;
  readonly displayNameValue: Locator;
  readonly displayNameInput: Locator;
  readonly displayNameSave: Locator;
  readonly displayNameError: Locator;
  readonly currentPasswordInput: Locator;
  readonly newPasswordInput: Locator;
  readonly changePasswordSubmit: Locator;
  readonly changePasswordError: Locator;
  readonly signOutButton: Locator;
  readonly emailChangeInput: Locator;
  readonly emailChangeSubmit: Locator;
  readonly emailChangeError: Locator;
  readonly emailChangeBanner: Locator;
  readonly emailChangeBannerPending: Locator;
  readonly emailChangeBannerCancel: Locator;

  constructor(private readonly page: Page) {
    this.emailValue = page.getByTestId('profile-email');
    this.displayNameValue = page.getByTestId('profile-display-name');
    this.displayNameInput = page.getByTestId('display-name-input');
    this.displayNameSave = page.getByTestId('display-name-save');
    this.displayNameError = page.getByTestId('display-name-error');
    this.currentPasswordInput = page.getByTestId('current-password-input');
    this.newPasswordInput = page.getByTestId('new-password-input');
    this.changePasswordSubmit = page.getByTestId('change-password-submit');
    this.changePasswordError = page.getByTestId('change-password-error');
    this.signOutButton = page.getByTestId('sign-out-button');
    this.emailChangeInput = page.getByTestId('email-change-input');
    this.emailChangeSubmit = page.getByTestId('email-change-submit');
    this.emailChangeError = page.getByTestId('email-change-error');
    this.emailChangeBanner = page.getByTestId('email-change-banner');
    this.emailChangeBannerPending = page.getByTestId('email-change-banner-pending');
    this.emailChangeBannerCancel = page.getByTestId('email-change-banner-cancel');
  }

  async goto(): Promise<void> {
    await this.page.goto('/profile');
    await expect(this.emailValue).toBeVisible();
  }
}
