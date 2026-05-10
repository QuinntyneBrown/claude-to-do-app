import { Page, Locator } from '@playwright/test';

export class ErrorPage {
  readonly errorState: Locator;
  readonly retryButton: Locator;
  readonly backToTodosLink: Locator;

  constructor(private readonly page: Page) {
    this.errorState = page.getByTestId('error-page-state');
    this.retryButton = page.getByTestId('error-page-retry');
    this.backToTodosLink = page.getByTestId('error-page-back-to-todos');
  }

  async goto(): Promise<void> {
    await this.page.goto('/error');
  }
}
