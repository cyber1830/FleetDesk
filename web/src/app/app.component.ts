import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <header class="topbar">
      <a routerLink="/jobs" class="brand">⚓ FleetDesk</a>
      @if (auth.isLoggedIn()) {
        <span class="who">{{ auth.email() }} · {{ auth.role() }}</span>
        <button (click)="auth.logout()">Sign out</button>
      }
    </header>
    <main><router-outlet /></main>
  `,
})
export class AppComponent {
  constructor(public auth: AuthService) {}
}
