import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { AppShellComponent, AppShellNavItem } from 'components';
import { TodosListComponent } from 'domain';

@Component({
  selector: 'tb-todos-page',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    RouterLink,
    AppShellComponent,
    TodosListComponent
  ],
  templateUrl: './todos-page.component.html',
  styleUrl: './todos-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodosPageComponent {
  protected readonly navItems: readonly AppShellNavItem[] = [
    { label: 'To-dos', icon: 'checklist', route: '/todos' },
    { label: 'Profile', icon: 'person', route: '/profile' }
  ];
}
