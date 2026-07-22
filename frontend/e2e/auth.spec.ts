import { test, expect } from '@playwright/test';

test.describe('Auth Flow', () => {
  test('Should show error on invalid login', async ({ page }) => {
    await page.goto('/');

    // Wait for the layout to load (nav bar usually visible)
    await page.waitForLoadState('networkidle');

    // Make sure we are on login page (assuming root redirects or shows login)
    // Actually, root is probably map or home. Let's explicitly go to /auth/login
    await page.goto('/auth/login');

    // Fill in the form
    await page.fill('input[type="email"]', 'invaliduser@prohvat.app');
    await page.fill('input[type="password"]', 'wrongpassword');

    // Click login button
    await page.click('button[type="submit"]');

    // Expect to see an error message
    const errorMsg = page.locator('.error');
    await expect(errorMsg).toBeVisible({ timeout: 5000 });
  });
});
