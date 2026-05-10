import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG, ApiConfig } from './api-config';
import { Todo } from './todo';
import { TodoDetail, UpdateTodoRequest } from './todo-detail';
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

  getById(id: string): Observable<TodoDetail> {
    return this.http.get<TodoDetail>(`${this.config.baseUrl}/api/todos/${id}`);
  }

  create(request: CreateTodoRequest): Observable<Todo> {
    return this.http.post<Todo>(`${this.config.baseUrl}/api/todos`, request);
  }

  update(id: string, request: UpdateTodoRequest): Observable<TodoDetail> {
    return this.http.put<TodoDetail>(`${this.config.baseUrl}/api/todos/${id}`, request);
  }

  toggleStatus(id: string, request: ToggleTodoStatusRequest): Observable<TodoDetail> {
    return this.http.patch<TodoDetail>(`${this.config.baseUrl}/api/todos/${id}/status`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.config.baseUrl}/api/todos/${id}`);
  }
}
