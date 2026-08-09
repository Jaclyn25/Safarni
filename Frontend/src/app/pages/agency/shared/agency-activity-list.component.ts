import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ActivityItem {
  id: string;
  title: string;
  description: string;
  timestamp: string;
  type: 'booking' | 'package' | 'verification' | 'system';
  status?: string;
}

@Component({
  selector: 'app-agency-activity-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-2xl border border-gray-100 p-6 shadow-sm">
      <div class="flex items-center justify-between mb-4">
        <h3 class="font-bold text-gray-900 text-base font-display">Recent Activity</h3>
        <span class="text-xs text-gray-400 font-medium">Real-time updates</span>
      </div>
      @if (activities.length === 0) {
        <p class="text-sm text-gray-400 py-4 text-center">No recent activity to show.</p>
      } @else {
        <div class="space-y-4">
          @for (item of activities; track item.id) {
            <div class="flex items-start gap-3.5">
              <div [class]="'w-9 h-9 rounded-xl flex items-center justify-center shrink-0 mt-0.5 ' + getActivityBg(item.type)">
                <span class="material-symbols-outlined text-lg" [class]="getActivityColor(item.type)">
                  {{ getActivityIcon(item.type) }}
                </span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center justify-between gap-2">
                  <h4 class="text-sm font-semibold text-gray-900 truncate">{{ item.title }}</h4>
                  <span class="text-xs text-gray-400 shrink-0">{{ item.timestamp }}</span>
                </div>
                <p class="text-xs text-gray-500 mt-0.5 line-clamp-1">{{ item.description }}</p>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class AgencyActivityListComponent {
  @Input() activities: ActivityItem[] = [];

  getActivityIcon(type: string): string {
    switch (type) {
      case 'booking': return 'confirmation_number';
      case 'package': return 'luggage';
      case 'verification': return 'verified';
      default: return 'notifications';
    }
  }

  getActivityBg(type: string): string {
    switch (type) {
      case 'booking': return 'bg-blue-50';
      case 'package': return 'bg-purple-50';
      case 'verification': return 'bg-emerald-50';
      default: return 'bg-gray-100';
    }
  }

  getActivityColor(type: string): string {
    switch (type) {
      case 'booking': return 'text-blue-600';
      case 'package': return 'text-purple-600';
      case 'verification': return 'text-emerald-600';
      default: return 'text-gray-600';
    }
  }
}
