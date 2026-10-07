import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';
import { JobsComponent } from './jobs.component';
import { LoginComponent } from './login.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'jobs', component: JobsComponent, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'jobs' },
  { path: '**', redirectTo: 'jobs' },
];
