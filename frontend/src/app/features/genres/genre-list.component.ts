import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ErrorService } from '../../core/error.service';
import { ApiError, Genre, GenreListParams, PagedResult } from '../../core/models';
import { PaginationComponent } from '../../shared/components/pagination.component';

type GenreForm = FormGroup<{
  id: FormControl<string | null>;
  name: FormControl<string>;
}>;

@Component({
  selector: 'app-genre-list',
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent],
  templateUrl: './genre-list.component.html',
  styleUrl: './genre-list.component.scss'
})
export class GenreListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly errorService = inject(ErrorService);
  private readonly fb = inject(FormBuilder);

  readonly data = signal<PagedResult<Genre> | null>(null);
  readonly loading = signal(false);
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly editing = signal<Genre | null>(null);
  readonly isEditing = signal(false);
  readonly saving = signal(false);
  readonly message = signal<string | null>(null);

  readonly form: GenreForm = this.fb.group({
    id: this.fb.control<string | null>(null),
    name: this.fb.control<string>('', { validators: [Validators.required], nonNullable: true }),
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.message.set(null);
    this.errorService.clear();

    const params: GenreListParams = {
      page: this.page(),
      pageSize: this.pageSize,
      sort: 'name',
      direction: 'asc',
      name: this.search() || undefined,
    };

    this.api.listGenres(params)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => this.data.set(result),
        error: (error: ApiError) => this.errorService.show(error.nonFieldError ?? 'Failed to load genres.'),
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
    this.form.reset({ id: null, name: '' });
    this.errorService.clear();
    this.message.set(null);
  }

  startEdit(genre: Genre): void {
    this.editing.set(genre);
    this.isEditing.set(true);
    this.form.setValue({ id: genre.id, name: genre.name });
    this.errorService.clear();
    this.message.set(null);
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.isEditing.set(false);
    this.form.reset({ id: null, name: '' });
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
      ? this.api.updateGenre(value.id, { name: value.name })
      : this.api.createGenre({ name: value.name });

    request$
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.editing.set(null);
          this.isEditing.set(false);
          this.form.reset({ id: null, name: '' });
          this.message.set(value.id ? 'Genre updated.' : 'Genre created.');
          this.load();
        },
        error: (error: ApiError) => this.handleError(error),
      });
  }

  deleteGenre(genre: Genre): void {
    if (!window.confirm(`Delete genre "${genre.name}"?`)) {
      return;
    }

    this.api.deleteGenre(genre.id).subscribe({
      next: () => {
        this.message.set('Genre deleted.');
        this.load();
      },
      error: (error: ApiError) => {
        this.errorService.show(error.nonFieldError || 'This genre is in use and cannot be deleted.');
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
      const control = this.form.get(key);
      if (control) {
        control.setErrors({ server: message });
      }
    }
  }
}
