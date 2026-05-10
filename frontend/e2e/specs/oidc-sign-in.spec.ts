import { test, expect, Route } from '@playwright/test';
import { SignInPage } from '../page-objects/sign-in.page';
import { OidcCallbackPage } from '../page-objects/oidc-callback.page';
import { TodosListPage } from '../page-objects/todos-list.page';

const apiOrigin = 'http://localhost:5217';

test.describe('OIDC sign-in — F-004', () => {
  test('Begin_oidc_redirects_to_authorization_url', async ({ page }) => {
    await page.addInitScript(() => {
      (window as unknown as { __TICKBOX_OIDC_ENABLED__?: boolean }).__TICKBOX_OIDC_ENABLED__ = true;
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

    // Intercept the IdP URL the app would otherwise navigate to so the test
    // doesn't actually leave the origin; fulfill with a placeholder page.
    await page.route('https://idp.test/**', async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'text/html',
        body: '<html><body>IdP login</body></html>'
      });
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

    // Intercept the IdP URL the app would otherwise navigate to so the test
    // doesn't actually leave the origin; fulfill with a placeholder page.
    await page.route('https://idp.test/**', async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'text/html',
        body: '<html><body>IdP login</body></html>'
      });
    });

    const signIn = new SignInPage(page);
    await signIn.goto();
    await expect(signIn.oidcButton).toBeVisible();
    await signIn.oidcButton.click();
    await expect.poll(() => page.url(), { timeout: 5_000 }).toContain('idp.test');
    expect(page.url()).toContain('https://idp.test/authorize');
    expect(page.url()).toContain('client_id=tickbox');
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
    const todos = new TodosListPage(page);
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
