import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SignInResponse } from 'api';
import { BrandIconComponent } from 'components';
import { PasswordResetCompleteFormComponent } from 'domain';
import { AuthStateService } from '../auth/auth-state.service';

@Component({
  selector: 'tb-complete-password-reset-page',
  standalone: true,
  imports: [BrandIconComponent, PasswordResetCompleteFormComponent],
  templateUrl: './complete-password-reset-page.component.html',
  styleUrl: './password-reset-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompletePasswordResetPageComponent implements OnInit {
  protected readonly token = signal('');

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly authState: AuthStateService
  ) {}

  ngOnInit(): void {
    this.token.set(this.route.snapshot.queryParamMap.get('token') ?? '');
  }

  protected onCompleted(response: SignInResponse): void {
    this.authState.setAccessToken(response.accessToken);
    void this.router.navigateByUrl('/todos');
  }
}
