import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly data = signal<PagedResult<Genre> | null>(null);
  readonly loading = signal(false);
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly editing = signal<Genre | null>(null);
  readonly isEditing = signal(false);
  readonly saving = signal(false);
  readonly message = signal<string | null>(null);
  readonly confirmingDelete = signal<Genre | null>(null);

  readonly form: GenreForm = this.fb.group({
    id: this.fb.control<string | null>(null),
    name: this.fb.control<string>('', { validators: [Validators.required], nonNullable: true }),
  });

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    this.search.set(params['search'] ?? '');
    this.page.set(parseInt(params['page'] ?? '1', 10) || 1);
    this.load();
  }

  load(): void {
    this.loading.set(true);
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
    this.confirmingDelete.set(null);
    this.updateUrl();
    this.load();
  }

  onPageChange(next: number): void {
    this.page.set(next);
    this.updateUrl();
    this.load();
  }

  private updateUrl(): void {
    const queryParams: Record<string, string | number> = {};
    const search = this.search();
    if (search) queryParams['search'] = search;
    if (this.page() !== 1) queryParams['page'] = this.page();

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      replaceUrl: true,
    });
  }

  startCreate(): void {
    this.editing.set(null);
    this.isEditing.set(true);
    this.confirmingDelete.set(null);
    this.form.reset({ id: null, name: '' });
    this.errorService.clear();
    this.message.set(null);
  }

  startEdit(genre: Genre): void {
    this.editing.set(genre);
    this.isEditing.set(true);
    this.confirmingDelete.set(null);
    this.form.setValue({ id: genre.id, name: genre.name });
    this.errorService.clear();
    this.message.set(null);
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.isEditing.set(false);
    this.confirmingDelete.set(null);
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

  requestDelete(genre: Genre): void {
    this.confirmingDelete.set(genre);
  }

  cancelDelete(): void {
    this.confirmingDelete.set(null);
  }

  confirmDelete(genre: Genre): void {
    this.confirmingDelete.set(null);
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
