import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SignInResponse } from 'api';
import { BrandIconComponent } from 'components';
import { SignUpFormComponent } from 'domain';
import { AuthStateService } from '../auth/auth-state.service';

@Component({
  selector: 'tb-sign-up-page',
  standalone: true,
  imports: [BrandIconComponent, SignUpFormComponent, RouterLink],
  templateUrl: './sign-up-page.component.html',
  styleUrl: './sign-up-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SignUpPageComponent {
  constructor(
    private readonly authState: AuthStateService,
    private readonly router: Router
  ) {}

  protected onSignedUp(response: SignInResponse): void {
    this.authState.setAccessToken(response.accessToken);
    void this.router.navigateByUrl('/todos');
  }
}
