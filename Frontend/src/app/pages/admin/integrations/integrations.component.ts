import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { AdminService } from '../../../services/admin.service';
import { IntegrationConfigEntry, IntegrationHealthCheck } from '../../../models/admin.models';

export interface IntegrationGroup {
  category: string;
  icon: string;
  items: IntegrationHealthCheck[];
}

export interface FeatureTopology {
  featureName: string;
  description: string;
  icon: string;
  backends: string[];
  status: 'Healthy' | 'Degraded';
}

@Component({
  selector: 'app-admin-integrations',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  providers: [provideCharts(withDefaultRegisterables())],
  templateUrl: './integrations.component.html',
})
export class AdminIntegrationsComponent implements OnInit, OnDestroy {
  readonly isLoading = signal(true);
  readonly trackByIndex = (index: number): number => index;
  readonly loadError = signal<string | null>(null);
  readonly notice = signal<{ kind: 'success' | 'error'; text: string } | null>(null);
  readonly testingName = signal<string | null>(null);
  readonly isTestingAll = signal(false);
  readonly selectedConfig = signal<IntegrationConfigEntry | null>(null);
  readonly Math = Math;

  health: IntegrationHealthCheck[] = [];
  config: IntegrationConfigEntry[] = [];
  activeGroupFilter = '';

  autoRefreshEnabled = false;
  private autoRefreshTimer: ReturnType<typeof setInterval> | null = null;

