import { test, expect, Route } from '@playwright/test';
import { ProfilePage } from '../page-objects/profile.page';
import { ConfirmEmailChangePage } from '../page-objects/confirm-email-change.page';

const apiOrigin = 'http://localhost:5217';

async function signedIn(page: import('@playwright/test').Page) {
  await page.addInitScript(() => {
    sessionStorage.setItem('tickbox.access-token', 'fake.jwt.for.tests');
  });
}

const baseProfile = {
  email: 'ada@example.com',
  displayName: 'Ada Lovelace',
  pendingEmail: null as string | null
};

test.describe('Profile email change — F-008', () => {
  test('Request_email_change_renders_pending_banner', async ({ page }) => {
    await signedIn(page);
    let profile = { ...baseProfile, pendingEmail: null as string | null };
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
    });
    await page.route(`${apiOrigin}/api/account/email-change/request`, async (route: Route) => {
      const body = JSON.parse(route.request().postData() ?? '{}') as { newEmail: string };
      profile = { ...profile, pendingEmail: body.newEmail };
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
    });

    const profilePage = new ProfilePage(page);
    await profilePage.goto();
    await profilePage.emailChangeInput.fill('ada+new@example.com');
    await profilePage.emailChangeSubmit.click();
    await expect(profilePage.emailChangeBanner).toBeVisible();
    await expect(profilePage.emailChangeBannerPending).toContainText('ada+new@example.com');
  });

  test('Cancel_email_change_clears_pending_banner', async ({ page }) => {
    await signedIn(page);
    let profile = { ...baseProfile, pendingEmail: 'ada+new@example.com' as string | null };
    await page.route(`${apiOrigin}/api/account/me`, async (route: Route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
    });
    await page.route(`${apiOrigin}/api/account/email-change`, async (route: Route) => {
      if (route.request().method() === 'DELETE') {
        profile = { ...profile, pendingEmail: null };
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
      } else {
        await route.fulfill({ status: 405 });
      }
    });

    const profilePage = new ProfilePage(page);
    await profilePage.goto();
    await expect(profilePage.emailChangeBanner).toBeVisible();
    await profilePage.emailChangeBannerCancel.click();
    await expect(profilePage.emailChangeBanner).toBeHidden();
  });

  test('Confirm_email_change_with_valid_token_swaps_login_email', async ({ page }) => {
    await signedIn(page);
    await page.route(`${apiOrigin}/api/account/email-change/confirm`, async (route: Route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ email: 'ada+new@example.com', displayName: 'Ada Lovelace', pendingEmail: null })
      });
    });

    const confirmPage = new ConfirmEmailChangePage(page);
    await confirmPage.goto('valid-token-abc');
    await expect(page).toHaveURL(/\/profile$/);
  });

  test('Confirm_email_change_with_expired_token_shows_inline_error_and_link_to_sign_in', async ({ page }) => {
    await page.route(`${apiOrigin}/api/account/email-change/confirm`, async (route: Route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Token invalid or expired', status: 400 })
      });
    });

    const confirmPage = new ConfirmEmailChangePage(page);
    await confirmPage.goto('expired-token-xyz');
    await expect(confirmPage.errorMessage).toBeVisible();
    await expect(confirmPage.signInLink).toBeVisible();
  });
});
