import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Input, Output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ACCOUNT_SERVICE, IAccountService, MyProfile } from 'api';

@Component({
  selector: 'tb-email-change-banner',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './email-change-banner.component.html',
  styleUrl: './email-change-banner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmailChangeBannerComponent {
  @Input({ required: true }) pendingEmail!: string;
  @Output() readonly cancelled = new EventEmitter<MyProfile>();

  protected readonly submitting = signal(false);

  constructor(@Inject(ACCOUNT_SERVICE) private readonly accountService: IAccountService) {}

  protected cancel(): void {
    if (this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.accountService.cancelEmailChange().subscribe({
      next: profile => {
        this.submitting.set(false);
        this.cancelled.emit(profile);
      },
      error: () => {
        this.submitting.set(false);
      }
    });
  }
}
