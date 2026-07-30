import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-password-recovery-shell',
  standalone: true,
  templateUrl: './password-recovery-shell.component.html'
})
export class PasswordRecoveryShellComponent {
  @Input({ required: true }) visualSrc = '';
  @Input() visualAlt = '';
}
