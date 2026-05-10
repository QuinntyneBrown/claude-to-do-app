import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ACCOUNT_SERVICE, IAccountService } from 'api';
import { ErrorBannerComponent } from 'components';

@Component({
  selector: 'tb-confirm-email-change-page',
  standalone: true,
  imports: [MatButtonModule, RouterLink, ErrorBannerComponent],
  templateUrl: './confirm-email-change-page.component.html',
  styleUrl: './confirm-email-change-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmEmailChangePageComponent implements OnInit {
  protected readonly status = signal<'pending' | 'success' | 'error'>('pending');
  protected readonly error = signal<string | null>(null);

  constructor(
    @Inject(ACCOUNT_SERVICE) private readonly accountService: IAccountService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (token === '') {
      this.status.set('error');
      this.error.set('Confirmation link is missing its token.');
      return;
    }
    this.accountService.confirmEmailChange({ token }).subscribe({
      next: () => {
        this.status.set('success');
        void this.router.navigateByUrl('/profile');
      },
      error: () => {
        this.status.set('error');
        this.error.set('This confirmation link is invalid or expired.');
      }
    });
  }
}
