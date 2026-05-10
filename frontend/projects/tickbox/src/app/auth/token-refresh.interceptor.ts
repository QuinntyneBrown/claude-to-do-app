import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AUTH_SERVICE } from 'api';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthStateService } from './auth-state.service';

const REFRESH_PATH = '/api/auth/refresh';

/**
 * On the first 401 from a non-refresh endpoint, attempts to silently refresh the
 * access token using the HttpOnly refresh-token cookie, then replays the original
 * request with the new bearer. Hard 401 (refresh itself failed) clears local auth
 * state and routes to /sign-in.
 */
export const tokenRefreshInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.endsWith(REFRESH_PATH) || req.url.includes('/api/auth/sign-in') || req.url.includes('/api/auth/register')) {
    return next(req);
  }

  const authState = inject(AuthStateService);
  const authService = inject(AUTH_SERVICE);
  const router = inject(Router);

  return next(req).pipe(
    catchError(error => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return authService.refreshAccessToken().pipe(
        switchMap(response => {
          authState.setAccessToken(response.accessToken);
          return next(req.clone({ setHeaders: { Authorization: `Bearer ${response.accessToken}` } }));
        }),
        catchError(refreshError => {
          authState.signOut();
          void router.navigateByUrl('/sign-in');
          return throwError(() => refreshError);
        })
      );
    })
  );
};
