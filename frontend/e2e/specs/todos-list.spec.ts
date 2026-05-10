import { test, expect, Route } from '@playwright/test';
import { TodosListPage } from '../page-objects/todos-list.page';

const apiOrigin = 'http://localhost:5217';

const todoSeed = (overrides: Partial<{
  id: string;
  title: string;
  notes: string | null;
  dueDate: string | null;
  status: 'Incomplete' | 'Complete';
  createdAt: string;
  completedAt: string | null;
}>) => ({
  id: 'id-' + Math.random().toString(36).slice(2, 8),
  title: 'sample',
  notes: null,
  dueDate: null,
  status: 'Incomplete' as const,
  createdAt: new Date().toISOString(),
  completedAt: null,
  ...overrides
});

async function signedIn(page: import('@playwright/test').Page) {
  await page.addInitScript(() => {
    sessionStorage.setItem('tickbox.access-token', 'fake.jwt.for.tests');
  });
}

test.describe('Todos list — F-005', () => {
  test('Empty_state_renders_with_add_cta_when_no_todos', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    const todos = new TodosListPage(page);
    await page.goto('/todos');
    await todos.expectVisible();
    await expect(todos.emptyState).toBeVisible();
  });

  test('List_groups_todos_into_incomplete_and_complete_sections', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          todoSeed({ title: 'do A', status: 'Incomplete' }),
          todoSeed({ title: 'finished B', status: 'Complete' }),
          todoSeed({ title: 'do C', status: 'Incomplete' })
        ])
      });
    });

    const todos = new TodosListPage(page);
    await page.goto('/todos');
    await todos.expectVisible();

    await expect(todos.incompleteItems).toHaveCount(2);
    await expect(todos.completeItems).toHaveCount(1);
    await expect(todos.countSummary).toContainText('1 of 3');
  });

  test('Filter_chip_filters_to_one_state_only', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          todoSeed({ title: 'do A', status: 'Incomplete' }),
          todoSeed({ title: 'finished B', status: 'Complete' })
        ])
      });
    });

    const todos = new TodosListPage(page);
    await page.goto('/todos');
    await todos.expectVisible();
    await todos.filterIncomplete.click();
    await expect(todos.incompleteItems).toHaveCount(1);
    await expect(todos.completeItems).toHaveCount(0);

    await todos.filterComplete.click();
    await expect(todos.incompleteItems).toHaveCount(0);
    await expect(todos.completeItems).toHaveCount(1);

    await todos.filterAll.click();
    await expect(todos.incompleteItems).toHaveCount(1);
    await expect(todos.completeItems).toHaveCount(1);
  });

  test('Header_shows_today_date_label', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    const todos = new TodosListPage(page);
    await page.goto('/todos');
    await expect(todos.headerDateLabel).toBeVisible();
    const text = await todos.headerDateLabel.innerText();
    // Format: "Today, <day> <Month>" — match a digit-then-month pattern.
    expect(text).toMatch(/Today,\s+\d{1,2}\s+\w+/);
  });

  test('Tapping_checkbox_in_list_moves_todo_between_sections', async ({ page }) => {
    await signedIn(page);

    const initial = todoSeed({ id: 'todo-X', title: 'flip me', status: 'Incomplete' });
    const flipped = { ...initial, status: 'Complete', completedAt: new Date().toISOString() };

    let listCalls = 0;
    let toggleCalls = 0;
    let serverState: 'Incomplete' | 'Complete' = 'Incomplete';

    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      if (route.request().method() === 'GET') {
        listCalls += 1;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ ...initial, status: serverState, completedAt: serverState === 'Complete' ? new Date().toISOString() : null }])
        });
        return;
      }
      await route.fallback();
    });

    await page.route(`${apiOrigin}/api/todos/${initial.id}/status`, async (route: Route) => {
      toggleCalls += 1;
      const body = JSON.parse(route.request().postData() ?? '{}') as { status: 'Incomplete' | 'Complete' };
      serverState = body.status;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: initial.id,
          title: initial.title,
          notes: null,
          dueDate: null,
          status: body.status,
          createdAt: initial.createdAt,
          completedAt: body.status === 'Complete' ? new Date().toISOString() : null,
          activity: []
        })
      });
    });

    const todos = new TodosListPage(page);
    await page.goto('/todos');
    await todos.expectVisible();
    await expect(todos.incompleteItems).toHaveCount(1);
    await expect(todos.completeItems).toHaveCount(0);

    await todos.toggleByLabel('flip me');
    await expect(todos.completeItems).toHaveCount(1);
    await expect(todos.incompleteItems).toHaveCount(0);

    expect(toggleCalls).toBe(1);
    expect(listCalls).toBeGreaterThanOrEqual(1);
    void flipped;
  });
});
