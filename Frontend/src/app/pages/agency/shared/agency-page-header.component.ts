import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-agency-page-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
      <div>
        @if (eyebrow) {
          <span class="text-xs font-bold uppercase tracking-wider text-blue-600 mb-1 block">{{ eyebrow }}</span>
        }
        <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 tracking-tight font-display">{{ title }}</h1>
        @if (subtitle) {
          <p class="text-sm text-gray-500 mt-1 max-w-2xl">{{ subtitle }}</p>
        }
      </div>
      <div class="flex items-center gap-2 self-start sm:self-auto">
        <ng-content />
      </div>
    </div>
  `
})
export class AgencyPageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle?: string;
  @Input() eyebrow?: string;
}
