import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ErrorService } from '../../core/error.service';
import { ApiError, Author, Book, BookListParams, Genre, PagedResult } from '../../core/models';
import { PaginationComponent } from '../../shared/components/pagination.component';

type BookForm = FormGroup<{
  id: FormControl<string | null>;
  title: FormControl<string>;
  authorId: FormControl<string>;
  genreId: FormControl<string>;
  isbn: FormControl<string>;
  publishedYear: FormControl<number | null>;
}>;

@Component({
  selector: 'app-book-list',
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent],
  templateUrl: './book-list.component.html',
  styleUrl: './book-list.component.scss'
})
export class BookListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly errorService = inject(ErrorService);
  private readonly fb = inject(FormBuilder);

  readonly data = signal<PagedResult<Book> | null>(null);
  readonly loading = signal(false);
  readonly search = signal('');
  readonly authorFilter = signal<string>('');
  readonly genreFilter = signal<string>('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly editing = signal<Book | null>(null);
  readonly isEditing = signal(false);
  readonly saving = signal(false);
  readonly message = signal<string | null>(null);

  readonly authors = signal<Author[]>([]);
  readonly genres = signal<Genre[]>([]);
  readonly catalogsLoading = signal(false);

  readonly form: BookForm = this.fb.group({
    id: this.fb.control<string | null>(null),
    title: this.fb.control<string>('', { validators: [Validators.required], nonNullable: true }),
    authorId: this.fb.control<string>('', { validators: [Validators.required], nonNullable: true }),
    genreId: this.fb.control<string>('', { validators: [Validators.required], nonNullable: true }),
    isbn: this.fb.control<string>('', { nonNullable: true }),
    publishedYear: this.fb.control<number | null>(null),
  });

  ngOnInit(): void {
    this.loadCatalogs();
    this.load();
  }

  loadCatalogs(): void {
    this.catalogsLoading.set(true);
    this.api.listAuthors({ pageSize: 100, sort: 'name', direction: 'asc' })
      .subscribe({
        next: result => this.authors.set(result.items),
        error: () => this.authors.set([]),
      });
    this.api.listGenres({ pageSize: 100, sort: 'name', direction: 'asc' })
      .subscribe({
        next: result => this.genres.set(result.items),
        error: () => this.genres.set([]),
      });
  }

  load(): void {
    this.loading.set(true);
    this.errorService.clear();

    const params: BookListParams = {
      page: this.page(),
      pageSize: this.pageSize,
      sort: 'title',
      direction: 'asc',
      title: this.search() || undefined,
      authorId: this.authorFilter() || undefined,
      genreId: this.genreFilter() || undefined,
    };

    this.api.listBooks(params)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => this.data.set(result),
        error: (error: ApiError) => this.errorService.show(error.nonFieldError ?? 'Failed to load books.'),
      });
  }

  onSearch(term: string): void {
    this.search.set(term);
    this.page.set(1);
    this.load();
  }

  onFilterChange(): void {
    this.page.set(1);
    this.load();
  }

  onPageChange(next: number): void {
    this.page.set(next);
    this.load();
  }

  startCreate(): void {
    this.editing.set(null);
    this.isEditing.set(true);
    this.form.reset({ id: null, title: '', authorId: '', genreId: '', isbn: '', publishedYear: null });
    this.errorService.clear();
    this.message.set(null);
  }

  startEdit(book: Book): void {
    this.editing.set(book);
    this.isEditing.set(true);
    this.form.setValue({
      id: book.id,
      title: book.title,
      authorId: book.author.id,
      genreId: book.genre.id,
      isbn: book.isbn ?? '',
      publishedYear: book.publishedYear,
    });
    this.errorService.clear();
    this.message.set(null);
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.isEditing.set(false);
    this.form.reset({ id: null, title: '', authorId: '', genreId: '', isbn: '', publishedYear: null });
    this.errorService.clear();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorService.clear();
    this.message.set(null);
    const value = this.form.getRawValue();

    const request = {
      title: value.title,
      authorId: value.authorId,
      genreId: value.genreId,
      isbn: value.isbn || undefined,
      publishedYear: value.publishedYear ?? undefined,
    };

    const request$ = value.id
      ? this.api.updateBook(value.id, request)
      : this.api.createBook(request);

    request$
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.editing.set(null);
          this.isEditing.set(false);
          this.form.reset({ id: null, title: '', authorId: '', genreId: '', isbn: '', publishedYear: null });
          this.message.set(value.id ? 'Book updated.' : 'Book created.');
          this.load();
        },
        error: (error: ApiError) => this.handleError(error),
      });
  }

  deleteBook(book: Book): void {
    if (!window.confirm(`Delete book "${book.title}"?`)) {
      return;
    }

    this.api.deleteBook(book.id).subscribe({
      next: () => {
        this.message.set('Book deleted.');
        this.load();
      },
      error: (error: ApiError) => {
        this.errorService.show(error.nonFieldError || 'This book could not be deleted.');
      },
    });
  }

  private handleError(error: ApiError): void {
    if (error.nonFieldError) {
      this.errorService.show(error.nonFieldError);
    } else {
      this.errorService.clear();
    }

    for (const [key, message] of Object.entries(error.fieldErrors)) {
      const controlName = Object.keys(this.form.controls).find(
        (name) => name.toLowerCase() === key.toLowerCase()
      );
      const control = controlName ? this.form.get(controlName) : null;
      if (control) {
        control.setErrors({ server: message });
      }
    }
  }
}
