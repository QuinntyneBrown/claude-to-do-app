import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'todos' },
  {
    path: 'sign-in',
    loadComponent: () => import('./sign-in/sign-in.component').then(m => m.SignInComponent)
  },
  {
    path: 'sign-up',
    loadComponent: () => import('./sign-up/sign-up-page.component').then(m => m.SignUpPageComponent)
  },
  {
    path: 'password-reset/request',
    loadComponent: () => import('./password-reset/request-password-reset-page.component').then(m => m.RequestPasswordResetPageComponent)
  },
  {
    path: 'password-reset/complete',
    loadComponent: () => import('./password-reset/complete-password-reset-page.component').then(m => m.CompletePasswordResetPageComponent)
  },
  {
    path: 'oidc/callback',
    loadComponent: () => import('./oidc/oidc-callback-page.component').then(m => m.OidcCallbackPageComponent)
  },
  {
    path: 'todos',
    canActivate: [authGuard],
    loadComponent: () => import('./todos/todos-page.component').then(m => m.TodosPageComponent)
  },
  {
    path: 'todos/new',
    canActivate: [authGuard],
    loadComponent: () => import('./todos/todo-detail-page.component').then(m => m.TodoDetailPageComponent)
  },
  {
    path: 'todos/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./todos/todo-detail-page.component').then(m => m.TodoDetailPageComponent)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./profile/profile-page.component').then(m => m.ProfilePageComponent)
  },
  {
    path: 'email-change/confirm',
    loadComponent: () => import('./email-change/confirm-email-change-page.component').then(m => m.ConfirmEmailChangePageComponent)
  },
  {
    path: 'error',
    loadComponent: () => import('./error/error-page.component').then(m => m.ErrorPageComponent)
  },
  { path: '**', redirectTo: '/error?reason=not_found' }
];
