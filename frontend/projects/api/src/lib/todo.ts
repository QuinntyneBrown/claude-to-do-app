export type TodoStatus = 'Incomplete' | 'Complete';

export interface Todo {
  id: string;
  title: string;
  status: TodoStatus;
  createdAt: string;
  completedAt: string | null;
}
