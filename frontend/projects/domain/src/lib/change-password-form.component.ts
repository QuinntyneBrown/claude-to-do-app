import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ACCOUNT_SERVICE, IAccountService } from 'api';
import { ErrorBannerComponent } from 'components';

@Component({
  selector: 'tb-change-password-form',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, ErrorBannerComponent],
  templateUrl: './change-password-form.component.html',
  styleUrl: './change-password-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChangePasswordFormComponent {
  @Output() readonly changed = new EventEmitter<void>();

  protected currentPassword = '';
  protected newPassword = '';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor(@Inject(ACCOUNT_SERVICE) private readonly accountService: IAccountService) {}

  protected submit(): void {
    if (this.submitting()) {
      return;
    }
    if (this.currentPassword === '' || this.newPassword === '') {
      this.error.set('Enter your current and new password.');
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.accountService
      .changePassword({ currentPassword: this.currentPassword, newPassword: this.newPassword })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.currentPassword = '';
          this.newPassword = '';
          this.changed.emit();
        },
        error: () => {
          this.submitting.set(false);
          this.error.set('Could not change password. Check your current password and try again.');
        }
      });
  }
}
