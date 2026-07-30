import { Routes } from '@angular/router';
import { GenreListComponent } from './features/genres/genre-list.component';
import { AuthorListComponent } from './features/authors/author-list.component';
import { BookListComponent } from './features/books/book-list.component';

export const routes: Routes = [
  { path: '', redirectTo: '/genres', pathMatch: 'full' },
  { path: 'genres', component: GenreListComponent },
  { path: 'authors', component: AuthorListComponent },
  { path: 'books', component: BookListComponent },
];
