import { Page, Locator, expect } from '@playwright/test';

export class TodosPage {
  readonly count: Locator;
  readonly newTodoInput: Locator;
  readonly addButton: Locator;
  readonly items: Locator;
  readonly emptyState: Locator;
  readonly signOutButton: Locator;

  constructor(private readonly page: Page) {
    this.count = page.getByTestId('todos-count');
    this.newTodoInput = page.getByTestId('new-todo-input');
    this.addButton = page.getByTestId('add-todo-button');
    this.items = page.getByTestId('todo-item');
    this.emptyState = page.getByTestId('todos-empty');
    this.signOutButton = page.getByTestId('sign-out-button');
  }

  async expectVisible(): Promise<void> {
    await expect(this.page).toHaveURL(/\/todos$/);
    await expect(this.count).toBeVisible();
  }

  async addTodo(title: string): Promise<void> {
    await this.newTodoInput.fill(title);
    await this.addButton.click();
  }

  async expectItemCount(n: number): Promise<void> {
    await expect(this.items).toHaveCount(n);
  }

  async expectItemTitle(index: number, title: string): Promise<void> {
    await expect(this.items.nth(index)).toContainText(title);
  }
}
