import { test, expect, Route } from '@playwright/test';
import { SignInPage } from '../page-objects/sign-in.page';
import { OidcCallbackPage } from '../page-objects/oidc-callback.page';
import { TodosPage } from '../page-objects/todos.page';

const apiOrigin = 'http://localhost:5217';

test.describe('OIDC sign-in — F-004', () => {
  test('Begin_oidc_redirects_to_authorization_url', async ({ page }) => {
    await page.addInitScript(() => {
      (window as unknown as { __TICKBOX_OIDC_ENABLED__?: boolean }).__TICKBOX_OIDC_ENABLED__ = true;
      (window as unknown as { __TICKBOX_OIDC_REDIRECTS__?: string[] }).__TICKBOX_OIDC_REDIRECTS__ = [];
      const w = window as unknown as { __TICKBOX_OIDC_REDIRECTS__: string[] };
      const original = window.location.assign.bind(window.location);
      Object.defineProperty(window.location, 'assign', {
        configurable: true,
        value: (url: string) => {
          w.__TICKBOX_OIDC_REDIRECTS__.push(url);
          // Don't actually navigate.
        }
      });
      void original;
    });

    await page.route(`${apiOrigin}/api/auth/oidc/authorize`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          authorizationUrl: 'https://idp.test/authorize?client_id=tickbox&state=abc',
          state: 'abc'
        })
      });
    });

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.oidcButton.click();

    const redirects = await page.evaluate(() =>
      (window as unknown as { __TICKBOX_OIDC_REDIRECTS__?: string[] }).__TICKBOX_OIDC_REDIRECTS__ ?? []
    );
    expect(redirects).toHaveLength(1);
    expect(redirects[0]).toContain('https://idp.test/authorize');
  });

  test('Callback_with_valid_code_signs_in_and_routes_to_todos', async ({ page }) => {
    await page.route(`${apiOrigin}/api/auth/oidc/callback`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ userId: 'u', accessToken: 'fake.jwt.for.tests' })
      });
    });
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    const callback = new OidcCallbackPage(page);
    const todos = new TodosPage(page);
    await callback.goto('idp-code', 'abc');
    await todos.expectVisible();
  });

  test('Callback_with_invalid_state_routes_back_to_sign_in_with_reason', async ({ page }) => {
    await page.route(`${apiOrigin}/api/auth/oidc/callback`, async (route: Route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Validation failed', status: 400 })
      });
    });

    const callback = new OidcCallbackPage(page);
    await callback.goto('idp-code', 'wrong');
    await expect(page).toHaveURL(/\/sign-in\?reason=oidc_failed$/);
  });
});
