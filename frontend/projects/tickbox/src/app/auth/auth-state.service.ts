import { Injectable, computed, signal } from '@angular/core';

const TOKEN_KEY = 'tickbox.access-token';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private readonly token = signal<string | null>(this.readToken());

  readonly isSignedIn = computed(() => this.token() !== null);

  getAccessToken(): string | null {
    return this.token();
  }

  setAccessToken(token: string): void {
    sessionStorage.setItem(TOKEN_KEY, token);
    this.token.set(token);
  }

  signOut(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    this.token.set(null);
  }

  private readToken(): string | null {
    if (typeof sessionStorage === 'undefined') {
      return null;
    }
    return sessionStorage.getItem(TOKEN_KEY);
  }
}
