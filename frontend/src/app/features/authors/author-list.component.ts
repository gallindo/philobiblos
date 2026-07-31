import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
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
  private readonly authService = inject(AuthService);
  private readonly errorService = inject(ErrorService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEditor = computed(() => this.authService.isEditor());

  readonly data = signal<PagedResult<Author> | null>(null);
  readonly loading = signal(false);
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly editing = signal<Author | null>(null);
  readonly isEditing = signal(false);
  readonly saving = signal(false);
  readonly message = signal<string | null>(null);
  readonly confirmingDelete = signal<Author | null>(null);

  readonly form: AuthorForm = this.fb.group({
    id: this.fb.control<string | null>(null),
    name: this.fb.control<string>('', { validators: [Validators.required], nonNullable: true }),
    bio: this.fb.control<string>('', { nonNullable: true }),
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
    this.form.reset({ id: null, name: '', bio: '' });
    this.errorService.clear();
    this.message.set(null);
  }

  startEdit(author: Author): void {
    this.editing.set(author);
    this.isEditing.set(true);
    this.confirmingDelete.set(null);
    this.form.setValue({ id: author.id, name: author.name, bio: author.bio ?? '' });
    this.errorService.clear();
    this.message.set(null);
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.isEditing.set(false);
    this.confirmingDelete.set(null);
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

  requestDelete(author: Author): void {
    this.confirmingDelete.set(author);
  }

  cancelDelete(): void {
    this.confirmingDelete.set(null);
  }

  confirmDelete(author: Author): void {
    this.confirmingDelete.set(null);
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
