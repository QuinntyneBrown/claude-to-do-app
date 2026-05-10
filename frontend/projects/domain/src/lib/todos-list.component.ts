import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ITodosService, TODOS_SERVICE, Todo } from 'api';
import { catchError, finalize, of } from 'rxjs';

@Component({
  selector: 'tb-todos-list',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule
  ],
  templateUrl: './todos-list.component.html',
  styleUrl: './todos-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodosListComponent implements OnInit {
  protected readonly todos = signal<Todo[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected newTitle = '';

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
        catchError((err: unknown) => {
          this.error.set('Could not load to-dos. Sign in and try again.');
          console.error(err);
          return of<Todo[]>([]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe(items => this.todos.set(items));
  }

  protected addTodo(): void {
    const title = this.newTitle.trim();
    if (title === '') {
      return;
    }
    this.todosService
      .create({ title })
      .subscribe(created => {
        this.todos.update(items => [created, ...items]);
        this.newTitle = '';
      });
  }
}
