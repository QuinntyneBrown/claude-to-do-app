import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BrandIconComponent } from 'components';
import { PasswordResetRequestFormComponent } from 'domain';

@Component({
  selector: 'tb-request-password-reset-page',
  standalone: true,
  imports: [RouterLink, BrandIconComponent, PasswordResetRequestFormComponent],
  templateUrl: './request-password-reset-page.component.html',
  styleUrl: './password-reset-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RequestPasswordResetPageComponent {}
