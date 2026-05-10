import { ChangeDetectionStrategy, Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';
import { AuthStateService } from '../auth/auth-state.service';

@Component({
  selector: 'tb-oidc-callback-page',
  standalone: true,
  imports: [],
  templateUrl: './oidc-callback-page.component.html',
  styleUrl: './oidc-callback-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OidcCallbackPageComponent implements OnInit {
  constructor(
    @Inject(AUTH_SERVICE) private readonly authService: IAuthService,
    private readonly authState: AuthStateService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const code = this.route.snapshot.queryParamMap.get('code') ?? '';
    const state = this.route.snapshot.queryParamMap.get('state') ?? '';
    if (code === '' || state === '') {
      void this.router.navigateByUrl('/sign-in?reason=oidc_failed');
      return;
    }
    this.authService.completeOidcSignIn({ code, state }).subscribe({
      next: response => {
        this.authState.setAccessToken(response.accessToken);
        void this.router.navigateByUrl('/todos');
      },
      error: () => {
        void this.router.navigateByUrl('/sign-in?reason=oidc_failed');
      }
    });
  }
}
