import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { AdminService } from '../../../services/admin.service';
import {
  AdminConversation,
  AdminConversationMessage,
  AiActivityItem,
  PagedResult,
} from '../../../models/admin.models';

@Component({
  selector: 'app-admin-ai-activity',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective],
  providers: [provideCharts(withDefaultRegisterables())],
  templateUrl: './ai-activity.component.html',
})
export class AdminAiActivityComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly trackByIndex = (index: number): number => index;
  readonly loadError = signal<string | null>(null);
  readonly notice = signal<{ kind: 'success' | 'error'; text: string } | null>(null);
  readonly conversationOpen = signal(false);
  readonly Math = Math;

  providerFilter = '';
  statusFilter = '';

  result: PagedResult<AiActivityItem> | null = null;
  conversation: AdminConversation | null = null;
  readonly conversationLoading = signal(false);

  // Computed summary metrics
  totalRequests = 0;
  failedCount = 0;
  avgLatency = 0;
  p95Latency = 0;
  rateLimitCount = 0;
  totalTokens = 0;

  // Chart configs
  latencyChartType: ChartType = 'line';
  latencyChartData: ChartData<'line'> = { labels: [], datasets: [] };
  latencyChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: { mode: 'index', intersect: false },
    },
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, grid: { color: 'rgba(0, 0, 0, 0.04)' } },
    },
  };

  statusChartType: ChartType = 'doughnut';
  statusChartData: ChartData<'doughnut'> = { labels: [], datasets: [] };
  statusChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom', labels: { boxWidth: 12 } },
    },
  };

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  applyProviderPill(provider: string): void {
    this.providerFilter = provider;
    this.load(1);
  }

  load(page = 1): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.adminService
      .getAiActivity({
        provider: this.providerFilter || undefined,
        status: this.statusFilter || undefined,
        page,
        pageSize: 50,
      })
      .subscribe({
        next: (result) => {
          this.result = result;
          this.computeMetrics(result.items);
          this.updateCharts(result.items);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.isLoading.set(false);
          this.loadError.set(this.extractError(error, 'Could not load AI activity metrics.'));
        },
      });
  }

  private computeMetrics(items: AiActivityItem[]): void {
    this.totalRequests = items.length;
    this.failedCount = items.filter((i) => i.status !== 'Success').length;
    this.rateLimitCount = items.filter((i) => i.errorCode === 'RateLimit' || i.errorCode === '429').length;
    this.totalTokens = items.reduce((acc, curr) => acc + (curr.promptTokens || 0) + (curr.completionTokens || 0), 0);

    if (items.length > 0) {
      const latencies = items.map((i) => i.latencyMs).sort((a, b) => a - b);
      this.avgLatency = Math.round(latencies.reduce((acc, curr) => acc + curr, 0) / latencies.length);
      const p95Index = Math.min(latencies.length - 1, Math.floor(latencies.length * 0.95));
      this.p95Latency = latencies[p95Index] || 0;
    } else {
      this.avgLatency = 0;
      this.p95Latency = 0;
    }
  }

  private updateCharts(items: AiActivityItem[]): void {
    if (!items || items.length === 0) {
      this.latencyChartData = { labels: [], datasets: [] };
      this.statusChartData = { labels: [], datasets: [] };
      return;
    }

    // Latency trend
    const recentItems = [...items].reverse().slice(-20);
    const labels = recentItems.map((i) => this.formatTime(i.createdAt));
    this.latencyChartData = {
      labels,
      datasets: [
        {
          label: 'Latency (ms)',
          data: recentItems.map((i) => i.latencyMs),
          borderColor: '#8b5cf6',
          backgroundColor: 'rgba(139, 92, 246, 0.08)',
          borderWidth: 2,
          fill: true,
          tension: 0.3,
        },
      ],
    };

    // Status distribution
    const statusCounts: Record<string, number> = {};
    for (const item of items) {
      statusCounts[item.status] = (statusCounts[item.status] || 0) + 1;
    }

    this.statusChartData = {
      labels: Object.keys(statusCounts),
      datasets: [
        {
          data: Object.values(statusCounts),
          backgroundColor: ['#10b981', '#ef4444', '#f59e0b', '#6366f1'],
        },
      ],
    };
  }

  openConversation(item: AiActivityItem): void {
    if (!item.conversationId) return;
    this.conversationLoading.set(true);
    this.conversationOpen.set(true);
    this.notice.set(null);

    this.adminService.getAiConversation(item.conversationId).subscribe({
      next: (conversation) => {
        this.conversation = conversation;
        this.conversationLoading.set(false);
      },
      error: (error: unknown) => {
        this.conversationLoading.set(false);
        this.conversation = null;
        this.notice.set({
          kind: 'error',
          text: this.extractError(error, 'Could not load conversation transcript.'),
        });
      },
    });
  }

  closeConversation(): void {
    this.conversationOpen.set(false);
    this.conversation = null;
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString(undefined, {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString(undefined, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  statusBadgeClass(status: string): string {
    if (status === 'Success') {
      return 'bg-emerald-50 text-emerald-700 border-emerald-200';
    }
    return 'bg-red-50 text-red-700 border-red-200';
  }

  latencyBadgeClass(ms: number): string {
    if (ms < 500) return 'text-emerald-700 font-semibold';
    if (ms < 1500) return 'text-amber-700 font-semibold';
    return 'text-red-700 font-semibold';
  }

  providerBadgeClass(provider: string): string {
    if (provider.toLowerCase().includes('gemini')) return 'bg-purple-50 text-purple-700 border-purple-200';
    if (provider.toLowerCase().includes('openai')) return 'bg-emerald-50 text-emerald-700 border-emerald-200';
    if (provider.toLowerCase().includes('qdrant')) return 'bg-blue-50 text-blue-700 border-blue-200';
    return 'bg-slate-100 text-slate-700 border-slate-200';
  }

  isUserMessage(message: AdminConversationMessage): boolean {
    return message.role === 'user';
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
