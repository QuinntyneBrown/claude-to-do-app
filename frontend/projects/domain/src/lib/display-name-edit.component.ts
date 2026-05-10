import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Input, OnChanges, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ACCOUNT_SERVICE, IAccountService, MyProfile } from 'api';
import { ErrorBannerComponent } from 'components';

@Component({
  selector: 'tb-display-name-edit',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, ErrorBannerComponent],
  templateUrl: './display-name-edit.component.html',
  styleUrl: './display-name-edit.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DisplayNameEditComponent implements OnChanges {
  @Input({ required: true }) currentName = '';
  @Output() readonly updated = new EventEmitter<MyProfile>();

  protected displayName = '';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor(@Inject(ACCOUNT_SERVICE) private readonly accountService: IAccountService) {}

  ngOnChanges(): void {
    this.displayName = this.currentName;
  }

  protected save(): void {
    if (this.submitting()) {
      return;
    }
    const trimmed = this.displayName.trim();
    if (trimmed === '') {
      this.error.set('Display name cannot be blank.');
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.accountService.updateDisplayName({ displayName: trimmed }).subscribe({
      next: profile => {
        this.submitting.set(false);
        this.updated.emit(profile);
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Could not update display name.');
      }
    });
  }
}
