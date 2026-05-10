import { Page, Locator, expect } from '@playwright/test';

export class SignUpPage {
  readonly displayNameInput: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;

  constructor(private readonly page: Page) {
    this.displayNameInput = page.getByTestId('display-name-input');
    this.emailInput = page.getByTestId('email-input');
    this.passwordInput = page.getByTestId('password-input');
    this.submitButton = page.getByTestId('sign-up-submit');
    this.errorMessage = page.getByTestId('sign-up-error');
  }

  async goto(): Promise<void> {
    await this.page.goto('/sign-up');
    await expect(this.submitButton).toBeVisible();
  }

  async signUp(displayName: string, email: string, password: string): Promise<void> {
    await this.displayNameInput.fill(displayName);
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }
}
