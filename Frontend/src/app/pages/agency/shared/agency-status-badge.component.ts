import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-agency-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [class]="'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold tracking-wide ' + badgeClasses">
      <span class="w-1.5 h-1.5 rounded-full" [class]="dotClasses"></span>
      {{ status }}
    </span>
  `
})
export class AgencyStatusBadgeComponent {
  @Input({ required: true }) status!: string;

  get normalizedStatus(): string {
    return (this.status || '').toLowerCase().trim();
  }

  get badgeClasses(): string {
    switch (this.normalizedStatus) {
      case 'verified':
      case 'published':
      case 'approved':
      case 'accepted':
        return 'bg-emerald-50 text-emerald-700 border border-emerald-200/60';
      case 'pending':
      case 'pending review':
      case 'draft':
      case 'needs review':
      case 'under review':
        return 'bg-amber-50 text-amber-700 border border-amber-200/60';
      case 'rejected':
      case 'inactive':
      case 'suspended':
      case 'changes requested':
        return 'bg-rose-50 text-rose-700 border border-rose-200/60';
      default:
        return 'bg-gray-100 text-gray-700 border border-gray-200';
    }
  }

  get dotClasses(): string {
    switch (this.normalizedStatus) {
      case 'verified':
      case 'published':
      case 'approved':
      case 'accepted':
        return 'bg-emerald-500';
      case 'pending':
      case 'pending review':
      case 'draft':
      case 'needs review':
      case 'under review':
        return 'bg-amber-500';
      case 'rejected':
      case 'inactive':
      case 'suspended':
      case 'changes requested':
        return 'bg-rose-500';
      default:
        return 'bg-gray-400';
    }
  }
}
