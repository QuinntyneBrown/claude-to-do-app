import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AUTH_SERVICE, IAuthService } from 'api';

@Component({
  selector: 'tb-password-reset-request-form',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './password-reset-request-form.component.html',
  styleUrl: './password-reset-request-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PasswordResetRequestFormComponent {
  protected email = '';
  protected readonly submitting = signal(false);
  protected readonly submitted = signal(false);

  constructor(@Inject(AUTH_SERVICE) private readonly authService: IAuthService) {}

  protected submit(): void {
    if (this.submitting() || this.email.trim() === '') {
      return;
    }
    this.submitting.set(true);
    this.authService.requestPasswordReset({ email: this.email.trim() }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
      },
      error: () => {
        // Same UX whether or not the email exists (no enumeration).
        this.submitting.set(false);
        this.submitted.set(true);
      }
    });
  }
}
