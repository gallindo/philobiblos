import { test, expect } from '@playwright/test';
import {
  cleanupTestData,
  createGenreViaApi,
  createBookViaApi,
  createAuthorViaApi,
  uniqueName,
} from './helpers';

test.describe('AC-5.2 / AC-5.3 / AC-5.4 Genres', () => {
  test.beforeEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test('shows loading indicator while fetching and empty state when no records', async ({ page }) => {
    await page.route('/api/genres**', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 300));
      await route.continue();
    });

    await page.goto('/genres');
    await expect(page.getByTestId('loading-indicator')).toBeVisible();
    await expect(page.getByTestId('loading-indicator')).toBeHidden();
    await expect(page.getByTestId('empty-state')).toContainText('No genres found');
    await expect(page.getByTestId('genres-table')).not.toBeVisible();
  });

  test('displays records in a table and supports pagination', async ({ page, request }) => {
    for (let i = 1; i <= 25; i++) {
      await createGenreViaApi(request, `AA Genre ${String(i).padStart(2, '0')} E2E`);
    }

    await page.goto('/genres');
    await expect(page.getByTestId('genres-table')).toBeVisible();
    await expect(page.getByTestId('genre-row')).toHaveCount(20);
    await expect(page.getByTestId('pagination')).toBeVisible();
    await expect(page.getByTestId('pagination-info')).toContainText('1–20 of 25');

    await page.getByTestId('pagination-next').click();
    await expect(page.getByTestId('genre-row')).toHaveCount(5);
    await expect(page.getByTestId('pagination-info')).toContainText('21–25 of 25');
    await expect(page.getByTestId('pagination-page-number')).toContainText('Page 2 of 2');

    await page.getByTestId('pagination-previous').click();
    await expect(page.getByTestId('genre-row')).toHaveCount(20);
    await expect(page.getByTestId('pagination-info')).toContainText('1–20 of 25');
  });

  test('search refreshes the list and resets to page 1', async ({ page, request }) => {
    await createGenreViaApi(request, 'Alpha E2E');
    await createGenreViaApi(request, 'Beta E2E');
    for (let i = 1; i <= 22; i++) {
      await createGenreViaApi(request, `Other ${i} E2E`);
    }

    await page.goto('/genres');
    await page.getByTestId('pagination-next').click();
    await expect(page.getByTestId('pagination-page-number')).toContainText('Page 2 of 2');

    await page.getByTestId('genre-search').fill('Alpha');
    await expect(page.getByTestId('genre-row')).toHaveCount(1);
    await expect(page.getByTestId('genre-name')).toContainText('Alpha E2E');
    await expect(page.getByTestId('pagination')).not.toBeVisible();
  });

  test('creates a genre and required fields are visually indicated', async ({ page }) => {
    await page.goto('/genres');
    await page.getByTestId('new-genre-button').click();
    await expect(page.getByTestId('genre-form')).toBeVisible();

    await page.getByTestId('genre-form-name').fill('x');
    await page.getByTestId('genre-form-name').clear();
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('genre-form-name')).toHaveClass(/invalid/);
    await expect(page.getByTestId('genre-form-name-required')).toContainText('required');

    const name = uniqueName('Genre');
    await page.getByTestId('genre-form-name').fill(name);
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('success-message')).toContainText('Genre created');
    await expect(page.getByTestId('genre-row').filter({ hasText: name })).toBeVisible();
  });

  test('shows server field errors and non-field conflict errors', async ({ page, request }) => {
    const existingName = uniqueName('Genre');
    await createGenreViaApi(request, existingName);

    await page.goto('/genres');
    await page.getByTestId('new-genre-button').click();

    await page.getByTestId('genre-form-name').fill('a'.repeat(101));
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('genre-form-name-error')).toBeVisible();

    await page.getByTestId('genre-form-name').fill(existingName);
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('error-banner')).toContainText(/already exists|conflict/i);
  });

  test('edits a genre and the list refreshes', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const updatedName = uniqueName('Updated Genre');

    await page.goto('/genres');
    const row = page.getByTestId('genre-row').filter({ hasText: genre.name });
    await row.getByTestId('genre-edit').click();

    await page.getByTestId('genre-form-name').fill(updatedName);
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('success-message')).toContainText('Genre updated');
    await expect(page.getByTestId('genre-row').filter({ hasText: updatedName })).toBeVisible();
    await expect(page.getByTestId('genre-row').filter({ hasText: genre.name })).not.toBeVisible();
  });

  test('deletes a genre after confirmation', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));

    await page.goto('/genres');
    const row = page.getByTestId('genre-row').filter({ hasText: genre.name });

    await row.getByTestId('genre-delete').click();
    await row.getByTestId('genre-delete-confirm').click();

    await expect(page.getByTestId('success-message')).toContainText('Genre deleted');
    await expect(page.getByTestId('genre-row').filter({ hasText: genre.name })).not.toBeVisible();
  });

  test('shows in-use message when deleting a genre with associated books', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const author = await createAuthorViaApi(request, uniqueName('Author'));
    await createBookViaApi(request, {
      title: uniqueName('Book'),
      authorId: author.id,
      genreId: genre.id,
    });

    await page.goto('/genres');
    const row = page.getByTestId('genre-row').filter({ hasText: genre.name });

    await row.getByTestId('genre-delete').click();
    await row.getByTestId('genre-delete-confirm').click();

    await expect(page.getByTestId('error-banner')).toContainText(/in use|cannot be deleted/i);
    await expect(page.getByTestId('genre-row').filter({ hasText: genre.name })).toBeVisible();
  });
});
