import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Author,
  AuthorCreateRequest,
  AuthorListParams,
  AuthorUpdateRequest,
  Book,
  BookCreateRequest,
  BookListParams,
  BookUpdateRequest,
  Genre,
  GenreCreateRequest,
  GenreListParams,
  GenreUpdateRequest,
  PagedResult,
} from './models';

function toHttpParams(
  params: Record<string, string | number | boolean | undefined | null>
): HttpParams {
  let httpParams = new HttpParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      httpParams = httpParams.set(key, String(value));
    }
  }
  return httpParams;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  // Genres

  listGenres(params: GenreListParams): Observable<PagedResult<Genre>> {
    return this.http.get<PagedResult<Genre>>(`${this.baseUrl}/genres`, {
      params: toHttpParams(params),
    });
  }

  getGenre(id: string): Observable<Genre> {
    return this.http.get<Genre>(`${this.baseUrl}/genres/${id}`);
  }

  createGenre(request: GenreCreateRequest): Observable<Genre> {
    return this.http.post<Genre>(`${this.baseUrl}/genres`, request);
  }

  updateGenre(id: string, request: GenreUpdateRequest): Observable<Genre> {
    return this.http.put<Genre>(`${this.baseUrl}/genres/${id}`, request);
  }

  deleteGenre(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/genres/${id}`);
  }

  listAuthors(params: AuthorListParams): Observable<PagedResult<Author>> {
    return this.http.get<PagedResult<Author>>(`${this.baseUrl}/authors`, {
      params: toHttpParams(params),
    });
  }

  getAuthor(id: string): Observable<Author> {
    return this.http.get<Author>(`${this.baseUrl}/authors/${id}`);
  }

  createAuthor(request: AuthorCreateRequest): Observable<Author> {
    return this.http.post<Author>(`${this.baseUrl}/authors`, request);
  }

  updateAuthor(id: string, request: AuthorUpdateRequest): Observable<Author> {
    return this.http.put<Author>(`${this.baseUrl}/authors/${id}`, request);
  }

  deleteAuthor(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/authors/${id}`);
  }

  listBooks(params: BookListParams): Observable<PagedResult<Book>> {
    return this.http.get<PagedResult<Book>>(`${this.baseUrl}/books`, {
      params: toHttpParams(params),
    });
  }

  getBook(id: string): Observable<Book> {
    return this.http.get<Book>(`${this.baseUrl}/books/${id}`);
  }

  createBook(request: BookCreateRequest): Observable<Book> {
    return this.http.post<Book>(`${this.baseUrl}/books`, request);
  }

  updateBook(id: string, request: BookUpdateRequest): Observable<Book> {
    return this.http.put<Book>(`${this.baseUrl}/books/${id}`, request);
  }

  deleteBook(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/books/${id}`);
  }
}
