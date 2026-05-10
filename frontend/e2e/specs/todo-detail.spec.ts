import { test, expect, Route } from '@playwright/test';
import { TodoDetailPage } from '../page-objects/todo-detail.page';
import { TodosListPage } from '../page-objects/todos-list.page';

const apiOrigin = 'http://localhost:5217';

async function signedIn(page: import('@playwright/test').Page) {
  await page.addInitScript(() => {
    sessionStorage.setItem('tickbox.access-token', 'fake.jwt.for.tests');
  });
}

const detailSeed = (overrides: Partial<{
  id: string;
  title: string;
  notes: string | null;
  dueDate: string | null;
  status: 'Incomplete' | 'Complete';
  createdAt: string;
  completedAt: string | null;
  activity: { kind: string; occurredAt: string }[];
}>) => ({
  id: 'todo-1',
  title: 'sample',
  notes: null,
  dueDate: null,
  status: 'Incomplete' as const,
  createdAt: new Date().toISOString(),
  completedAt: null,
  activity: [{ kind: 'Created', occurredAt: new Date().toISOString() }],
  ...overrides
});

test.describe('Todo detail — F-006', () => {
  test('FAB_to_detail_in_create_mode_then_save_returns_to_list', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'new-id',
            title: 'New thing',
            notes: null,
            dueDate: null,
            status: 'Incomplete',
            createdAt: new Date().toISOString(),
            completedAt: null
          })
        });
        return;
      }
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    const detail = new TodoDetailPage(page);
    const todos = new TodosListPage(page);
    await detail.gotoNew();
    await detail.titleInput.fill('New thing');
    await detail.saveButton.click();
    await todos.expectVisible();
  });

  test('Open_detail_then_save_persists_new_fields', async ({ page }) => {
    await signedIn(page);
    const initial = detailSeed({});

    await page.route(`${apiOrigin}/api/todos/todo-1`, async (route: Route) => {
      const method = route.request().method();
      if (method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(initial)
        });
        return;
      }
      if (method === 'PUT') {
        const body = JSON.parse(route.request().postData() ?? '{}');
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ ...initial, ...body, status: initial.status, completedAt: initial.completedAt })
        });
        return;
      }
      await route.fallback();
    });
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    const detail = new TodoDetailPage(page);
    const todos = new TodosListPage(page);
    await detail.gotoEdit('todo-1');
    await detail.titleInput.fill('updated');
    await detail.saveButton.click();
    await todos.expectVisible();
  });

  test('Past_due_date_inline_error', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 400,
          contentType: 'application/problem+json',
          body: JSON.stringify({
            title: 'Validation failed',
            status: 400,
            errors: { dueDate: ['Due date must be today or later.'] }
          })
        });
        return;
      }
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    const detail = new TodoDetailPage(page);
    await detail.gotoNew();
    await detail.titleInput.fill('past due');
    // Native date input — fill yesterday's ISO date
    const yesterday = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString().slice(0, 10);
    await page.locator('[data-testid="todo-due-date-input"]').fill(yesterday);
    await detail.saveButton.click();
    await expect(detail.errorMessage).toBeVisible();
  });

  test('Detail_chip_set_toggles_status_and_writes_activity_strip_entry', async ({ page }) => {
    await signedIn(page);
    let serverState: 'Incomplete' | 'Complete' = 'Incomplete';
    let serverActivity = [{ kind: 'Created', occurredAt: new Date().toISOString() }];

    await page.route(`${apiOrigin}/api/todos/todo-1`, async (route: Route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(detailSeed({ status: serverState, activity: serverActivity, completedAt: serverState === 'Complete' ? new Date().toISOString() : null }))
        });
        return;
      }
      await route.fallback();
    });

    await page.route(`${apiOrigin}/api/todos/todo-1/status`, async (route: Route) => {
      const body = JSON.parse(route.request().postData() ?? '{}') as { status: 'Incomplete' | 'Complete' };
      serverState = body.status;
      if (body.status === 'Complete') {
        serverActivity = [...serverActivity, { kind: 'MarkedComplete', occurredAt: new Date().toISOString() }];
      }
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(detailSeed({ status: serverState, activity: serverActivity, completedAt: serverState === 'Complete' ? new Date().toISOString() : null }))
      });
    });

    const detail = new TodoDetailPage(page);
    await detail.gotoEdit('todo-1');
    await expect(detail.activityItems).toHaveCount(1);
    await detail.statusComplete.click();
    await expect(detail.activityItems).toHaveCount(2);
  });

  test('Delete_button_opens_confirm_dialog_then_removes_from_list', async ({ page }) => {
    await signedIn(page);
    const initial = detailSeed({});
    let deleted = false;

    await page.route(`${apiOrigin}/api/todos/todo-1`, async (route: Route) => {
      const method = route.request().method();
      if (method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(initial)
        });
        return;
      }
      if (method === 'DELETE') {
        deleted = true;
        await route.fulfill({ status: 204, body: '' });
        return;
      }
      await route.fallback();
    });
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    const detail = new TodoDetailPage(page);
    const todos = new TodosListPage(page);
    await detail.gotoEdit('todo-1');
    await detail.deleteButton.click();
    await expect(detail.confirmDialog).toBeVisible();
    await detail.confirmDeleteButton.click();
    await todos.expectVisible();
    expect(deleted).toBe(true);
  });
});
