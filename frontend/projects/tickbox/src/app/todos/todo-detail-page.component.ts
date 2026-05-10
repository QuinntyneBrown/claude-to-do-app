import { ChangeDetectionStrategy, Component, Inject, OnInit, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { ITodosService, TODOS_SERVICE, TodoDetail } from 'api';
import { AppShellComponent, AppShellNavItem, ConfirmDialogComponent, ConfirmDialogData } from 'components';
import { TodoActivityListComponent, TodoEditFormComponent } from 'domain';

@Component({
  selector: 'tb-todo-detail-page',
  standalone: true,
  imports: [AppShellComponent, TodoEditFormComponent, TodoActivityListComponent],
  templateUrl: './todo-detail-page.component.html',
  styleUrl: './todo-detail-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodoDetailPageComponent implements OnInit {
  protected readonly navItems: readonly AppShellNavItem[] = [
    { label: 'To-dos', icon: 'checklist', route: '/todos' },
    { label: 'Profile', icon: 'person', route: '/profile' }
  ];

  protected readonly todo = signal<TodoDetail | null>(null);
  protected readonly mode = signal<'create' | 'edit'>('create');

  constructor(
    @Inject(TODOS_SERVICE) private readonly todosService: ITodosService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly dialog: MatDialog
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id === null || id === 'new') {
      this.mode.set('create');
      return;
    }
    this.mode.set('edit');
    this.todosService.getById(id).subscribe({
      next: detail => this.todo.set(detail),
      error: () => void this.router.navigateByUrl('/todos')
    });
  }

  protected onSaved(): void {
    void this.router.navigateByUrl('/todos');
  }

  protected onStatusChanged(detail: TodoDetail): void {
    this.todo.set(detail);
  }

  protected onDeleteRequested(): void {
    const detail = this.todo();
    if (!detail) {
      return;
    }
    const data: ConfirmDialogData = {
      title: 'Delete this to-do?',
      message: 'This cannot be undone.',
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      destructive: true
    };
    this.dialog.open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.todosService.delete(detail.id).subscribe({
          next: () => void this.router.navigateByUrl('/todos'),
          error: () => void this.router.navigateByUrl('/todos')
        });
      });
  }
}
