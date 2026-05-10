import { test, expect, Route } from '@playwright/test';
import { ProfilePage } from '../page-objects/profile.page';

const apiOrigin = 'http://localhost:5217';

async function signedIn(page: import('@playwright/test').Page) {
  await page.addInitScript(() => {
    sessionStorage.setItem('tickbox.access-token', 'fake.jwt.for.tests');
  });
}

const profileSeed = {
  email: 'ada@example.com',
  displayName: 'Ada Lovelace',
  pendingEmail: null
};

test.describe('Profile delete account — F-009', () => {
  test('Delete_account_confirm_signs_out_and_routes_to_sign_in', async ({ page }) => {
    await signedIn(page);
    let deleteCalls = 0;
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profileSeed) });
    });
    await page.route(`${apiOrigin}/api/account`, async (route: Route) => {
      if (route.request().method() === 'DELETE') {
        deleteCalls += 1;
        await route.fulfill({ status: 204, body: '' });
      } else {
        await route.fallback();
      }
    });

    const profilePage = new ProfilePage(page);
    await profilePage.goto();
    await page.getByTestId('delete-account-button').click();
    await page.getByTestId('confirm-dialog-confirm').click();
    await expect(page).toHaveURL(/\/sign-in$/);
    expect(deleteCalls).toBe(1);
    const accessToken = await page.evaluate(() => sessionStorage.getItem('tickbox.access-token'));
    expect(accessToken).toBeNull();
  });

  test('Delete_account_cancel_closes_dialog_without_deleting', async ({ page }) => {
    await signedIn(page);
    let deleteCalls = 0;
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profileSeed) });
    });
    await page.route(`${apiOrigin}/api/account`, async (route: Route) => {
      if (route.request().method() === 'DELETE') {
        deleteCalls += 1;
        await route.fulfill({ status: 204, body: '' });
      } else {
        await route.fallback();
      }
    });

    const profilePage = new ProfilePage(page);
    await profilePage.goto();
    await page.getByTestId('delete-account-button').click();
    await page.getByTestId('confirm-dialog-cancel').click();
    await expect(page.getByTestId('confirm-dialog')).toBeHidden();
    await expect(page).toHaveURL(/\/profile$/);
    expect(deleteCalls).toBe(0);
  });
});
