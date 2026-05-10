import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AppShellComponent, AppShellNavItem, EmptyStateComponent } from 'components';

@Component({
  selector: 'tb-error-page',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, RouterLink, AppShellComponent, EmptyStateComponent],
  templateUrl: './error-page.component.html',
  styleUrl: './error-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorPageComponent {
  protected readonly navItems: readonly AppShellNavItem[] = [
    { label: 'To-dos', icon: 'checklist', route: '/todos' },
    { label: 'Profile', icon: 'person', route: '/profile' }
  ];

  protected readonly title: string;
  protected readonly supportingText: string;

  constructor(route: ActivatedRoute) {
    const reason = route.snapshot.queryParamMap.get('reason');
    if (reason === 'not_found') {
      this.title = 'Page not found';
      this.supportingText = 'The page you tried to open doesn’t exist.';
    } else {
      this.title = 'Something went wrong';
      this.supportingText = 'We hit an unexpected error. Try again, or head back to your to-dos.';
    }
  }

  protected reload(): void {
    if (typeof window !== 'undefined') {
      window.location.reload();
    }
  }
}
