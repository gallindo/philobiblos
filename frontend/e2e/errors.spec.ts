import { test, expect } from '@playwright/test';
import { cleanupTestData, uniqueName } from './helpers';

test.describe('AC-5.6 Error handling', () => {
  test.beforeEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test('shows user-readable message when the API is unreachable', async ({ page }) => {
    await page.route('/api/genres**', (route) => route.abort('internetdisconnected'));
    await page.goto('/genres');
    await expect(page.getByTestId('error-banner')).toBeVisible();
    const message = await page.getByTestId('error-banner').textContent();
    expect(message).not.toBeNull();
    expect(message!.length).toBeGreaterThan(0);
  });

  test('shows a user-readable message for 500 responses', async ({ page }) => {
    await page.route('/api/genres**', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'https://example.com/errors/internal',
          title: 'Internal Server Error',
          status: 500,
          detail: 'Something went wrong on the server.',
          correlationId: 'test-correlation-id',
        }),
      })
    );

    await page.goto('/genres');
    await expect(page.getByTestId('error-banner')).toContainText(/Something went wrong|Internal Server Error/i);
  });

  test('shows server validation errors per field', async ({ page }) => {
    await page.goto('/genres');
    await page.getByTestId('new-genre-button').click();

    await page.getByTestId('genre-form-name').fill('x');
    await page.getByTestId('genre-form-name').clear();
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('genre-form-name-required')).toContainText('required');

    await page.getByTestId('genre-form-name').fill(uniqueName('Genre'));
    await page.getByTestId('genre-form-name').clear();
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('genre-form-name')).toHaveClass(/invalid/);
  });
});
