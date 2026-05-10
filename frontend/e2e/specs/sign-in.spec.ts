import { test, expect, Route } from '@playwright/test';
import { SignInPage } from '../page-objects/sign-in.page';
import { TodosPage } from '../page-objects/todos.page';

const apiOrigin = 'http://localhost:5217';

test.describe('Sign in — F-002', () => {
  test('Sign_in_with_valid_creds_routes_to_todos', async ({ page }) => {
    await page.route(`${apiOrigin}/api/auth/sign-in`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ userId: 'u', accessToken: 'fake.jwt.for.tests' })
      });
    });
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      });
    });

    const signIn = new SignInPage(page);
    const todos = new TodosPage(page);
    await signIn.goto();
    await signIn.signIn('ada@example.com', 'correct-horse-battery-staple');
    await todos.expectVisible();
  });

  test('Sign_in_with_wrong_password_shows_inline_error', async ({ page }) => {
    await page.route(`${apiOrigin}/api/auth/sign-in`, async (route: Route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Unauthorized', status: 401, detail: 'Incorrect email or password.' })
      });
    });

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn('ada@example.com', 'wrong-pw');
    await expect(signIn.errorMessage).toBeVisible();
    await expect(page).toHaveURL(/\/sign-in$/);
  });

  test('Sign_in_hides_oidc_button_when_disabled', async ({ page }) => {
    await page.addInitScript(() => {
      (window as unknown as { __TICKBOX_OIDC_ENABLED__?: boolean }).__TICKBOX_OIDC_ENABLED__ = false;
    });

    const signIn = new SignInPage(page);
    await signIn.goto();
    await expect(signIn.oidcButton).toHaveCount(0);
  });

  test('Sign_in_shows_oidc_button_when_enabled', async ({ page }) => {
    await page.addInitScript(() => {
      (window as unknown as { __TICKBOX_OIDC_ENABLED__?: boolean }).__TICKBOX_OIDC_ENABLED__ = true;
    });

    const signIn = new SignInPage(page);
    await signIn.goto();
    await expect(signIn.oidcButton).toBeVisible();
  });

  test('Refresh_interceptor_silently_renews_session_on_401', async ({ page }) => {
    let listCalls = 0;
    let refreshCalls = 0;

    await page.route(`${apiOrigin}/api/auth/sign-in`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ userId: 'u', accessToken: 'expired.access.token' })
      });
    });

    await page.route(`${apiOrigin}/api/auth/refresh`, async (route: Route) => {
      refreshCalls += 1;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ userId: 'u', accessToken: 'fresh.access.token' })
      });
    });

    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      const auth = route.request().headers()['authorization'];
      listCalls += 1;
      if (auth === 'Bearer expired.access.token') {
        await route.fulfill({ status: 401, body: '' });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      });
    });

    const signIn = new SignInPage(page);
    const todos = new TodosPage(page);
    await signIn.goto();
    await signIn.signIn('ada@example.com', 'correct-horse-battery-staple');
    await todos.expectVisible();

    expect(refreshCalls).toBe(1);
    expect(listCalls).toBeGreaterThanOrEqual(2);
  });
});
