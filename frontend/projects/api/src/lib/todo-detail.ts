import { TodoStatus } from './todo';

export type TodoActivityKind = 'Created' | 'MarkedComplete';

export interface TodoActivity {
  readonly kind: TodoActivityKind;
  readonly occurredAt: string;
}

export interface TodoDetail {
  readonly id: string;
  readonly title: string;
  readonly notes: string | null;
  readonly dueDate: string | null;
  readonly status: TodoStatus;
  readonly createdAt: string;
  readonly completedAt: string | null;
  readonly activity: TodoActivity[];
}

export interface UpdateTodoRequest {
  readonly title: string;
  readonly notes?: string | null;
  readonly dueDate?: string | null;
}
