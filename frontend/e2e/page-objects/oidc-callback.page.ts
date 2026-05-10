import { Page } from '@playwright/test';

export class OidcCallbackPage {
  constructor(private readonly page: Page) {}

  async goto(code: string, state: string): Promise<void> {
    await this.page.goto(`/oidc/callback?code=${encodeURIComponent(code)}&state=${encodeURIComponent(state)}`);
  }
}
