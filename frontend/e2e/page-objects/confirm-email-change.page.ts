import { Page, Locator } from '@playwright/test';

export class ConfirmEmailChangePage {
  readonly status: Locator;
  readonly errorMessage: Locator;
  readonly signInLink: Locator;

  constructor(private readonly page: Page) {
    this.status = page.getByTestId('confirm-email-change-status');
    this.errorMessage = page.getByTestId('confirm-email-change-error');
    this.signInLink = page.getByTestId('confirm-email-change-sign-in-link');
  }

  async goto(token: string): Promise<void> {
    await this.page.goto(`/email-change/confirm?token=${encodeURIComponent(token)}`);
  }
}
