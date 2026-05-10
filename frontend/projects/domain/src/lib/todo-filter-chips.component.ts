import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';

export type TodoFilter = 'all' | 'incomplete' | 'complete';

@Component({
  selector: 'tb-todo-filter-chips',
  standalone: true,
  imports: [MatChipsModule],
  templateUrl: './todo-filter-chips.component.html',
  styleUrl: './todo-filter-chips.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodoFilterChipsComponent {
  @Input() filter: TodoFilter = 'all';
  @Input() totalCount = 0;
  @Input() incompleteCount = 0;
  @Input() completeCount = 0;
  @Output() readonly filterChanged = new EventEmitter<TodoFilter>();

  protected select(filter: TodoFilter): void {
    if (this.filter !== filter) {
      this.filterChanged.emit(filter);
    }
  }
}
