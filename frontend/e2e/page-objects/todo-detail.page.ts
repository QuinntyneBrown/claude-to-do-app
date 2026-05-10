import { Page, Locator, expect } from '@playwright/test';

export class TodoDetailPage {
  readonly titleInput: Locator;
  readonly notesInput: Locator;
  readonly statusIncomplete: Locator;
  readonly statusComplete: Locator;
  readonly saveButton: Locator;
  readonly deleteButton: Locator;
  readonly confirmDialog: Locator;
  readonly confirmDeleteButton: Locator;
  readonly cancelDeleteButton: Locator;
  readonly errorMessage: Locator;
  readonly activityItems: Locator;

  constructor(private readonly page: Page) {
    this.titleInput = page.getByTestId('todo-title-input');
    this.notesInput = page.getByTestId('todo-notes-input');
    this.statusIncomplete = page.getByTestId('status-chip-incomplete');
    this.statusComplete = page.getByTestId('status-chip-complete');
    this.saveButton = page.getByTestId('todo-save');
    this.deleteButton = page.getByTestId('todo-delete');
    this.confirmDialog = page.getByTestId('confirm-dialog');
    this.confirmDeleteButton = page.getByTestId('confirm-dialog-confirm');
    this.cancelDeleteButton = page.getByTestId('confirm-dialog-cancel');
    this.errorMessage = page.getByTestId('todo-edit-error');
    this.activityItems = page.getByTestId('todo-activity-item');
  }

  async gotoNew(): Promise<void> {
    await this.page.goto('/todos/new');
    await expect(this.saveButton).toBeVisible();
  }

  async gotoEdit(id: string): Promise<void> {
    await this.page.goto(`/todos/${id}`);
    await expect(this.saveButton).toBeVisible();
  }
}
