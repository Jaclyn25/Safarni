import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { PasswordRecoveryShellComponent } from '../../shared/password-recovery-shell/password-recovery-shell.component';

@Component({
  selector: 'app-verification-email-sent',
  standalone: true,
  imports: [RouterLink, PasswordRecoveryShellComponent],
  templateUrl: './verification-email-sent.component.html'
})
export class VerificationEmailSentComponent {
  private readonly route = inject(ActivatedRoute);

  readonly visualSrc = 'assets/password-recovery/check-email-visual.jpg';
  readonly emailArtSrc = 'assets/password-recovery/email-sent-icon.png';

  readonly email =
    this.route.snapshot.queryParamMap.get('email')?.trim().toLowerCase() ??
    sessionStorage.getItem('email_verification_email') ??
    '';
}
