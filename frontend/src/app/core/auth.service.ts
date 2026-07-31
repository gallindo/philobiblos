import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { LoginRequest, RegisterRequest, User } from './models';
import { catchError, of, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUser = signal<User | null>(null);

  readonly user = computed(() => this.currentUser());
  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  readonly isEditor = computed(() =>
    this.currentUser()?.roles.includes('Editor') ?? false
  );
  readonly isAdmin = computed(() =>
    this.currentUser()?.roles.includes('Admin') ?? false
  );

  loadUser(): void {
    this.http
      .get<User | null>('/api/auth/me', { withCredentials: true })
      .subscribe({
        next: (user) => this.currentUser.set(user),
        error: () => this.currentUser.set(null),
      });
  }

  loginWithGoogle(): void {
    window.location.href = '/api/auth/login';
  }

  loginWithEmailPassword(request: LoginRequest) {
    return this.http
      .post<User>('/api/auth/login', request, { withCredentials: true })
      .pipe(
        tap((user) => this.currentUser.set(user)),
        catchError((error) => {
          this.currentUser.set(null);
          throw error;
        })
      );
  }

  register(request: RegisterRequest) {
    return this.http
      .post<User>('/api/auth/register', request, { withCredentials: true })
      .pipe(
        tap((user) => this.currentUser.set(user)),
        catchError((error) => {
          this.currentUser.set(null);
          throw error;
        })
      );
  }

  logout(): void {
    this.http
      .post('/api/auth/logout', {}, { withCredentials: true, responseType: 'text' })
      .subscribe({
        next: () => {
          this.currentUser.set(null);
          window.location.href = '/';
        },
        error: () => {
          this.currentUser.set(null);
          window.location.href = '/';
        },
      });
  }
}
