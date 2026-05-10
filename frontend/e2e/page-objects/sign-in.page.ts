import { Page, Locator, expect } from '@playwright/test';

export class SignInPage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;
  readonly oidcButton: Locator;
  readonly forgotPasswordLink: Locator;

  constructor(private readonly page: Page) {
    this.emailInput = page.getByTestId('email-input');
    this.passwordInput = page.getByTestId('password-input');
    this.submitButton = page.getByTestId('sign-in-submit');
    this.errorMessage = page.getByTestId('sign-in-error');
    this.oidcButton = page.getByTestId('oidc-sign-in-button');
    this.forgotPasswordLink = page.getByTestId('forgot-password-link');
  }

  async goto(): Promise<void> {
    await this.page.goto('/sign-in');
    await expect(this.submitButton).toBeVisible();
  }

  async signIn(email: string, password: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }
}
