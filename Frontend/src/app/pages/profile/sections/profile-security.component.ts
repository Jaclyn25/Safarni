import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-profile-security',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-security.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileSecurityComponent {
  readonly confirmDelete = signal(false);

  toggleConfirm(): void {
    this.confirmDelete.set(!this.confirmDelete());
  }
}