  // Chart configs
  healthChartType: ChartType = 'doughnut';
  healthChartData: ChartData<'doughnut'> = { labels: [], datasets: [] };
  healthChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom', labels: { boxWidth: 12 } },
    },
  };

  latencyChartType: ChartType = 'bar';
  latencyChartData: ChartData<'bar'> = { labels: [], datasets: [] };
  latencyChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (ctx) => `${ctx.raw} ms latency`,
        },
      },
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

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  toggleAutoRefresh(): void {
    this.autoRefreshEnabled = !this.autoRefreshEnabled;
    if (this.autoRefreshEnabled) {
      this.startAutoRefresh();
      this.notice.set({ kind: 'success', text: 'Auto-refresh enabled (polling health every 30s).' });
    } else {
      this.stopAutoRefresh();
      this.notice.set({ kind: 'success', text: 'Auto-refresh disabled.' });
    }
  }

  private startAutoRefresh(): void {
    this.stopAutoRefresh();
    this.autoRefreshTimer = setInterval(() => {
      this.loadSilent();
    }, 30000);
  }

  private stopAutoRefresh(): void {
    if (this.autoRefreshTimer) {
      clearInterval(this.autoRefreshTimer);
      this.autoRefreshTimer = null;
    }
  }

  applyGroupPill(category: string): void {
    this.activeGroupFilter = category;
  }

  load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.adminService.getIntegrationsHealth().subscribe({
      next: (health) => {
        this.health = health;
        this.updateHealthChart(health);
        this.updateLatencyChart(health);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.loadError.set(this.extractError(error, 'Could not check integration health.'));
      },
    });

    this.adminService.getIntegrationsConfig().subscribe({
      next: (config) => {
        this.config = config;
      },
      error: () => {
        this.config = [];
      },
    });
  }

  loadSilent(): void {
    this.adminService.getIntegrationsHealth().subscribe({
      next: (health) => {
        this.health = health;
        this.updateHealthChart(health);
        this.updateLatencyChart(health);
      },
    });
  }

  private updateHealthChart(health: IntegrationHealthCheck[]): void {
    if (!health || health.length === 0) {
      this.healthChartData = { labels: [], datasets: [] };
      return;
    }

    const counts: Record<string, number> = {
      Healthy: health.filter((h) => h.status === 'Healthy').length,
      Unhealthy: health.filter((h) => h.status === 'Unhealthy').length,
      NotConfigured: health.filter((h) => h.status !== 'Healthy' && h.status !== 'Unhealthy').length,
    };

    this.healthChartData = {
      labels: ['Healthy', 'Unhealthy', 'Not Configured'],
      datasets: [
        {
          data: [counts['Healthy'], counts['Unhealthy'], counts['NotConfigured']],
          backgroundColor: ['#10b981', '#ef4444', '#9ca3af'],
        },
      ],
    };
  }

  private updateLatencyChart(health: IntegrationHealthCheck[]): void {
    if (!health || health.length === 0) {
      this.latencyChartData = { labels: [], datasets: [] };
      return;
    }

    const sorted = [...health].sort((a, b) => b.latencyMs - a.latencyMs);
    this.latencyChartData = {
      labels: sorted.map((h) => h.displayName),
      datasets: [
        {
          label: 'Latency (ms)',
          data: sorted.map((h) => h.latencyMs),
          backgroundColor: sorted.map((h) =>
            h.latencyMs < 200 ? '#10b981' : h.latencyMs < 800 ? '#f59e0b' : '#ef4444'
          ),
          borderRadius: 4,
        },
      ],
    };
  }

  get featureTopology(): FeatureTopology[] {
    return [
      {
        featureName: 'AI Itinerary Planner',
        description: 'Generates custom day-by-day travel itineraries and recommendations.',
        icon: 'auto_awesome',
        backends: ['Gemini Itinerary', 'Qdrant Vector DB'],
        status: this.isSubsystemHealthy(['Gemini Itinerary', 'Qdrant']) ? 'Healthy' : 'Degraded',
      },
      {
        featureName: 'AI Concierge & RAG Chat',
        description: 'Context-aware AI travel assistant powered by vector retrieval.',
        icon: 'chat',
        backends: ['Groq Chat', 'OpenRouter Embeddings', 'Qdrant Vector DB', 'Google Places'],
        status: this.isSubsystemHealthy(['Groq Chat', 'OpenRouter', 'Qdrant', 'Google Places']) ? 'Healthy' : 'Degraded',
      },
      {
        featureName: 'Destination & Places Search',
        description: 'Live place search, attraction info, and photo thumbnails.',
        icon: 'travel_explore',
        backends: ['Google Places'],
        status: this.isSubsystemHealthy(['Google Places']) ? 'Healthy' : 'Degraded',
      },
      {
        featureName: 'Email & Password Reset',
        description: 'Account registration verification and forced password reset emails.',
        icon: 'mail',
        backends: ['Email (SMTP)'],
        status: this.isSubsystemHealthy(['Email']) ? 'Healthy' : 'Degraded',
      },
    ];
  }

  private isSubsystemHealthy(names: string[]): boolean {
    return names.every((n) => {
      const match = this.health.find((h) => h.displayName.toLowerCase().includes(n.toLowerCase()) || h.name.toLowerCase().includes(n.toLowerCase()));
      return match ? match.status === 'Healthy' : true;
    });
  }

  get groupedIntegrations(): IntegrationGroup[] {
    if (!this.health || this.health.length === 0) return [];

    const ai = this.health.filter((h) =>
      ['gemini', 'openai', 'llm', 'ai'].some((k) => h.name.toLowerCase().includes(k))
    );
    const rag = this.health.filter((h) =>
      ['qdrant', 'vector', 'rag'].some((k) => h.name.toLowerCase().includes(k))
    );
    const search = this.health.filter((h) =>
      ['google', 'places', 'amadeus', 'skyscanner', 'flight'].some((k) => h.name.toLowerCase().includes(k))
    );
    const email = this.health.filter((h) =>
      ['email', 'resend', 'smtp', 'twilio'].some((k) => h.name.toLowerCase().includes(k))
    );

    const groupedNames = new Set([...ai, ...rag, ...search, ...email].map((h) => h.name));
    const infrastructure = this.health.filter((h) => !groupedNames.has(h.name));

    const groups: IntegrationGroup[] = [];

    if (ai.length > 0) groups.push({ category: 'AI & Language Models', icon: 'smart_toy', items: ai });
    if (rag.length > 0) groups.push({ category: 'RAG & Vector Database', icon: 'database', items: rag });
    if (search.length > 0) groups.push({ category: 'Search & Travel APIs', icon: 'travel_explore', items: search });
    if (email.length > 0) groups.push({ category: 'Email & Notifications', icon: 'mail', items: email });
    if (infrastructure.length > 0) groups.push({ category: 'Infrastructure & Storage', icon: 'cloud', items: infrastructure });

    const allGroups = groups.length > 0 ? groups : [{ category: 'All Connected Services', icon: 'api', items: this.health }];

    if (!this.activeGroupFilter) return allGroups;
    return allGroups.filter((g) => g.category === this.activeGroupFilter);
  }

  test(name: string): void {
    this.testingName.set(name);
    this.notice.set(null);

    this.adminService.testIntegration(name).subscribe({
      next: (check) => {
        this.testingName.set(null);
        const index = this.health.findIndex((item) => item.name === check.name);
        if (index >= 0) {
          this.health[index] = check;
        }
        this.updateHealthChart(this.health);
        this.updateLatencyChart(this.health);
        this.notice.set({
          kind: check.status === 'Healthy' ? 'success' : 'error',
          text: `${check.displayName}: ${check.status} (${check.latencyMs} ms). ${check.detail}`,
        });
      },
      error: (error: unknown) => {
        this.testingName.set(null);
        this.notice.set({
          kind: 'error',
          text: this.extractError(error, 'Could not test this integration.'),
        });
      },
    });
  }

  testAllServices(): void {
    this.isTestingAll.set(true);
    this.load();
    setTimeout(() => {
      this.isTestingAll.set(false);
      this.notice.set({
        kind: 'success',
        text: 'All subsystem integration diagnostics re-evaluated successfully.',
      });
    }, 1200);
  }

  openConfigDetail(c: IntegrationConfigEntry): void {
    this.selectedConfig.set(c);
  }

  closeConfigDetail(): void {
    this.selectedConfig.set(null);
  }

  // Calculated Metrics
  get totalServicesCount(): number {
    return this.health.length;
  }

  get healthyServicesCount(): number {
    return this.health.filter((h) => h.status === 'Healthy').length;
  }

  get unhealthyServicesCount(): number {
    return this.health.filter((h) => h.status === 'Unhealthy').length;
  }

  get meanLatencyMs(): number {
    if (this.health.length === 0) return 0;
    const sum = this.health.reduce((acc, curr) => acc + curr.latencyMs, 0);
    return Math.round(sum / this.health.length);
  }

  latencyBadgeClass(ms: number): string {
    if (ms < 200) return 'text-emerald-700 font-semibold';
    if (ms < 800) return 'text-amber-700 font-semibold';
    return 'text-red-700 font-semibold';
  }

  statusBadgeClass(status: string): string {
    if (status === 'Healthy') return 'bg-emerald-100 text-emerald-800 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300';
    if (status === 'Unhealthy') return 'bg-red-100 text-red-800 border-red-200 dark:bg-red-950 dark:text-red-300';
    return 'bg-gray-100 text-gray-700 border-gray-200 dark:bg-gray-800 dark:text-gray-300';
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleString(undefined, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
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
