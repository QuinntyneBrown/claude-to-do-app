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
  }

  async goto(): Promise<void> {
    await this.page.goto('/profile');
    await expect(this.emailValue).toBeVisible();
  }
}
