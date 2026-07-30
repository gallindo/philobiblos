import { test, expect } from '@playwright/test';
import {
  cleanupTestData,
  createGenreViaApi,
  createAuthorViaApi,
  createBookViaApi,
  uniqueName,
} from './helpers';

test.describe('AC-5.2 / AC-5.3 / AC-5.4 / AC-5.5 Books', () => {
  test.beforeEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test('shows loading indicator and empty state when no records', async ({ page }) => {
    await page.route('/api/books**', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 300));
      await route.continue();
    });

    await page.goto('/books');
    await expect(page.getByTestId('loading-indicator')).toBeVisible();
    await expect(page.getByTestId('loading-indicator')).toBeHidden();
    await expect(page.getByTestId('empty-state')).toContainText('No books found');
  });

  test('list shows author and genre names and supports pagination', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const author = await createAuthorViaApi(request, uniqueName('Author'));
    for (let i = 1; i <= 25; i++) {
      await createBookViaApi(request, {
        title: `AA Book ${String(i).padStart(2, '0')} E2E`,
        authorId: author.id,
        genreId: genre.id,
      });
    }

    await page.goto('/books');
    await expect(page.getByTestId('books-table')).toBeVisible();
    await expect(page.getByTestId('book-row')).toHaveCount(20);
    await expect(page.getByTestId('book-author').first()).toContainText(author.name);
    await expect(page.getByTestId('book-genre').first()).toContainText(genre.name);
    await expect(page.getByTestId('pagination')).toBeVisible();

    await page.getByTestId('pagination-next').click();
    await expect(page.getByTestId('book-row')).toHaveCount(5);
  });

  test('search refreshes the list and filters by author and genre', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const author = await createAuthorViaApi(request, uniqueName('Author'));
    await createBookViaApi(request, {
      title: 'Unique Alpha E2E',
      authorId: author.id,
      genreId: genre.id,
    });
    await createBookViaApi(request, {
      title: 'Other Beta E2E',
      authorId: author.id,
      genreId: genre.id,
    });

    await page.goto('/books');
    await page.getByTestId('book-search').fill('Alpha');
    await expect(page.getByTestId('book-row')).toHaveCount(1);
    await expect(page.getByTestId('book-title')).toContainText('Unique Alpha E2E');

    await page.getByTestId('book-search').clear();
    await page.getByTestId('author-filter').selectOption({ label: author.name });
    await expect(page.getByTestId('book-row')).toHaveCount(2);

    await page.getByTestId('genre-filter').selectOption({ label: genre.name });
    await expect(page.getByTestId('book-row')).toHaveCount(2);

    await page.getByTestId('book-search').fill('NoMatchingTerm');
    await expect(page.getByTestId('book-row')).toHaveCount(0);
    await page.getByTestId('clear-filters').click();
    await expect(page.getByTestId('book-row')).toHaveCount(2);
  });

  test('create book form has populated author and genre selects and creates a book', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const author = await createAuthorViaApi(request, uniqueName('Author'));

    await page.goto('/books');
    await page.getByTestId('new-book-button').click();
    await expect(page.getByTestId('book-form')).toBeVisible();

    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('book-form-title')).toHaveClass(/invalid/);
    await expect(page.getByTestId('book-form-author')).toHaveClass(/invalid/);
    await expect(page.getByTestId('book-form-genre')).toHaveClass(/invalid/);

    await expect(page.getByTestId('book-form-author')).toContainText(author.name);
    await expect(page.getByTestId('book-form-genre')).toContainText(genre.name);

    const title = uniqueName('Book');
    await page.getByTestId('book-form-title').fill(title);
    await page.getByTestId('book-form-author').selectOption({ label: author.name });
    await page.getByTestId('book-form-genre').selectOption({ label: genre.name });
    await page.getByTestId('book-form-published-year').fill('2020');
    await page.getByTestId('save-button').click();

    await expect(page.getByTestId('success-message')).toContainText('Book created');
    const row = page.getByTestId('book-row').filter({ hasText: title });
    await expect(row).toBeVisible();
    await expect(row.getByTestId('book-author')).toContainText(author.name);
    await expect(row.getByTestId('book-genre')).toContainText(genre.name);
  });

  test('book form validates relationships and shows server errors', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const author = await createAuthorViaApi(request, uniqueName('Author'));
    const existingBook = await createBookViaApi(request, {
      title: uniqueName('Book'),
      authorId: author.id,
      genreId: genre.id,
      isbn: '9780134685991',
    });

    await page.goto('/books');
    await page.getByTestId('new-book-button').click();

    await page.getByTestId('book-form-title').fill(existingBook.title);
    await page.getByTestId('book-form-author').selectOption({ label: author.name });
    await page.getByTestId('book-form-genre').selectOption({ label: genre.name });
    await page.getByTestId('book-form-isbn').fill('9780134685991');
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('error-banner')).toContainText(/already exists|conflict/i);

    await page.getByTestId('book-form-isbn').fill('not-an-isbn');
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('book-form-isbn-error')).toBeVisible();

    await page.getByTestId('cancel-button').click();
    await page.getByTestId('new-book-button').click();
    await page.getByTestId('book-form-title').fill(uniqueName('Book'));
    await page.getByTestId('book-form-author').selectOption({ label: author.name });
    await page.getByTestId('book-form-genre').selectOption({ label: genre.name });
    await page.getByTestId('book-form-published-year').fill('3000');
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('book-form-published-year-error')).toBeVisible();
  });

  test('edits a book and the list refreshes', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const author = await createAuthorViaApi(request, uniqueName('Author'));
    const book = await createBookViaApi(request, {
      title: uniqueName('Book'),
      authorId: author.id,
      genreId: genre.id,
    });
    const newTitle = uniqueName('Updated Book');

    await page.goto('/books');
    const row = page.getByTestId('book-row').filter({ hasText: book.title });
    await row.getByTestId('book-edit').click();

    await page.getByTestId('book-form-title').fill(newTitle);
    await page.getByTestId('save-button').click();
    await expect(page.getByTestId('success-message')).toContainText('Book updated');
    await expect(page.getByTestId('book-row').filter({ hasText: newTitle })).toBeVisible();
  });

  test('deletes a book after confirmation', async ({ page, request }) => {
    const genre = await createGenreViaApi(request, uniqueName('Genre'));
    const author = await createAuthorViaApi(request, uniqueName('Author'));
    const book = await createBookViaApi(request, {
      title: uniqueName('Book'),
      authorId: author.id,
      genreId: genre.id,
    });

    await page.goto('/books');
    const row = page.getByTestId('book-row').filter({ hasText: book.title });

    await row.getByTestId('book-delete').click();
    await row.getByTestId('book-delete-confirm').click();

    await expect(page.getByTestId('success-message')).toContainText('Book deleted');
    await expect(page.getByTestId('book-row').filter({ hasText: book.title })).not.toBeVisible();
  });
});
