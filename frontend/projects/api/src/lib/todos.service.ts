import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG, ApiConfig } from './api-config';
import { Todo } from './todo';
import { CreateTodoRequest, ITodosService, ToggleTodoStatusRequest } from './todos.service.contract';

@Injectable()
export class TodosService implements ITodosService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_CONFIG) private readonly config: ApiConfig
  ) {}

  list(): Observable<Todo[]> {
    return this.http.get<Todo[]>(`${this.config.baseUrl}/api/todos`);
  }

  create(request: CreateTodoRequest): Observable<Todo> {
    return this.http.post<Todo>(`${this.config.baseUrl}/api/todos`, request);
  }

  toggleStatus(id: string, request: ToggleTodoStatusRequest): Observable<Todo> {
    return this.http.patch<Todo>(`${this.config.baseUrl}/api/todos/${id}/status`, request);
  }
}
