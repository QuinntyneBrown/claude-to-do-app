import { test, expect, Route } from '@playwright/test';
import { SignInPage } from '../page-objects/sign-in.page';
import { TodosPage } from '../page-objects/todos.page';

const apiOrigin = 'http://localhost:5217';

test.describe('Sample flow — sign in then create and list to-dos', () => {
  test('user signs in, the list loads from the API, and they can add a to-do', async ({ page }) => {
    let signInCalls = 0;
    let listCalls = 0;
    let createCalls = 0;
    const created: { id: string; title: string; status: 'Incomplete' | 'Complete'; createdAt: string; completedAt: string | null }[] = [];

    await page.route(`${apiOrigin}/api/auth/sign-in`, async (route: Route) => {
      signInCalls += 1;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ userId: 'user-1', accessToken: 'fake.jwt.for.tests' })
      });
    });

    await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
      const request = route.request();
      const auth = request.headers()['authorization'];

      if (request.method() === 'GET') {
        listCalls += 1;
        if (auth !== 'Bearer fake.jwt.for.tests') {
          await route.fulfill({ status: 401, body: '' });
          return;
        }
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(created)
        });
        return;
      }

      if (request.method() === 'POST') {
        createCalls += 1;
        const body = JSON.parse(request.postData() ?? '{}') as { title: string };
        const todo = {
          id: `todo-${createCalls}`,
          title: body.title,
          status: 'Incomplete' as const,
          createdAt: new Date().toISOString(),
          completedAt: null
        };
        created.unshift(todo);
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(todo)
        });
        return;
      }

      await route.fallback();
    });

    const signIn = new SignInPage(page);
    const todos = new TodosPage(page);

    await signIn.goto();
    await signIn.signIn('ada@example.com', 'correct-horse-battery-staple');

    await todos.expectVisible();
    await todos.expectItemCount(0);
    await expect(todos.emptyState).toBeVisible();

    await todos.addTodo('Draft launch announcement');
    await todos.expectItemCount(1);
    await todos.expectItemTitle(0, 'Draft launch announcement');

    expect(signInCalls).toBe(1);
    expect(listCalls).toBeGreaterThanOrEqual(1);
    expect(createCalls).toBe(1);
  });
});
