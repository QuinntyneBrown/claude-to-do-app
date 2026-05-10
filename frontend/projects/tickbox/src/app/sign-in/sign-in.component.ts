import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';
import { BrandIconComponent, ErrorBannerComponent } from 'components';
import { OidcSignInButtonComponent } from 'domain';
import { AuthStateService } from '../auth/auth-state.service';

@Component({
  selector: 'tb-sign-in',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    RouterLink,
    BrandIconComponent,
    ErrorBannerComponent,
    OidcSignInButtonComponent
  ],
  templateUrl: './sign-in.component.html',
  styleUrl: './sign-in.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SignInComponent {
  protected email = '';
  protected password = '';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor(
    @Inject(AUTH_SERVICE) private readonly authService: IAuthService,
    private readonly authState: AuthStateService,
    private readonly router: Router
  ) {}

  protected submit(): void {
    if (this.submitting()) {
      return;
    }
    if (this.email.trim() === '' || this.password === '') {
      this.error.set('Enter your email and password.');
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.authService.signIn({ email: this.email.trim(), password: this.password }).subscribe({
      next: response => {
        this.authState.setAccessToken(response.accessToken);
        this.submitting.set(false);
        void this.router.navigateByUrl('/todos');
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Incorrect email or password.');
      }
    });
  }
}
