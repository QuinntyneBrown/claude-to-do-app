import { ChangeDetectionStrategy, Component, computed, Inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ITodosService, TODOS_SERVICE, Todo, TodoStatus } from 'api';
import { LoadingBarComponent, EmptyStateComponent } from 'components';
import { catchError, finalize, of } from 'rxjs';
import { TodoListItemComponent } from './todo-list-item.component';
import { TodoFilter, TodoFilterChipsComponent } from './todo-filter-chips.component';

@Component({
  selector: 'tb-todos-list',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    LoadingBarComponent,
    EmptyStateComponent,
    TodoListItemComponent,
    TodoFilterChipsComponent
  ],
  templateUrl: './todos-list.component.html',
  styleUrl: './todos-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodosListComponent implements OnInit {
  protected readonly todos = signal<Todo[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly filter = signal<TodoFilter>('all');

  protected readonly headerDateLabel = (() => {
    const today = new Date();
    return `Today, ${today.getDate()} ${today.toLocaleDateString(undefined, { month: 'long' })}`;
  })();

  protected readonly incomplete = computed(() => this.todos().filter(t => t.status === 'Incomplete'));
  protected readonly complete = computed(() => this.todos().filter(t => t.status === 'Complete'));
  protected readonly visibleIncomplete = computed(() => this.filter() === 'complete' ? [] : this.incomplete());
  protected readonly visibleComplete = computed(() => this.filter() === 'incomplete' ? [] : this.complete());
  protected readonly countSummary = computed(() => `${this.complete().length} of ${this.todos().length} complete`);

  constructor(@Inject(TODOS_SERVICE) private readonly todosService: ITodosService) {}

  ngOnInit(): void {
    this.refresh();
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.todosService
      .list()
      .pipe(
        catchError(() => {
          this.error.set('We couldn’t reach the server. Check your connection and try again.');
          return of<Todo[]>([]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe(items => this.todos.set(items));
  }

  protected onFilterChanged(filter: TodoFilter): void {
    this.filter.set(filter);
  }

  protected onToggle(todo: Todo, status: TodoStatus): void {
    this.todosService.toggleStatus(todo.id, { status }).subscribe({
      next: detail => {
        const projected: Todo = {
          id: detail.id,
          title: detail.title,
          notes: detail.notes,
          dueDate: detail.dueDate,
          status: detail.status,
          createdAt: detail.createdAt,
          completedAt: detail.completedAt
        };
        this.todos.update(items => items.map(t => t.id === projected.id ? projected : t));
      },
      error: () => this.refresh()
    });
  }
}
