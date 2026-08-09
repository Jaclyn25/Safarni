import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-agency-kpi-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-2xl border border-gray-100 p-5 shadow-sm hover:shadow-md transition-all duration-200">
      <div class="flex items-center justify-between mb-3">
        <span class="text-xs font-semibold uppercase tracking-wider text-gray-500">{{ title }}</span>
        <div [class]="'w-10 h-10 rounded-xl flex items-center justify-center ' + iconBgClass">
          <span class="material-symbols-outlined text-xl" [class]="iconColorClass">{{ icon }}</span>
        </div>
      </div>
      <div class="flex items-baseline justify-between">
        <span class="text-2xl font-bold text-gray-900 tracking-tight">{{ value }}</span>
        @if (trend) {
          <span [class]="'text-xs font-medium px-2 py-0.5 rounded-full flex items-center gap-1 ' + (trendUp ? 'bg-emerald-50 text-emerald-700' : 'bg-rose-50 text-rose-700')">
            <span class="material-symbols-outlined text-xs">{{ trendUp ? 'trending_up' : 'trending_down' }}</span>
            {{ trend }}
          </span>
        }
      </div>
      @if (subtitle) {
        <p class="text-xs text-gray-400 mt-1.5">{{ subtitle }}</p>
      }
    </div>
  `
})
export class AgencyKpiCardComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) value!: string | number;
  @Input() icon: string = 'analytics';
  @Input() subtitle?: string;
  @Input() trend?: string;
  @Input() trendUp: boolean = true;
  @Input() variant: 'blue' | 'emerald' | 'amber' | 'rose' | 'purple' | 'slate' = 'blue';

  get iconBgClass(): string {
    switch (this.variant) {
      case 'emerald': return 'bg-emerald-50';
      case 'amber': return 'bg-amber-50';
      case 'rose': return 'bg-rose-50';
      case 'purple': return 'bg-purple-50';
      case 'slate': return 'bg-gray-100';
      case 'blue':
      default: return 'bg-blue-50';
    }
  }

  get iconColorClass(): string {
    switch (this.variant) {
      case 'emerald': return 'text-emerald-600';
      case 'amber': return 'text-amber-600';
      case 'rose': return 'text-rose-600';
      case 'purple': return 'text-purple-600';
      case 'slate': return 'text-gray-600';
      case 'blue':
      default: return 'text-blue-600';
    }
  }
}
