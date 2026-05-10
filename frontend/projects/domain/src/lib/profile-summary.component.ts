import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MyProfile } from 'api';

@Component({
  selector: 'tb-profile-summary',
  standalone: true,
  imports: [],
  templateUrl: './profile-summary.component.html',
  styleUrl: './profile-summary.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfileSummaryComponent {
  @Input({ required: true }) profile!: MyProfile;
}
