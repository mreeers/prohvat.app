import { test, expect } from '@playwright/test';

test.describe('Map View', () => {
  test('Should show map container', async ({ page }) => {
    // Navigate to root which should be the map view
    await page.goto('/');

    // Wait for the map container to be visible
    const mapContainer = page.locator('.map-container');
    await expect(mapContainer).toBeVisible({ timeout: 10000 });
  });
});
