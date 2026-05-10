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

test.describe('Profile core — F-007', () => {
  test('Profile_renders_email_and_display_name', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(profileSeed)
      });
    });

    const profile = new ProfilePage(page);
    await profile.goto();
    await expect(profile.emailValue).toContainText('ada@example.com');
    await expect(profile.displayNameValue).toContainText('Ada Lovelace');
  });

  test('Update_display_name_persists_and_renders_new_value', async ({ page }) => {
    await signedIn(page);
    let currentName = profileSeed.displayName;
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...profileSeed, displayName: currentName })
      });
    });
    await page.route(`${apiOrigin}/api/account/display-name`, async (route: Route) => {
      const body = JSON.parse(route.request().postData() ?? '{}') as { displayName: string };
      currentName = body.displayName;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...profileSeed, displayName: currentName })
      });
    });

    const profile = new ProfilePage(page);
    await profile.goto();
    await profile.displayNameInput.fill('New Name');
    await profile.displayNameSave.click();
    await expect(profile.displayNameValue).toContainText('New Name');
  });

  test('Update_display_name_rejects_blank_via_inline_error', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profileSeed) });
    });
    await page.route(`${apiOrigin}/api/account/display-name`, async (route: Route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Validation failed', status: 400 })
      });
    });

    const profile = new ProfilePage(page);
    await profile.goto();
    await profile.displayNameInput.fill('');
    await profile.displayNameSave.click();
    await expect(profile.displayNameError).toBeVisible();
  });

  test('Change_password_with_correct_current_persists', async ({ page }) => {
    await signedIn(page);
    let changes = 0;
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profileSeed) });
    });
    await page.route(`${apiOrigin}/api/account/password`, async (route: Route) => {
      changes += 1;
      await route.fulfill({ status: 204, body: '' });
    });

    const profile = new ProfilePage(page);
    await profile.goto();
    await profile.currentPasswordInput.fill('correct-horse-battery-staple');
    await profile.newPasswordInput.fill('brand-new-passphrase-456');
    await profile.changePasswordSubmit.click();
    await expect.poll(() => changes, { timeout: 5_000 }).toBe(1);
  });

  test('Change_password_with_wrong_current_shows_inline_error', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profileSeed) });
    });
    await page.route(`${apiOrigin}/api/account/password`, async (route: Route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Validation failed', status: 400 })
      });
    });

    const profile = new ProfilePage(page);
    await profile.goto();
    await profile.currentPasswordInput.fill('totally-wrong-password');
    await profile.newPasswordInput.fill('brand-new-passphrase-456');
    await profile.changePasswordSubmit.click();
    await expect(profile.changePasswordError).toBeVisible();
  });

  test('Sign_out_clears_auth_state_and_routes_to_sign_in', async ({ page }) => {
    await signedIn(page);
    let signOutCalls = 0;
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profileSeed) });
    });
    await page.route(`${apiOrigin}/api/auth/sign-out`, async (route: Route) => {
      signOutCalls += 1;
      await route.fulfill({ status: 204, body: '' });
    });

    const profile = new ProfilePage(page);
    await profile.goto();
    await profile.signOutButton.click();
    await expect(page).toHaveURL(/\/sign-in$/);
    expect(signOutCalls).toBe(1);

    const accessToken = await page.evaluate(() => sessionStorage.getItem('tickbox.access-token'));
    expect(accessToken).toBeNull();
  });
});
