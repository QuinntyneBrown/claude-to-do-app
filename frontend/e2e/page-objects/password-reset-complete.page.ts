import { Page, Locator, expect } from '@playwright/test';

export class PasswordResetCompletePage {
  readonly newPasswordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;

  constructor(private readonly page: Page) {
    this.newPasswordInput = page.getByTestId('new-password-input');
    this.submitButton = page.getByTestId('reset-complete-submit');
    this.errorMessage = page.getByTestId('reset-complete-error');
  }

  async goto(token: string): Promise<void> {
    await this.page.goto(`/password-reset/complete?token=${encodeURIComponent(token)}`);
    await expect(this.submitButton).toBeVisible();
  }

  async submit(newPassword: string): Promise<void> {
    await this.newPasswordInput.fill(newPassword);
    await this.submitButton.click();
  }
}
