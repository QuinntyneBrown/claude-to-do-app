import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AUTH_SERVICE, IAuthService, SignInResponse } from 'api';

@Component({
  selector: 'tb-sign-up-form',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './sign-up-form.component.html',
  styleUrl: './sign-up-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SignUpFormComponent {
  @Output() readonly signedUp = new EventEmitter<SignInResponse>();

  protected displayName = '';
  protected email = '';
  protected password = '';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor(@Inject(AUTH_SERVICE) private readonly authService: IAuthService) {}

  protected submit(): void {
    if (this.submitting()) {
      return;
    }
    if (this.displayName.trim() === '' || this.email.trim() === '' || this.password === '') {
      this.error.set('Please fill in every field.');
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.authService
      .register({
        displayName: this.displayName.trim(),
        email: this.email.trim(),
        password: this.password
      })
      .subscribe({
        next: response => {
          this.submitting.set(false);
          this.signedUp.emit(response);
        },
        error: () => {
          this.submitting.set(false);
          this.error.set('Could not create the account. Check your details and try again.');
        }
      });
  }
}
