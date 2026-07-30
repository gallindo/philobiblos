export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Genre {
  id: string;
  name: string;
}

export interface Author {
  id: string;
  name: string;
  bio: string | null;
}

export interface AuthorSummary {
  id: string;
  name: string;
}

export interface GenreSummary {
  id: string;
  name: string;
}

export interface Book {
  id: string;
  title: string;
  isbn: string | null;
  publishedYear: number | null;
  author: AuthorSummary;
  genre: GenreSummary;
}

export interface GenreCreateRequest {
  name: string;
}

export interface GenreUpdateRequest {
  name: string;
}

export interface AuthorCreateRequest {
  name: string;
  bio?: string;
}

export interface AuthorUpdateRequest {
  name: string;
  bio?: string;
}

export interface BookCreateRequest {
  title: string;
  authorId: string;
  genreId: string;
  isbn?: string;
  publishedYear?: number;
}

export interface BookUpdateRequest {
  title: string;
  authorId: string;
  genreId: string;
  isbn?: string;
  publishedYear?: number;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface ApiError {
  fieldErrors: Record<string, string>;
  nonFieldError: string | null;
}

export interface PagedListParams {
  [key: string]: string | number | boolean | undefined | null;
  page?: number;
  pageSize?: number;
  sort?: string;
  direction?: string;
}

export interface GenreListParams extends PagedListParams {
  name?: string;
}

export interface AuthorListParams extends PagedListParams {
  name?: string;
}

export interface BookListParams extends PagedListParams {
  title?: string;
  authorId?: string;
  genreId?: string;
}
