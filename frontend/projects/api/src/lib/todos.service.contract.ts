import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { Todo, TodoStatus } from './todo';
import { TodoDetail, UpdateTodoRequest } from './todo-detail';

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
  getById(id: string): Observable<TodoDetail>;
  create(request: CreateTodoRequest): Observable<Todo>;
  update(id: string, request: UpdateTodoRequest): Observable<TodoDetail>;
  toggleStatus(id: string, request: ToggleTodoStatusRequest): Observable<TodoDetail>;
  delete(id: string): Observable<void>;
}

export const TODOS_SERVICE = new InjectionToken<ITodosService>('Tickbox.ITodosService');
