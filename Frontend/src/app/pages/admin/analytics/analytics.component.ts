import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { AdminService } from '../../../services/admin.service';
import {
  AnalyticsDailyPoint,
  AnalyticsFunnel,
  AnalyticsOverview,
  TopDestination,
} from '../../../models/admin.models';

interface FunnelStage {
  label: string;
  value: number;
  percentage: number;
  dropoff?: number;
}

@Component({
  selector: 'app-admin-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective],
  providers: [provideCharts(withDefaultRegisterables())],
  templateUrl: './analytics.component.html',
})
export class AdminAnalyticsComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly trackByIndex = (index: number): number => index;
  readonly loadError = signal<string | null>(null);

  from = '';
  to = '';
  activePreset: '7d' | '30d' | '90d' | 'custom' = '30d';
  chartViewMode: 'bar' | 'line' = 'bar';

  overview: AnalyticsOverview | null = null;
  funnel: AnalyticsFunnel | null = null;
  destinations: TopDestination[] = [];

  // Range metrics
  totalRangeUsers = 0;
  totalRangeTrips = 0;
  totalRangeMessages = 0;

  // Chart configurations
  activityChartType: ChartType = 'bar';
  activityChartData: ChartData<'bar' | 'line'> = { labels: [], datasets: [] };
  activityChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom', labels: { boxWidth: 12, font: { size: 11 } } },
      tooltip: {
        mode: 'index',
        intersect: false,
        padding: 10,
        cornerRadius: 8,
      },
    },
    scales: {
      x: {
        stacked: true,
        grid: { display: false },
        ticks: { maxRotation: 45, minRotation: 0, font: { size: 10 } },
      },
      y: {
        stacked: true,
        beginAtZero: true,
        grid: { color: 'rgba(0, 0, 0, 0.04)' },
        ticks: { stepSize: 1, precision: 0 },
      },
    },
  };

  funnelChartType: ChartType = 'bar';
  funnelChartData: ChartData<'bar'> = { labels: [], datasets: [] };
  funnelChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'y',
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (context) => `${context.raw} users (${this.funnelStages[context.dataIndex]?.percentage || 0}%)`,
        },
      },
    },
    scales: {
      x: { beginAtZero: true, grid: { color: 'rgba(0, 0, 0, 0.04)' }, ticks: { precision: 0 } },
      y: { grid: { display: false } },
    },
  };

  destinationsChartType: 'doughnut' = 'doughnut';
  destinationsChartData: ChartData<'doughnut'> = { labels: [], datasets: [] };
  destinationsChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '65%',
    plugins: {
      legend: {
        position: 'bottom',
        labels: { boxWidth: 10, font: { size: 11 } },
      },
      tooltip: {
        callbacks: {
          label: (context) => ` ${context.label}: ${context.raw} trip(s)`,
        },
      },
    },
  };

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.selectPreset('30d');
  }

  selectPreset(preset: '7d' | '30d' | '90d' | 'custom'): void {
    this.activePreset = preset;
    if (preset !== 'custom') {
      const now = new Date();
      const days = preset === '7d' ? 7 : preset === '30d' ? 30 : 90;
      const start = new Date(now.getTime() - days * 24 * 60 * 60 * 1000);
      this.from = this.toDateInput(start);
      this.to = this.toDateInput(now);
    }
    this.load();
  }

  setChartViewMode(mode: 'bar' | 'line'): void {
    this.chartViewMode = mode;
    this.activityChartType = mode;
    if (this.overview) {
      this.updateActivityChart(this.overview);
    }
  }

  load(): void {
    if (!this.from || !this.to) {
      this.loadError.set('Pick both a start and an end date.');
      return;
    }
    this.isLoading.set(true);
    this.loadError.set(null);

    this.adminService.getAnalyticsOverview(this.from, this.to).subscribe({
      next: (overview) => {
        this.overview = overview;
        this.computeRangeMetrics(overview.points);
        this.updateActivityChart(overview);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.loadError.set(this.extractError(error, 'Could not load analytics overview.'));
      },
    });

    this.adminService.getAnalyticsFunnels().subscribe({
      next: (funnel) => {
        this.funnel = funnel;
        this.updateFunnelChart(funnel);
      },
      error: () => {
        this.funnel = null;
      },
    });

    this.adminService.getTopDestinations(10).subscribe({
      next: (destinations) => {
        this.destinations = destinations;
        this.updateDestinationsChart(destinations);
      },
      error: () => {
        this.destinations = [];
      },
    });
  }

  private computeRangeMetrics(points: AnalyticsDailyPoint[]): void {
    if (!points || points.length === 0) {
      this.totalRangeUsers = 0;
      this.totalRangeTrips = 0;
      this.totalRangeMessages = 0;
      return;
    }

    this.totalRangeUsers = points.reduce((acc, p) => acc + p.users, 0);
    this.totalRangeTrips = points.reduce((acc, p) => acc + p.trips, 0);
    this.totalRangeMessages = points.reduce((acc, p) => acc + p.messages, 0);
  }

  private updateActivityChart(overview: AnalyticsOverview): void {
    if (!overview || overview.points.length === 0) {
      this.activityChartData = { labels: [], datasets: [] };
      return;
    }

    const labels = overview.points.map((p) => this.shortDate(p.date));
    const isLine = this.chartViewMode === 'line';

    this.activityChartData = {
      labels,
      datasets: [
        {
          label: 'Users',
          data: overview.points.map((p) => p.users),
          backgroundColor: isLine ? 'rgba(16, 185, 129, 0.15)' : '#10b981',
          borderColor: '#10b981',
          borderWidth: 2,
          borderRadius: 4,
          tension: 0.3,
          fill: isLine,
        },
        {
          label: 'Trips',
          data: overview.points.map((p) => p.trips),
          backgroundColor: isLine ? 'rgba(59, 130, 246, 0.15)' : '#3b82f6',
          borderColor: '#3b82f6',
          borderWidth: 2,
          borderRadius: 4,
          tension: 0.3,
          fill: isLine,
        },
        {
          label: 'Messages',
          data: overview.points.map((p) => p.messages),
          backgroundColor: isLine ? 'rgba(139, 92, 246, 0.15)' : '#8b5cf6',
          borderColor: '#8b5cf6',
          borderWidth: 2,
          borderRadius: 4,
          tension: 0.3,
          fill: isLine,
        },
        {
          label: 'Itinerary Jobs',
          data: overview.points.map((p) => p.itineraryJobs),
          backgroundColor: isLine ? 'rgba(99, 102, 241, 0.15)' : '#6366f1',
          borderColor: '#6366f1',
          borderWidth: 2,
          borderRadius: 4,
          tension: 0.3,
          fill: isLine,
        },
      ],
    };
  }

  private updateFunnelChart(funnel: AnalyticsFunnel | null): void {
    if (!funnel || funnel.registeredUsers <= 0) {
      this.funnelChartData = { labels: [], datasets: [] };
      return;
    }

    const stages = this.funnelStages;
    this.funnelChartData = {
      labels: stages.map((s) => s.label),
      datasets: [
        {
          data: stages.map((s) => s.value),
          backgroundColor: ['#2563eb', '#3b82f6', '#60a5fa', '#93c5fd', '#bfdbfe'],
          borderRadius: 4,
          barThickness: 24,
        },
      ],
    };
  }

  private updateDestinationsChart(destinations: TopDestination[]): void {
    if (!destinations || destinations.length === 0) {
      this.destinationsChartData = { labels: [], datasets: [] };
      return;
    }

    const palette = [
      '#0d9488', '#3b82f6', '#8b5cf6', '#f59e0b', '#ec4899',
      '#10b981', '#06b6d4', '#f43f5e', '#84cc16', '#a855f7'
    ];

    this.destinationsChartData = {
      labels: destinations.map((d) => d.name),
      datasets: [
        {
          data: destinations.map((d) => d.tripCount),
          backgroundColor: palette.slice(0, destinations.length),
          borderWidth: 2,
          borderColor: '#ffffff',
          hoverOffset: 6,
        },
      ],
    };
  }

  get funnelStages(): FunnelStage[] {
    const funnel = this.funnel;
    if (!funnel || funnel.registeredUsers <= 0) {
      return [];
    }

    const reg = funnel.registeredUsers;
    const ver = funnel.verifiedUsers;
    const trip = funnel.usersWithTrips;
    const itin = funnel.usersWithGeneratedItineraries;

    return [
      { label: 'Registered Users', value: reg, percentage: 100 },
      { label: 'Verified Emails', value: ver, percentage: this.percent(ver, reg), dropoff: this.percent(reg - ver, reg) },
      { label: 'Created a Trip', value: trip, percentage: this.percent(trip, reg), dropoff: this.percent(ver - trip, ver) },
      { label: 'Generated Itinerary', value: itin, percentage: this.percent(itin, reg), dropoff: this.percent(trip - itin, trip) },
    ];
  }

  get maxDestinationTrips(): number {
    if (this.destinations.length === 0) return 1;
    return Math.max(...this.destinations.map((d) => d.tripCount));
  }

  destinationPercentage(tripCount: number): number {
    const max = this.maxDestinationTrips;
    if (max <= 0) return 0;
    return Math.round((tripCount / max) * 100);
  }

  shortDate(iso: string): string {
    return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
  }

  private toDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private percent(part: number, whole: number): number {
    if (whole <= 0) return 0;
    return Math.round((part / whole) * 100);
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
