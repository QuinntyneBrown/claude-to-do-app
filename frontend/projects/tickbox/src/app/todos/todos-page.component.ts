import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router } from '@angular/router';
import { BrandIconComponent } from 'components';
import { TodosListComponent } from 'domain';
import { AuthStateService } from '../auth/auth-state.service';

@Component({
  selector: 'tb-todos-page',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
    BrandIconComponent,
    TodosListComponent
  ],
  templateUrl: './todos-page.component.html',
  styleUrl: './todos-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodosPageComponent {
  constructor(
    private readonly authState: AuthStateService,
    private readonly router: Router
  ) {}

  protected signOut(): void {
    this.authState.signOut();
    void this.router.navigateByUrl('/sign-in');
  }
}
