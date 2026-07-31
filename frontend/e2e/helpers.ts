import { APIRequestContext, Page } from '@playwright/test';

export interface Genre {
  id: string;
  name: string;
}

export interface Author {
  id: string;
  name: string;
  bio: string | null;
}

export interface Book {
  id: string;
  title: string;
  isbn: string | null;
  publishedYear: number | null;
  author: { id: string; name: string };
  genre: { id: string; name: string };
}

export function uniqueName(prefix: string): string {
  return `${prefix} E2E ${Date.now()} ${Math.random().toString(36).slice(2, 7)}`;
}

export async function authenticateApi(request: APIRequestContext): Promise<void> {
  const response = await request.post('/api/auth/test-login');
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Failed to authenticate API: ${response.status()} ${body}`);
  }
}

export async function authenticatePage(page: Page): Promise<void> {
  const response = await page.request.post('/api/auth/test-login');
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Failed to authenticate page: ${response.status()} ${body}`);
  }
}

export async function createGenreViaApi(request: APIRequestContext, name: string): Promise<Genre> {
  await authenticateApi(request);
  const response = await request.post('/api/genres', { data: { name } });
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Failed to create genre: ${response.status()} ${body}`);
  }
  return response.json();
}

export async function createAuthorViaApi(request: APIRequestContext, name: string, bio?: string): Promise<Author> {
  await authenticateApi(request);
  const response = await request.post('/api/authors', { data: { name, bio } });
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Failed to create author: ${response.status()} ${body}`);
  }
  return response.json();
}

export async function createBookViaApi(
  request: APIRequestContext,
  book: {
    title: string;
    authorId: string;
    genreId: string;
    isbn?: string;
    publishedYear?: number;
  }
): Promise<Book> {
  await authenticateApi(request);
  const response = await request.post('/api/books', { data: book });
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Failed to create book: ${response.status()} ${body}`);
  }
  return response.json();
}

export async function deleteGenreViaApi(request: APIRequestContext, id: string): Promise<void> {
  await authenticateApi(request);
  await request.delete(`/api/genres/${id}`);
}

export async function deleteAuthorViaApi(request: APIRequestContext, id: string): Promise<void> {
  await authenticateApi(request);
  await request.delete(`/api/authors/${id}`);
}

export async function deleteBookViaApi(request: APIRequestContext, id: string): Promise<void> {
  await authenticateApi(request);
  await request.delete(`/api/books/${id}`);
}

export async function listGenresViaApi(request: APIRequestContext, name?: string): Promise<Genre[]> {
  const response = await request.get('/api/genres', {
    params: { pageSize: 100, sort: 'name', direction: 'asc', ...(name ? { name } : {}) },
  });
  if (!response.ok()) {
    throw new Error(`Failed to list genres: ${response.status()}`);
  }
  const result = await response.json();
  return result.items;
}

export async function listAuthorsViaApi(request: APIRequestContext, name?: string): Promise<Author[]> {
  const response = await request.get('/api/authors', {
    params: { pageSize: 100, sort: 'name', direction: 'asc', ...(name ? { name } : {}) },
  });
  if (!response.ok()) {
    throw new Error(`Failed to list authors: ${response.status()}`);
  }
  const result = await response.json();
  return result.items;
}

export async function listBooksViaApi(request: APIRequestContext, title?: string): Promise<Book[]> {
  const response = await request.get('/api/books', {
    params: { pageSize: 100, sort: 'title', direction: 'asc', ...(title ? { title } : {}) },
  });
  if (!response.ok()) {
    throw new Error(`Failed to list books: ${response.status()}`);
  }
  const result = await response.json();
  return result.items;
}

export async function cleanupTestData(request: APIRequestContext): Promise<void> {
  const books = await listBooksViaApi(request);
  for (const book of books) {
    await deleteBookViaApi(request, book.id);
  }

  const authors = await listAuthorsViaApi(request);
  for (const author of authors) {
    await deleteAuthorViaApi(request, author.id);
  }

  const genres = await listGenresViaApi(request);
  for (const genre of genres) {
    await deleteGenreViaApi(request, genre.id);
  }
}
