import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { Todo, TodoStatus } from 'api';

@Component({
  selector: 'tb-todo-list-item',
  standalone: true,
  imports: [MatCheckboxModule],
  templateUrl: './todo-list-item.component.html',
  styleUrl: './todo-list-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodoListItemComponent {
  @Input({ required: true }) todo!: Todo;
  @Output() readonly toggled = new EventEmitter<TodoStatus>();

  protected onCheckboxChange(): void {
    this.toggled.emit(this.todo.status === 'Complete' ? 'Incomplete' : 'Complete');
  }

  protected get dueLabel(): string | null {
    if (!this.todo.dueDate) return null;
    const due = new Date(this.todo.dueDate + 'T00:00:00');
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const oneDay = 24 * 60 * 60 * 1000;
    const diffDays = Math.round((due.getTime() - today.getTime()) / oneDay);
    if (diffDays === 0) return 'Due today';
    if (diffDays === 1) return 'Due tomorrow';
    return `Due ${due.toLocaleDateString(undefined, { day: 'numeric', month: 'short' })}`;
  }

  protected get testId(): string {
    return this.todo.status === 'Complete' ? 'complete-item' : 'incomplete-item';
  }
}
