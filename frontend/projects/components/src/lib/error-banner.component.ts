import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'tb-error-banner',
  standalone: true,
  imports: [],
  templateUrl: './error-banner.component.html',
  styleUrl: './error-banner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorBannerComponent {
  @Input() message: string | null = null;
  @Input() testId: string | null = null;
}
