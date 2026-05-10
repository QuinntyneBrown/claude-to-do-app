import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AUTH_SERVICE, IAuthService, SignInResponse } from 'api';

@Component({
  selector: 'tb-password-reset-complete-form',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './password-reset-complete-form.component.html',
  styleUrl: './password-reset-complete-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PasswordResetCompleteFormComponent {
  @Input({ required: true }) token = '';
  @Output() readonly completed = new EventEmitter<SignInResponse>();

  protected newPassword = '';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor(@Inject(AUTH_SERVICE) private readonly authService: IAuthService) {}

  protected submit(): void {
    if (this.submitting() || this.newPassword === '' || this.token === '') {
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.authService.completePasswordReset({ token: this.token, newPassword: this.newPassword }).subscribe({
      next: response => {
        this.submitting.set(false);
        this.completed.emit(response);
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('The reset link is invalid or has expired.');
      }
    });
  }
}
