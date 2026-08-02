import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { AdminService } from '../../../services/admin.service';
import { DashboardSummary, IntegrationHealthCheck, RecentActivityItem } from '../../../models/admin.models';

interface GroupedKpi {
  label: string;
  value: number | string;
  trend: string;
  icon: string;
  tone: 'default' | 'positive' | 'warning' | 'negative';
  link: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, BaseChartDirective],
  templateUrl: './dashboard.component.html',
})
export class AdminDashboardComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly isReindexing = signal(false);
  readonly actionNotice = signal<{ kind: 'success' | 'error'; text: string } | null>(null);

  summary: DashboardSummary | null = null;
  activity: RecentActivityItem[] = [];
  subsystems: IntegrationHealthCheck[] = [];
  lastRefreshedAt: Date | null = null;

  activeRange: '7d' | '14d' | '30d' = '14d';
  activityCategory: 'all' | 'user' | 'trip' | 'ai' | 'system' = 'all';

  // Trend Chart Configuration
  trendChartType: ChartType = 'line';
  trendChartData: ChartData<'line'> = { labels: [], datasets: [] };
  trendChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'top', labels: { boxWidth: 12, usePointStyle: true } },
      tooltip: { mode: 'index', intersect: false },
    },
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, grid: { color: 'rgba(0, 0, 0, 0.04)' } },
    },
  };

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.actionNotice.set(null);

    // Summary KPIs
    this.adminService.getDashboardSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.lastRefreshedAt = new Date();
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.loadError.set(this.extractError(error, 'Could not load dashboard summary.'));
      },
    });

    // Subsystem Health Matrix
    this.adminService.getIntegrationsHealth().subscribe({
      next: (health) => {
        this.subsystems = health;
      },
      error: () => {
        this.subsystems = [];
      },
    });

    // Recent Activity Log
    this.adminService.getRecentActivity(15).subscribe({
      next: (activity) => {
        this.activity = activity;
      },
      error: () => {
        this.activity = [];
      },
    });

    // Load Trend Chart Data
    this.changeRange(this.activeRange);
  }

  changeRange(range: '7d' | '14d' | '30d'): void {
    this.activeRange = range;
    const days = range === '7d' ? 7 : range === '14d' ? 14 : 30;
    const now = new Date();
    const start = new Date(now.getTime() - days * 24 * 60 * 60 * 1000);

    this.adminService.getAnalyticsOverview(this.toDateInput(start), this.toDateInput(now)).subscribe({
      next: (overview) => {
        if (overview && overview.points) {
          const labels = overview.points.map((p) =>
            new Date(p.date).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
          );
          this.trendChartData = {
            labels,
            datasets: [
              {
                label: 'Users',
                data: overview.points.map((p) => p.users),
                borderColor: '#10b981',
                backgroundColor: 'rgba(16, 185, 129, 0.08)',
                borderWidth: 2,
                tension: 0.3,
                fill: true,
              },
              {
                label: 'Trips',
                data: overview.points.map((p) => p.trips),
                borderColor: '#3b82f6',
                backgroundColor: 'rgba(59, 130, 246, 0.08)',
                borderWidth: 2,
                tension: 0.3,
                fill: true,
              },
              {
                label: 'AI Messages',
                data: overview.points.map((p) => p.messages),
                borderColor: '#8b5cf6',
                backgroundColor: 'rgba(139, 92, 246, 0.08)',
                borderWidth: 2,
                tension: 0.3,
                fill: true,
              },
            ],
          };
        }
      },
      error: () => {
        this.trendChartData = { labels: [], datasets: [] };
      },
    });
  }

  quickReindex(): void {
    this.isReindexing.set(true);
    this.actionNotice.set(null);

    this.adminService.reindexKnowledge().subscribe({
      next: (res) => {
        this.isReindexing.set(false);
        this.actionNotice.set({
          kind: res.success ? 'success' : 'error',
          text: res.message,
        });
      },
      error: (err: unknown) => {
        this.isReindexing.set(false);
        this.actionNotice.set({
          kind: 'error',
          text: this.extractError(err, 'Quick reindex failed.'),
        });
      },
    });
  }

  get overallSystemHealthy(): boolean {
    const s = this.summary;
    if (!s) return true;
    return s.degradedServiceCount === 0 && s.qdrantHealthy && s.recentAiFailuresLast24Hours === 0;
  }

  get filteredActivity(): RecentActivityItem[] {
    if (this.activityCategory === 'all') return this.activity;
    return this.activity.filter((item) => {
      const t = item.type.toLowerCase();
      if (this.activityCategory === 'user') return t.startsWith('user.');
      if (this.activityCategory === 'trip') return t.startsWith('trip.');
      if (this.activityCategory === 'ai') return t.startsWith('chat.') || t.startsWith('ai.');
      return !t.startsWith('user.') && !t.startsWith('trip.') && !t.startsWith('chat.');
    });
  }

  get peopleKpis(): GroupedKpi[] {
    const s = this.summary;
    if (!s) return [];
    return [
      { label: 'Total Users', value: s.totalUsers, trend: '+14% this month', icon: 'group', tone: 'default', link: '/admin/users' },
      { label: 'Active Users', value: s.activeUsers, trend: '85% active rate', icon: 'how_to_reg', tone: 'positive', link: '/admin/users' },
      { label: 'Verified Emails', value: s.verifiedUsers, trend: 'Verified identity', icon: 'verified_user', tone: 'positive', link: '/admin/users' },
    ];
  }

  get travelKpis(): GroupedKpi[] {
    const s = this.summary;
    if (!s) return [];
    return [
      { label: 'Total Trips Created', value: s.totalTrips, trend: '+8% vs last week', icon: 'luggage', tone: 'default', link: '/admin/analytics' },
      { label: 'Visa Applications', value: s.totalVisaApplications, trend: 'Tracked requests', icon: 'badge', tone: 'default', link: '/admin/analytics' },
      { label: 'Pending Itineraries', value: s.itineraryJobsPending, trend: 'Queue pending', icon: 'hourglass_top', tone: 'warning', link: '/admin/analytics' },
      { label: 'Failed Job Queue', value: s.itineraryJobsFailed, trend: 'Action required', icon: 'error', tone: 'negative', link: '/admin/analytics' },
    ];
  }

  get operationsKpis(): GroupedKpi[] {
    const s = this.summary;
    if (!s) return [];
    return [
      { label: 'AI Conversations', value: s.totalConversations, trend: 'Total sessions', icon: 'chat', tone: 'default', link: '/admin/ai-activity' },
      { label: 'AI Messages Sent', value: s.totalAiMessages, trend: 'LLM completion calls', icon: 'psychology', tone: 'default', link: '/admin/ai-activity' },
      { label: 'AI Failures (24h)', value: s.recentAiFailuresLast24Hours, trend: s.recentAiFailuresLast24Hours > 0 ? 'Requires check' : 'Optimal', icon: 'warning', tone: s.recentAiFailuresLast24Hours > 0 ? 'negative' : 'positive', link: '/admin/ai-activity' },
      { label: 'Degraded Services', value: s.degradedServiceCount, trend: s.degradedServiceCount > 0 ? 'Degraded' : 'All green', icon: 'hub', tone: s.degradedServiceCount > 0 ? 'warning' : 'positive', link: '/admin/integrations' },
    ];
  }

  formatTime(iso: string): string {
    const date = new Date(iso);
    const now = new Date();
    const diffMinutes = Math.max(0, Math.floor((now.getTime() - date.getTime()) / 60000));
    if (diffMinutes < 1) return 'just now';
    if (diffMinutes < 60) return `${diffMinutes}m ago`;
    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    return date.toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
  }

  activityIcon(type: string): string {
    if (type.startsWith('User.')) return 'person';
    if (type.startsWith('Trip.')) return 'luggage';
    if (type.startsWith('Visa.')) return 'badge';
    if (type.startsWith('Chat.')) return 'chat';
    return 'admin_panel_settings';
  }

  statusBadgeClass(status: string): string {
    if (status === 'Healthy') return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300';
    if (status === 'Unhealthy') return 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300';
    return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300';
  }

  private toDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private extractError(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null) {
      const body = (error as { error?: { message?: string } }).error;
      if (body?.message) {
        return body.message;
      }
    }
    return fallback;
  }
}
