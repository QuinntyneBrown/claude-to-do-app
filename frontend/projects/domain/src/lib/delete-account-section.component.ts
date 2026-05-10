import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { ACCOUNT_SERVICE, IAccountService } from 'api';
import { ConfirmDialogComponent, ConfirmDialogData } from 'components';

@Component({
  selector: 'tb-delete-account-section',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './delete-account-section.component.html',
  styleUrl: './delete-account-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeleteAccountSectionComponent {
  @Output() readonly deleted = new EventEmitter<void>();

  protected readonly submitting = signal(false);

  constructor(
    @Inject(ACCOUNT_SERVICE) private readonly accountService: IAccountService,
    private readonly dialog: MatDialog
  ) {}

  protected requestDelete(): void {
    if (this.submitting()) {
      return;
    }
    const data: ConfirmDialogData = {
      title: 'Delete your account?',
      message: 'This permanently removes your account and every to-do you own. This cannot be undone.',
      confirmLabel: 'Delete account',
      cancelLabel: 'Cancel',
      destructive: true
    };
    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.submitting.set(true);
        this.accountService.deleteMyAccount().subscribe({
          next: () => {
            this.submitting.set(false);
            this.deleted.emit();
          },
          error: () => {
            this.submitting.set(false);
          }
        });
      });
  }
}
