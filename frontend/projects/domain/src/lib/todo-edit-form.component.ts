import { ChangeDetectionStrategy, Component, EventEmitter, Inject, Input, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ITodosService, TODOS_SERVICE, TodoDetail, TodoStatus } from 'api';
import { ErrorBannerComponent } from 'components';

@Component({
  selector: 'tb-todo-edit-form',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    ErrorBannerComponent
  ],
  templateUrl: './todo-edit-form.component.html',
  styleUrl: './todo-edit-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodoEditFormComponent implements OnInit {
  @Input() existing: TodoDetail | null = null;
  @Output() readonly saved = new EventEmitter<void>();
  @Output() readonly deleted = new EventEmitter<void>();
  @Output() readonly statusChanged = new EventEmitter<TodoDetail>();

  protected title = '';
  protected notes = '';
  protected dueDate = '';
  protected readonly status = signal<TodoStatus>('Incomplete');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor(@Inject(TODOS_SERVICE) private readonly todosService: ITodosService) {}

  ngOnInit(): void {
    if (this.existing) {
      this.title = this.existing.title;
      this.notes = this.existing.notes ?? '';
      this.dueDate = this.existing.dueDate ?? '';
      this.status.set(this.existing.status);
    }
  }

  protected get isEditing(): boolean {
    return this.existing !== null;
  }

  protected save(): void {
    if (this.submitting() || this.title.trim() === '') {
      this.error.set('Title is required.');
      return;
    }
    this.submitting.set(true);
    this.error.set(null);

    const payload = {
      title: this.title.trim(),
      notes: this.notes.trim() === '' ? null : this.notes.trim(),
      dueDate: this.dueDate === '' ? null : this.dueDate
    };

    const op$ = this.existing
      ? this.todosService.update(this.existing.id, payload)
      : this.todosService.create(payload);

    op$.subscribe({
      next: () => {
        this.submitting.set(false);
        this.saved.emit();
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Could not save. Check your input and try again.');
      }
    });
  }

  protected setStatus(next: TodoStatus): void {
    if (!this.existing || this.status() === next) {
      this.status.set(next);
      return;
    }
    this.todosService.toggleStatus(this.existing.id, { status: next }).subscribe({
      next: detail => {
        this.status.set(detail.status);
        this.statusChanged.emit(detail);
      },
      error: () => this.error.set('Could not update status.')
    });
  }

  protected requestDelete(): void {
    this.deleted.emit();
  }
}
