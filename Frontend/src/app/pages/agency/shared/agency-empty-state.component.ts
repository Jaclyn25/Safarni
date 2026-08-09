import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-agency-empty-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col items-center justify-center p-8 sm:p-12 text-center bg-white rounded-2xl border border-dashed border-gray-200">
      <div class="w-14 h-14 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mb-4">
        <span class="material-symbols-outlined text-3xl">{{ icon }}</span>
      </div>
      <h3 class="text-lg font-bold text-gray-900 font-display">{{ title }}</h3>
      <p class="text-sm text-gray-500 max-w-sm mt-1 mb-6 leading-relaxed">{{ description }}</p>
      <ng-content />
    </div>
  `
})
export class AgencyEmptyStateComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) description!: string;
  @Input() icon: string = 'inbox';
}
