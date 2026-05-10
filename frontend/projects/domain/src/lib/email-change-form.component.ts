import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ACCOUNT_SERVICE, IAccountService, MyProfile } from 'api';
import { ErrorBannerComponent } from 'components';

@Component({
  selector: 'tb-email-change-form',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, ErrorBannerComponent],
  templateUrl: './email-change-form.component.html',
  styleUrl: './email-change-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmailChangeFormComponent {
  @Output() readonly requested = new EventEmitter<MyProfile>();

  protected newEmail = '';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor(@Inject(ACCOUNT_SERVICE) private readonly accountService: IAccountService) {}

  protected submit(): void {
    if (this.submitting()) {
      return;
    }
    const trimmed = this.newEmail.trim();
    if (trimmed === '') {
      this.error.set('Enter a new email address.');
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.accountService.requestEmailChange({ newEmail: trimmed }).subscribe({
      next: profile => {
        this.submitting.set(false);
        this.newEmail = '';
        this.requested.emit(profile);
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Could not request email change. Check the address and try again.');
      }
    });
  }
}
