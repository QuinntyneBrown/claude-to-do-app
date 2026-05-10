import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { Todo } from './todo';

export interface CreateTodoRequest {
  readonly title: string;
}

export interface ITodosService {
  list(): Observable<Todo[]>;
  create(request: CreateTodoRequest): Observable<Todo>;
}

export const TODOS_SERVICE = new InjectionToken<ITodosService>('Tickbox.ITodosService');
