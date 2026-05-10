import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'tb-empty-state',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  @Input() icon = 'inbox';
  @Input() title = '';
  @Input() supportingText: string | null = null;
  @Input() variant: 'default' | 'error' = 'default';
  @Input() testId: string | null = null;
}
