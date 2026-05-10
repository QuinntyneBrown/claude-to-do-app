import { test, expect, Route } from '@playwright/test';
import { PasswordResetRequestPage } from '../page-objects/password-reset-request.page';
import { PasswordResetCompletePage } from '../page-objects/password-reset-complete.page';
import { TodosPage } from '../page-objects/todos.page';

const apiOrigin = 'http://localhost:5217';

test.describe('Password reset — F-003', () => {
  test('Request_password_reset_shows_inline_confirmation_for_any_email', async ({ page }) => {
    let calls = 0;
    await page.route(`${apiOrigin}/api/auth/password-reset/request`, async (route: Route) => {
      calls += 1;
      await route.fulfill({ status: 202, body: '' });
    });

    const reqPage = new PasswordResetRequestPage(page);
    await reqPage.goto();
    await reqPage.submit('any@example.com');

    await expect(reqPage.confirmation).toBeVisible();
    expect(calls).toBe(1);
  });

  test('Complete_password_reset_with_valid_token_signs_in', async ({ page }) => {
    await page.route(`${apiOrigin}/api/auth/password-reset/complete`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ userId: 'u', accessToken: 'fake.jwt.for.tests' })
      });
    });
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    const completePage = new PasswordResetCompletePage(page);
    const todos = new TodosPage(page);
    await completePage.goto('valid-token-abc');
    await completePage.submit('brand-new-passphrase-123');

    await todos.expectVisible();
  });

  test('Complete_password_reset_with_expired_token_shows_inline_error', async ({ page }) => {
    await page.route(`${apiOrigin}/api/auth/password-reset/complete`, async (route: Route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          title: 'Validation failed',
          status: 400,
          errors: { token: ['The reset link is invalid or has expired.'] }
        })
      });
    });

    const completePage = new PasswordResetCompletePage(page);
    await completePage.goto('expired-token-xyz');
    await completePage.submit('another-strong-passphrase');

    await expect(completePage.errorMessage).toBeVisible();
  });
});
