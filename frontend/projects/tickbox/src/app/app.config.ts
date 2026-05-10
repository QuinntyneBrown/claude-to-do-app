import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { ACCOUNT_SERVICE, AccountService, API_CONFIG, AUTH_SERVICE, AuthService, TODOS_SERVICE, TodosService } from 'api';
import { authInterceptor } from './auth/auth.interceptor';
import { tokenRefreshInterceptor } from './auth/token-refresh.interceptor';
import { routes } from './app.routes';

const environmentApiBaseUrl =
  (typeof window !== 'undefined' && (window as { __TICKBOX_API__?: string }).__TICKBOX_API__) ||
  'http://localhost:5217';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideAnimationsAsync(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, tokenRefreshInterceptor])),
    { provide: API_CONFIG, useValue: { baseUrl: environmentApiBaseUrl } },
    { provide: AUTH_SERVICE, useClass: AuthService },
    { provide: TODOS_SERVICE, useClass: TodosService },
    { provide: ACCOUNT_SERVICE, useClass: AccountService }
  ]
};
