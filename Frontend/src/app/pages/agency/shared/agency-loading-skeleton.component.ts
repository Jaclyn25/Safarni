import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-agency-loading-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="animate-pulse space-y-4">
      @if (type === 'cards') {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          @for (i of [1, 2, 3, 4]; track i) {
            <div class="h-28 bg-gray-100 rounded-2xl"></div>
          }
        </div>
      } @else if (type === 'table') {
        <div class="bg-white rounded-2xl border border-gray-100 p-4 space-y-3">
          <div class="h-8 bg-gray-100 rounded-lg w-1/4"></div>
          @for (i of [1, 2, 3, 4, 5]; track i) {
            <div class="h-12 bg-gray-50 rounded-xl"></div>
          }
        </div>
      } @else {
        <div class="bg-white rounded-2xl border border-gray-100 p-6 space-y-4">
          <div class="h-6 bg-gray-100 rounded w-1/3"></div>
          <div class="h-4 bg-gray-50 rounded w-2/3"></div>
          <div class="h-32 bg-gray-50 rounded-xl"></div>
        </div>
      }
    </div>
  `
})
export class AgencyLoadingSkeletonComponent {
  @Input() type: 'cards' | 'table' | 'content' = 'content';
}
