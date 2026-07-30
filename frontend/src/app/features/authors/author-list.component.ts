import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ErrorService } from '../../core/error.service';
import { ApiError, Author, AuthorListParams, PagedResult } from '../../core/models';
import { PaginationComponent } from '../../shared/components/pagination.component';

type AuthorForm = FormGroup<{
  id: FormControl<string | null>;
  name: FormControl<string>;
  bio: FormControl<string>;
}>;

@Component({
  selector: 'app-author-list',
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent],
  templateUrl: './author-list.component.html',
  styleUrl: './author-list.component.scss'
})
export class AuthorListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly errorService = inject(ErrorService);
  private readonly fb = inject(FormBuilder);

  readonly data = signal<PagedResult<Author> | null>(null);
  readonly loading = signal(false);
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly editing = signal<Author | null>(null);
  readonly isEditing = signal(false);
  readonly saving = signal(false);
  readonly message = signal<string | null>(null);

  readonly form: AuthorForm = this.fb.group({
    id: this.fb.control<string | null>(null),
    name: this.fb.control<string>('', { validators: [Validators.required], nonNullable: true }),
    bio: this.fb.control<string>('', { nonNullable: true }),
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorService.clear();

    const params: AuthorListParams = {
      page: this.page(),
      pageSize: this.pageSize,
      sort: 'name',
      direction: 'asc',
      name: this.search() || undefined,
    };

    this.api.listAuthors(params)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => this.data.set(result),
        error: (error: ApiError) => this.errorService.show(error.nonFieldError ?? 'Failed to load authors.'),
      });
  }

  onSearch(term: string): void {
    this.search.set(term);
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
    this.form.reset({ id: null, name: '', bio: '' });
    this.errorService.clear();
    this.message.set(null);
  }

  startEdit(author: Author): void {
    this.editing.set(author);
    this.isEditing.set(true);
    this.form.setValue({ id: author.id, name: author.name, bio: author.bio ?? '' });
    this.errorService.clear();
    this.message.set(null);
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.isEditing.set(false);
    this.form.reset({ id: null, name: '', bio: '' });
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

    const request$ = value.id
      ? this.api.updateAuthor(value.id, { name: value.name, bio: value.bio || undefined })
      : this.api.createAuthor({ name: value.name, bio: value.bio || undefined });

    request$
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.editing.set(null);
          this.isEditing.set(false);
          this.form.reset({ id: null, name: '', bio: '' });
          this.message.set(value.id ? 'Author updated.' : 'Author created.');
          this.load();
        },
        error: (error: ApiError) => this.handleError(error),
      });
  }

  deleteAuthor(author: Author): void {
    if (!window.confirm(`Delete author "${author.name}"?`)) {
      return;
    }

    this.api.deleteAuthor(author.id).subscribe({
      next: () => {
        this.message.set('Author deleted.');
        this.load();
      },
      error: (error: ApiError) => {
        this.errorService.show(error.nonFieldError || 'This author is in use and cannot be deleted.');
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
