import { Page, Locator, expect } from '@playwright/test';

export class PasswordResetRequestPage {
  readonly emailInput: Locator;
  readonly submitButton: Locator;
  readonly confirmation: Locator;

  constructor(private readonly page: Page) {
    this.emailInput = page.getByTestId('email-input');
    this.submitButton = page.getByTestId('reset-request-submit');
    this.confirmation = page.getByTestId('reset-request-confirmation');
  }

  async goto(): Promise<void> {
    await this.page.goto('/password-reset/request');
    await expect(this.submitButton).toBeVisible();
  }

  async submit(email: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.submitButton.click();
  }
}
