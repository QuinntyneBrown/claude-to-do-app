import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { Todo, TodoStatus } from './todo';

export interface CreateTodoRequest {
  readonly title: string;
  readonly notes?: string | null;
  readonly dueDate?: string | null;
}

export interface ToggleTodoStatusRequest {
  readonly status: TodoStatus;
}

export interface ITodosService {
  list(): Observable<Todo[]>;
  create(request: CreateTodoRequest): Observable<Todo>;
  toggleStatus(id: string, request: ToggleTodoStatusRequest): Observable<Todo>;
}

export const TODOS_SERVICE = new InjectionToken<ITodosService>('Tickbox.ITodosService');
