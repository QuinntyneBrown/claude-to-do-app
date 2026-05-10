import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { ACCOUNT_SERVICE, AUTH_SERVICE, IAccountService, IAuthService, MyProfile } from 'api';
import { AppShellComponent, AppShellNavItem } from 'components';
import {
  ChangePasswordFormComponent,
  DeleteAccountSectionComponent,
  DisplayNameEditComponent,
  EmailChangeBannerComponent,
  EmailChangeFormComponent,
  ProfileSummaryComponent
} from 'domain';
import { AuthStateService } from '../auth/auth-state.service';

@Component({
  selector: 'tb-profile-page',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    AppShellComponent,
    ProfileSummaryComponent,
    DisplayNameEditComponent,
    ChangePasswordFormComponent,
    EmailChangeFormComponent,
    EmailChangeBannerComponent,
    DeleteAccountSectionComponent
  ],
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfilePageComponent implements OnInit {
  protected readonly navItems: readonly AppShellNavItem[] = [
    { label: 'To-dos', icon: 'checklist', route: '/todos' },
    { label: 'Profile', icon: 'person', route: '/profile' }
  ];

  protected readonly profile = signal<MyProfile | null>(null);

  constructor(
    @Inject(ACCOUNT_SERVICE) private readonly accountService: IAccountService,
    @Inject(AUTH_SERVICE) private readonly authService: IAuthService,
    private readonly authState: AuthStateService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.accountService.getMyProfile().subscribe({
      next: profile => this.profile.set(profile)
    });
  }

  protected onProfileChanged(profile: MyProfile): void {
    this.profile.set(profile);
  }

  protected signOut(): void {
    this.authService.signOut().subscribe({
      next: () => this.completeSignOut(),
      error: () => this.completeSignOut()
    });
  }

  protected onAccountDeleted(): void {
    this.completeSignOut();
  }

  private completeSignOut(): void {
    this.authState.signOut();
    void this.router.navigateByUrl('/sign-in');
  }
}
