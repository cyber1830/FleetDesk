import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse } from './models';

const STORAGE_KEY = 'fleetdesk.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<AuthResponse | null>(this.restore());

  readonly isLoggedIn = computed(() => {
    const s = this.session();
    return !!s && new Date(s.expiresAtUtc).getTime() > Date.now();
  });
  readonly email = computed(() => this.session()?.email ?? '');
  readonly role = computed(() => this.session()?.role ?? 'Viewer');
  readonly canEdit = computed(() => this.role() === 'Admin' || this.role() === 'Dispatcher');
  readonly isAdmin = computed(() => this.role() === 'Admin');

  constructor(private http: HttpClient, private router: Router) {}

  get token(): string | null {
    return this.isLoggedIn() ? this.session()!.token : null;
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', { email, password }).pipe(tap((r) => this.store(r)));
  }

  register(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/register', { email, password }).pipe(tap((r) => this.store(r)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
    this.router.navigateByUrl('/login');
  }

  private store(r: AuthResponse): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(r));
    this.session.set(r);
  }

  private restore(): AuthResponse | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as AuthResponse) : null;
    } catch {
      return null;
    }
  }
}
