import { Page, Locator, expect } from '@playwright/test';

export class TodosListPage {
  readonly appShell: Locator;
  readonly headerDateLabel: Locator;
  readonly countSummary: Locator;
  readonly filterAll: Locator;
  readonly filterIncomplete: Locator;
  readonly filterComplete: Locator;
  readonly incompleteItems: Locator;
  readonly completeItems: Locator;
  readonly emptyState: Locator;
  readonly addFab: Locator;
  readonly navRail: Locator;
  readonly navBar: Locator;

  constructor(private readonly page: Page) {
    this.appShell = page.getByTestId('app-shell');
    this.headerDateLabel = page.getByTestId('header-date-label');
    this.countSummary = page.getByTestId('count-summary');
    this.filterAll = page.getByTestId('filter-all');
    this.filterIncomplete = page.getByTestId('filter-incomplete');
    this.filterComplete = page.getByTestId('filter-complete');
    this.incompleteItems = page.getByTestId('incomplete-item');
    this.completeItems = page.getByTestId('complete-item');
    this.emptyState = page.getByTestId('todos-empty-state');
    this.addFab = page.getByTestId('add-todo-fab');
    this.navRail = page.getByTestId('nav-rail');
    this.navBar = page.getByTestId('nav-bar');
  }

  async expectVisible(): Promise<void> {
    await expect(this.page).toHaveURL(/\/todos$/);
    await expect(this.appShell).toBeVisible();
  }

  async toggleByLabel(title: string): Promise<void> {
    const item = this.page.locator(`[data-testid="incomplete-item"], [data-testid="complete-item"]`).filter({ hasText: title });
    await item.locator('[data-testid="todo-checkbox"]').click();
  }
}
