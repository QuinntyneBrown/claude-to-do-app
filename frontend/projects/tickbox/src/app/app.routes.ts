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
    path: 'todos',
    canActivate: [authGuard],
    loadComponent: () => import('./todos/todos-page.component').then(m => m.TodosPageComponent)
  },
  { path: '**', redirectTo: 'todos' }
];
