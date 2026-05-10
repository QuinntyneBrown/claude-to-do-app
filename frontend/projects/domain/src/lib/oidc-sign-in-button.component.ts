import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'tb-oidc-sign-in-button',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './oidc-sign-in-button.component.html',
  styleUrl: './oidc-sign-in-button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OidcSignInButtonComponent {
  protected get enabled(): boolean {
    return typeof window !== 'undefined'
      && (window as unknown as { __TICKBOX_OIDC_ENABLED__?: boolean }).__TICKBOX_OIDC_ENABLED__ === true;
  }

  protected beginOidc(): void {
    // Wired in F-004; the button only renders the call-to-action here.
    void this.enabled;
  }
}
