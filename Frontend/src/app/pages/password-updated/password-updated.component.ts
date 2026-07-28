import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PasswordRecoveryShellComponent } from '../../shared/password-recovery-shell/password-recovery-shell.component';

@Component({
  selector: 'app-password-updated',
  standalone: true,
  imports: [RouterLink, PasswordRecoveryShellComponent],
  templateUrl: './password-updated.component.html'
})
export class PasswordUpdatedComponent {
  readonly visualSrc = 'assets/password-recovery/password-updated-visual.jpg';
}
