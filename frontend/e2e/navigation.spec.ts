import { test, expect } from '@playwright/test';

test.describe('AC-5.1 Navigation', () => {
  test('default route redirects to /genres', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/genres$/);
    await expect(page.getByTestId('genres-section')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Genres' })).toBeVisible();
  });

  test('nav links switch sections without full page reload', async ({ page }) => {
    await page.goto('/genres');
    await expect(page.getByTestId('genres-section')).toBeVisible();

    const mainNav = page.getByTestId('main-nav');

    await page.evaluate(() => {
      (window as unknown as Record<string, string>).e2eNavMarker = ' preserved ';
    });

    await mainNav.getByTestId('nav-authors').click();
    await expect(page).toHaveURL(/\/authors$/);
    await expect(page.getByTestId('authors-section')).toBeVisible();
    expect(await page.evaluate(() => (window as unknown as Record<string, string>).e2eNavMarker)).toContain('preserved');

    await mainNav.getByTestId('nav-books').click();
    await expect(page).toHaveURL(/\/books$/);
    await expect(page.getByTestId('books-section')).toBeVisible();
    expect(await page.evaluate(() => (window as unknown as Record<string, string>).e2eNavMarker)).toContain('preserved');

    await mainNav.getByTestId('nav-genres').click();
    await expect(page).toHaveURL(/\/genres$/);
    await expect(page.getByTestId('genres-section')).toBeVisible();
    expect(await page.evaluate(() => (window as unknown as Record<string, string>).e2eNavMarker)).toContain('preserved');
  });
});
