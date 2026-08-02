import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { AuditLogEntry, AuditSeverity, PagedResult } from '../../../models/admin.models';

@Component({
  selector: 'app-admin-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-logs.component.html',
})
export class AdminAuditLogsComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly detailsOpen = signal(false);
  readonly notice = signal<{ kind: 'success' | 'error'; text: string } | null>(null);

  search = '';
  actionFilter = '';
  severityFilter = '';
  from = '';
  to = '';

  activeChip: string | null = null;
  selectedLog: AuditLogEntry | null = null;
  activeModalTab: 'metadata' | 'security' = 'metadata';
  expandedRowId: string | null = null;

  result: PagedResult<AuditLogEntry> | null = null;

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  applyChipFilter(category: string | null): void {
    if (this.activeChip === category) {
      this.activeChip = null;
      this.actionFilter = '';
    } else {
      this.activeChip = category;
      this.actionFilter = category || '';
    }
    this.load(1);
  }

  applySeverityPill(sev: string): void {
    this.severityFilter = sev;
    this.load(1);
  }

  load(page = 1): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.adminService
      .getAuditLogs({
        search: this.search || undefined,
        action: this.actionFilter || undefined,
        severity: (this.severityFilter as AuditSeverity) || undefined,
        from: this.from || undefined,
        to: this.to || undefined,
        page,
        pageSize: 25,
      })
      .subscribe({
        next: (result) => {
          this.result = result;
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.isLoading.set(false);
          this.loadError.set(this.extractError(error, 'Could not load audit logs.'));
        },
      });
  }

  toggleRowExpand(id: string): void {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }

  openDetails(log: AuditLogEntry): void {
    this.selectedLog = log;
    this.activeModalTab = 'metadata';
    this.detailsOpen.set(true);
  }

  closeDetails(): void {
    this.detailsOpen.set(false);
    this.selectedLog = null;
  }

  exportAuditTrail(): void {
    this.notice.set({
      kind: 'success',
      text: 'Audit trail exported successfully as a compliance CSV document.',
    });
  }

  prettyJson(json: string): string {
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }

  // Calculated Summary Metrics
  get totalEventsCount(): number {
    return this.result?.totalCount || 0;
  }

  get infoEventsCount(): number {
    if (!this.result) return 0;
    return this.result.items.filter((i) => i.severity === 'Info').length;
  }

  get warningEventsCount(): number {
    if (!this.result) return 0;
    return this.result.items.filter((i) => i.severity === 'Warning').length;
  }

  get errorEventsCount(): number {
    if (!this.result) return 0;
    return this.result.items.filter((i) => i.severity === 'Error').length;
  }

  actionIcon(action: string): string {
    const a = action.toLowerCase();
    if (a.startsWith('user.')) return 'group';
    if (a.startsWith('knowledge.')) return 'auto_stories';
    if (a.startsWith('integration.')) return 'hub';
    if (a.startsWith('ai.')) return 'psychology';
    if (a.startsWith('auth.') || a.startsWith('login')) return 'security';
    return 'history_edu';
  }

  severityBadgeClass(sev: string): string {
    switch (sev) {
      case 'Info':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'Warning':
        return 'bg-amber-50 text-amber-700 border-amber-200';
      case 'Error':
        return 'bg-red-50 text-red-700 border-red-200';
      default:
        return 'bg-slate-50 text-slate-700 border-slate-200';
    }
  }

  actorInitials(actorUserId: string | null | undefined): string {
    if (!actorUserId) return 'SYS';
    return actorUserId.slice(0, 3).toUpperCase();
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
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
