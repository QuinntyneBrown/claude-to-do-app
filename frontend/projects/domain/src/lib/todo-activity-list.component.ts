import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TodoActivity } from 'api';

@Component({
  selector: 'tb-todo-activity-list',
  standalone: true,
  imports: [],
  templateUrl: './todo-activity-list.component.html',
  styleUrl: './todo-activity-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodoActivityListComponent {
  @Input() activity: readonly TodoActivity[] = [];

  protected label(kind: TodoActivity['kind']): string {
    return kind === 'Created' ? 'Created' : 'Marked complete';
  }

  protected formatTime(occurredAt: string): string {
    const dt = new Date(occurredAt);
    const date = dt.toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
    const time = dt.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', hour12: false });
    return `${date}, ${time}`;
  }
}
