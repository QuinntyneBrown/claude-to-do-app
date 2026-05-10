import { test, expect, Route } from '@playwright/test';
import { TodosListPage } from '../page-objects/todos-list.page';
import { TodoDetailPage } from '../page-objects/todo-detail.page';
import { ErrorPage } from '../page-objects/error.page';

const apiOrigin = 'http://localhost:5217';

async function signedIn(page: import('@playwright/test').Page) {
  await page.addInitScript(() => {
    sessionStorage.setItem('tickbox.access-token', 'fake.jwt.for.tests');
  });
}

test.describe('Error state — F-010', () => {
  test('Network_failure_during_todos_load_renders_error_state_with_retry', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({ status: 500, contentType: 'application/problem+json', body: '{"title":"Server error","status":500}' });
    });

    const todos = new TodosListPage(page);
    await page.goto('/todos');
    await expect(page.getByTestId('todos-load-error')).toBeVisible();
    await expect(page.getByTestId('todos-load-error-retry')).toBeVisible();
  });

  test('Retry_button_re_attempts_the_failed_request', async ({ page }) => {
    await signedIn(page);
    let calls = 0;
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      calls += 1;
      if (calls === 1) {
        await route.fulfill({ status: 500, contentType: 'application/problem+json', body: '{"title":"Server error","status":500}' });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });

    await page.goto('/todos');
    await expect(page.getByTestId('todos-load-error')).toBeVisible();
    await page.getByTestId('todos-load-error-retry').click();
    await expect(page.getByTestId('todos-load-error')).toBeHidden();
    await expect(page.getByTestId('todos-empty-state')).toBeVisible();
    expect(calls).toBeGreaterThanOrEqual(2);
  });

  test('Form_input_is_preserved_when_retrying_after_5xx', async ({ page }) => {
    await signedIn(page);
    let attempts = 0;
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      if (route.request().method() !== 'POST') {
        await route.fallback();
        return;
      }
      attempts += 1;
      if (attempts === 1) {
        await route.fulfill({ status: 500, contentType: 'application/problem+json', body: '{"title":"Server error","status":500}' });
      } else {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: 'new-id', title: 'My new todo', notes: 'remember the milk', dueDate: null, status: 'Incomplete', createdAt: new Date().toISOString(), completedAt: null })
        });
      }
    });

    const detail = new TodoDetailPage(page);
    await detail.gotoNew();
    await detail.titleInput.fill('My new todo');
    await detail.notesInput.fill('remember the milk');
    await detail.saveButton.click();
    await expect(detail.errorMessage).toBeVisible();
    await expect(detail.titleInput).toHaveValue('My new todo');
    await expect(detail.notesInput).toHaveValue('remember the milk');
    await detail.saveButton.click();
    await expect(page).toHaveURL(/\/todos$/);
    expect(attempts).toBe(2);
  });

  test('Unknown_route_redirects_to_error_page_with_back_to_todos_link', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/this-route-does-not-exist');
    const errorPage = new ErrorPage(page);
    await expect(page).toHaveURL(/\/error/);
    await expect(errorPage.errorState).toBeVisible();
    await expect(errorPage.backToTodosLink).toBeVisible();
  });
});
