import { test, expect } from '@playwright/test';
import {
  authenticatePage,
  cleanupTestData,
  createGenreViaApi,
  createAuthorViaApi,
  createBookViaApi,
  uniqueName,
} from './helpers';

test.describe('AC-5.2 / AC-5.3 / AC-5.4 Authors', () => {
  test.beforeEach(async ({ request, page }) => {
    await cleanupTestData(request);
    await authenticatePage(page);
  });

  test('shows loading indicator while fetching and empty state when no records', async ({ page }) => {
    await page.route('/api/authors**', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 300));
      await route.continue();
    });

    await page.goto('/authors');
    await expect(page.getByTestId('loading-indicator')).toBeVisible();
    await expect(page.getByTestId('loading-indicator')).toBeHidden();
    await expect(page.getByTestId('empty-state')).toContainText('No authors found');
    await expect(page.getByTestId('authors-table')).not.toBeVisible();
  });

  test('displays records in a table and supports pagination', async ({ page, request }) => {
    for (let i = 1; i <= 25; i++) {
      await createAuthorViaApi(request, `AA Author ${String(i).padStart(2, '0')} E2E`);
    }

    await page.goto('/authors');
    await expect(page.getByTestId('authors-table')).toBeVisible();
    await expect(page.getByTestId('author-row')).toHaveCount(20);
    await expect(page.getByTestId('pagination')).toBeVisible();
    await expect(page.getByTestId('pagination-info')).toContainText('1–20 of 25');

    await page.getByTestId('pagination-next').click();
    await expect(page.getByTestId('author-row')).toHaveCount(5);
    await expect(page.getByTestId('pagination-info')).toContainText('21–25 of 25');
  });

  test('search refreshes the list and resets to page 1', async ({ page, request }) => {
    await createAuthorViaApi(request, 'Alpha E2E');
    for (let i = 1; i <= 22; i++) {
      await createAuthorViaApi(request, `Other ${i} E2E`);
    }

    await page.goto('/authors');
    await page.getByTestId('pagination-next').click();
    await expect(page.getByTestId('pagination-page-number')).toContainText('Page 2 of 2');

    await page.getByTestId('author-search').fill('Alpha');
    await expect(page.getByTestId('author-row')).toHaveCount(1);
    await expect(page.getByTestId('author-name')).toContainText('Alpha E2E');
    await expect(page.getByTestId('pagination')).not.toBeVisible();
  });

  test('creates an author and required fields are visually indicated', async ({ page }) => {
    await page.goto('/authors');
    await page.getByTestId('new-author-button').click();
    await expect(page.getByTestId('author-form')).toBeVisible();

    await page.getByTestId('author-form-name').fill('x');
    await page.getByTestId('author-form-name').clear();
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('author-form-name')).toHaveClass(/invalid/);
    await expect(page.getByTestId('author-form-name-required')).toContainText('required');

    const name = uniqueName('Author');
    const bio = 'A short biography for the author.';
    await page.getByTestId('author-form-name').fill(name);
    await page.getByTestId('author-form-bio').fill(bio);
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('success-message')).toContainText('Author created');
    const row = page.getByTestId('author-row').filter({ hasText: name });
    await expect(row).toBeVisible();
    await expect(row.getByTestId('author-bio')).toContainText(bio);
  });

  test('shows server field errors and non-field conflict errors', async ({ page, request }) => {
    const existingName = uniqueName('Author');
    await createAuthorViaApi(request, existingName);

    await page.goto('/authors');
    await page.getByTestId('new-author-button').click();

    await page.getByTestId('author-form-name').fill('a'.repeat(151));
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('author-form-name-error')).toBeVisible();

    await page.getByTestId('author-form-name').fill(existingName);
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('error-banner')).toContainText(/already exists|conflict/i);
  });

  test('edits an author and the list refreshes', async ({ page, request }) => {
    const author = await createAuthorViaApi(request, uniqueName('Author'));
    const updatedName = uniqueName('Updated Author');

    await page.goto('/authors');
    const row = page.getByTestId('author-row').filter({ hasText: author.name });
    await row.getByTestId('author-edit').click();

    await page.getByTestId('author-form-name').fill(updatedName);
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('success-message')).toContainText('Author updated');
    await expect(page.getByTestId('author-row').filter({ hasText: updatedName })).toBeVisible();
  });

  test('deletes an author after confirmation', async ({ page, request }) => {
    const author = await createAuthorViaApi(request, uniqueName('Author'));

    await page.goto('/authors');
    const row = page.getByTestId('author-row').filter({ hasText: author.name });

    await row.getByTestId('author-delete').click();
    await row.getByTestId('author-delete-confirm').click();

    await expect(page.getByTestId('success-message')).toContainText('Author deleted');
    await expect(page.getByTestId('author-row').filter({ hasText: author.name })).not.toBeVisible();
  });

  test('shows in-use message when deleting an author with associated books', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const author = await createAuthorViaApi(request, uniqueName('Author'));
    await createBookViaApi(request, {
      title: uniqueName('Book'),
      authorId: author.id,
      genreId: genre.id,
    });

    await page.goto('/authors');
    const row = page.getByTestId('author-row').filter({ hasText: author.name });

    await row.getByTestId('author-delete').click();
    await row.getByTestId('author-delete-confirm').click();

    await expect(page.getByTestId('error-banner')).toContainText(/in use|cannot be deleted/i);
    await expect(page.getByTestId('author-row').filter({ hasText: author.name })).toBeVisible();
  });
});
