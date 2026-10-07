import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  template: `
    <section class="card narrow">
      <h2>{{ mode() === 'login' ? 'Sign in' : 'Create account' }}</h2>
      <label>Email
        <input type="email" name="email" [(ngModel)]="email" autocomplete="username" />
      </label>
      <label>Password
        <input type="password" name="password" [(ngModel)]="password" autocomplete="current-password" />
      </label>
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
      <button class="primary" (click)="submit()" [disabled]="busy()">
        {{ mode() === 'login' ? 'Sign in' : 'Register' }}
      </button>
      <p class="muted">
        @if (mode() === 'login') {
          No account? <a href="#" (click)="toggle($event)">Register</a>
        } @else {
          Already registered? <a href="#" (click)="toggle($event)">Sign in</a>
        }
      </p>
      <p class="muted small">New accounts are read-only (Viewer). Dispatchers and admins can create and edit jobs.</p>
    </section>
  `,
})
export class LoginComponent {
  email = '';
  password = '';
  mode = signal<'login' | 'register'>('login');
  error = signal('');
  busy = signal(false);

  constructor(private auth: AuthService, private router: Router) {}

  toggle(e: Event): void {
    e.preventDefault();
    this.error.set('');
    this.mode.update((m) => (m === 'login' ? 'register' : 'login'));
  }

  submit(): void {
    this.error.set('');
    this.busy.set(true);
    const call =
      this.mode() === 'login'
        ? this.auth.login(this.email, this.password)
        : this.auth.register(this.email, this.password);

    call.subscribe({
      next: () => {
        this.busy.set(false);
        this.router.navigateByUrl('/jobs');
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(err.error?.error ?? 'Something went wrong. Check your details and try again.');
      },
    });
  }
}
