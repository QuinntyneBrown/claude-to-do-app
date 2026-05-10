export type TodoStatus = 'Incomplete' | 'Complete';

export interface Todo {
  id: string;
  title: string;
  notes: string | null;
  dueDate: string | null;
  status: TodoStatus;
  createdAt: string;
  completedAt: string | null;
}
