import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'tb-brand-icon',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './brand-icon.component.html',
  styleUrl: './brand-icon.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BrandIconComponent {
  @Input() label = 'Tickbox';
}
