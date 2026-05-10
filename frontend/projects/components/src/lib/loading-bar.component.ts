import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'tb-loading-bar',
  standalone: true,
  imports: [MatProgressBarModule],
  templateUrl: './loading-bar.component.html',
  styleUrl: './loading-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingBarComponent {
  @Input() loading = false;
}
