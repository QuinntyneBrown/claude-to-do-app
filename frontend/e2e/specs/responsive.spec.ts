import { test, expect, Route } from '@playwright/test';

const apiOrigin = 'http://localhost:5217';

const profileSeed = {
  email: 'ada@example.com',
  displayName: 'Ada Lovelace',
  pendingEmail: null
};

async function signedIn(page: import('@playwright/test').Page) {
  await page.addInitScript(() => {
    sessionStorage.setItem('tickbox.access-token', 'fake.jwt.for.tests');
  });
}

async function mockApi(page: import('@playwright/test').Page) {
  await page.route(`${apiOrigin}/api/todos`, async (route: Route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    } else {
      await route.fallback();
    }
  });
  await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profileSeed) });
  });
}

const breakpoints = [
  { width: 360, height: 800, label: 'mobile' },
  { width: 768, height: 1024, label: 'tablet' },
  { width: 1440, height: 900, label: 'desktop' }
];

const appShellRoutes = ['/todos', '/profile', '/error'];

test.describe('Responsive sweep — F-011', () => {
  for (const bp of breakpoints) {
    for (const route of appShellRoutes) {
      test(`AppShell_at_${bp.label}_${route.replace(/\//g, '_')}_renders_correct_nav_and_no_horizontal_scroll`, async ({ page }) => {
        await page.setViewportSize({ width: bp.width, height: bp.height });
        await signedIn(page);
        await mockApi(page);
        await page.goto(route);

        const navRail = page.getByTestId('nav-rail');
        const navBar = page.getByTestId('nav-bar');
        if (bp.width < 600) {
          await expect(navBar).toBeVisible();
          await expect(navRail).toBeHidden();
        } else {
          await expect(navRail).toBeVisible();
          await expect(navBar).toBeHidden();
        }

        const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
        expect(overflow).toBeLessThanOrEqual(0);
      });
    }
  }

  test('FAB_on_todos_meets_48dp_touch_target_on_mobile', async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 800 });
    await signedIn(page);
    await mockApi(page);
    await page.goto('/todos');

    const fab = page.getByTestId('add-todo-fab');
    const box = await fab.boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeGreaterThanOrEqual(48);
    expect(box!.height).toBeGreaterThanOrEqual(48);
  });

  test('SignOut_button_on_profile_meets_48dp_touch_target_on_mobile', async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 800 });
    await signedIn(page);
    await mockApi(page);
    await page.goto('/profile');

    const button = page.getByTestId('sign-out-button');
    const box = await button.boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeGreaterThanOrEqual(48);
    expect(box!.height).toBeGreaterThanOrEqual(48);
  });

  test('Sign_in_page_has_no_horizontal_scroll_at_mobile_width', async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 800 });
    await page.goto('/sign-in');
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow).toBeLessThanOrEqual(0);
  });
});
