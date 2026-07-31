import { Routes } from '@angular/router';
import { GenreListComponent } from './features/genres/genre-list.component';
import { AuthorListComponent } from './features/authors/author-list.component';
import { BookListComponent } from './features/books/book-list.component';
import { LoginComponent } from './features/login/login.component';
import { RegisterComponent } from './features/register/register.component';

export const routes: Routes = [
  { path: '', redirectTo: '/genres', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'genres', component: GenreListComponent },
  { path: 'authors', component: AuthorListComponent },
  { path: 'books', component: BookListComponent },
];
