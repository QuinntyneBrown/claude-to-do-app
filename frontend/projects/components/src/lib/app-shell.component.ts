import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AppShellNavItem } from './app-shell-nav-item';

@Component({
  selector: 'tb-app-shell',
  standalone: true,
  imports: [MatIconModule, MatToolbarModule, RouterLink, RouterLinkActive],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppShellComponent {
  @Input() title = 'Tickbox';
  @Input() navItems: readonly AppShellNavItem[] = [];
}
