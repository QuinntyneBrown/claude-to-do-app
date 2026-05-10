import { test, expect, Route } from '@playwright/test';
import { SignUpPage } from '../page-objects/sign-up.page';
import { TodosListPage } from '../page-objects/todos-list.page';

const apiOrigin = 'http://localhost:5217';

test.describe('Sign up — F-001', () => {
  test('Sign_up_with_valid_input_creates_account_and_routes_to_todos', async ({ page }) => {
    let registerCalls = 0;

    await page.route(`${apiOrigin}/api/auth/register`, async (route: Route) => {
      registerCalls += 1;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ userId: 'user-1', accessToken: 'fake.jwt.for.tests' })
      });
    });

    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      });
    });

    const signUp = new SignUpPage(page);
    const todos = new TodosListPage(page);

    await signUp.goto();
    await signUp.signUp('Ada Lovelace', 'ada@example.com', 'correct-horse-battery-staple');

    await todos.expectVisible();
    expect(registerCalls).toBe(1);
  });

  test('Sign_up_with_password_under_12_chars_shows_inline_error', async ({ page }) => {
    await page.route(`${apiOrigin}/api/auth/register`, async (route: Route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          title: 'Validation failed',
          status: 400,
          errors: { Password: ['Password must be at least 12 characters.'] }
        })
      });
    });

    const signUp = new SignUpPage(page);
    await signUp.goto();
    await signUp.signUp('Ada Lovelace', 'ada@example.com', 'short');

    await expect(signUp.errorMessage).toBeVisible();
    await expect(page).toHaveURL(/\/sign-up$/);
  });
});
