import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ErrorService } from './core/error.service';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Philobiblos';
  readonly errorService = inject(ErrorService);
  readonly authService = inject(AuthService);

  sections = [
    { path: '/genres', label: 'Genres' },
    { path: '/authors', label: 'Authors' },
    { path: '/books', label: 'Books' },
  ];

  ngOnInit(): void {
    this.authService.loadUser();
  }
}
