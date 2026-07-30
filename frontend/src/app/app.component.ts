import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ErrorService } from './core/error.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'Philobiblos';
  readonly errorService = inject(ErrorService);
  sections = [
    { path: '/genres', label: 'Genres' },
    { path: '/authors', label: 'Authors' },
    { path: '/books', label: 'Books' },
  ];
}
