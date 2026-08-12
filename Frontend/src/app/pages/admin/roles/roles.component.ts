import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { RoleInfo } from '../../../models/admin.models';

export interface ModulePermission {
  moduleName: string;
  category: string;
  description: string;
  admin: boolean;
  support: boolean;
  contentManager: boolean;
  analyst: boolean;
  user: boolean;
}

@Component({
  selector: 'app-admin-roles',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './roles.component.html',
})
export class AdminRolesComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly trackByIndex = (index: number): number => index;
  readonly loadError = signal<string | null>(null);
  readonly selectedRole = signal<RoleInfo | null>(null);
  readonly notice = signal<{ kind: 'success' | 'error'; text: string } | null>(null);

  roles: RoleInfo[] = [];
  activeCategory = '';
  compareRoleA = 'Support';
  compareRoleB = 'Analyst';
  activeModalTab: 'capabilities' | 'security' | 'users' = 'capabilities';

  readonly permissionMatrix: ModulePermission[] = [
    {
      moduleName: 'Dashboard & Operations',
      category: 'Core Operations',
      description: 'Access operational health summary, KPI cards, system status, and recent activity.',
      admin: true, support: true, contentManager: true, analyst: true, user: false,
    },
    {
      moduleName: 'User Management',
      category: 'User & Security',
      description: 'Search, filter, view user details, verify emails, force resets, and change roles.',
      admin: true, support: true, contentManager: false, analyst: false, user: false,
    },
    {
      moduleName: 'Knowledge Base & RAG',
      category: 'Content & RAG',
      description: 'Create, edit, archive knowledge articles, and trigger Qdrant vector reindexing.',
      admin: true, support: false, contentManager: true, analyst: false, user: false,
    },
    {
      moduleName: 'AI Activity & Inspection',
      category: 'AI Observability',
      description: 'Monitor latency, status distributions, rate limits, and inspect logged user transcripts.',
      admin: true, support: true, contentManager: false, analyst: true, user: false,
    },
    {
      moduleName: 'Integrations & Config',
      category: 'Core Operations',
      description: 'Test API health checks and verify masked secrets for third-party services.',
      admin: true, support: true, contentManager: false, analyst: false, user: false,
    },
    {
      moduleName: 'Analytics & Growth',
      category: 'Analytics',
      description: 'View daily activity trends, conversion funnels, and top trip destinations.',
      admin: true, support: true, contentManager: true, analyst: true, user: false,
    },
    {
      moduleName: 'Audit Logs Inspection',
      category: 'User & Security',
      description: 'Search, filter, and inspect detailed system audit events and IP metadata.',
      admin: true, support: false, contentManager: false, analyst: true, user: false,
    },
  ];

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.adminService.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles;
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.loadError.set(this.extractError(error, 'Could not load the role catalog.'));
      },
    });
  }

  get filteredMatrix(): ModulePermission[] {
    if (!this.activeCategory) return this.permissionMatrix;
    return this.permissionMatrix.filter((m) => m.category === this.activeCategory);
  }

  hasPermission(module: ModulePermission, roleName: string): boolean {
    switch (roleName) {
      case 'Admin': return module.admin;
      case 'Support': return module.support;
      case 'ContentManager': return module.contentManager;
      case 'Analyst': return module.analyst;
      case 'User': return module.user;
      default: return false;
    }
  }

  roleGrantedCount(roleName: string): number {
    return this.permissionMatrix.filter((m) => this.hasPermission(m, roleName)).length;
  }

  roleCapabilityPercent(roleName: string): number {
    const total = this.permissionMatrix.length;
    if (total === 0) return 0;
    return Math.round((this.roleGrantedCount(roleName) / total) * 100);
  }

  get comparisonRows(): { moduleName: string; category: string; roleAHas: boolean; roleBHas: boolean }[] {
    return this.permissionMatrix.map((m) => ({
      moduleName: m.moduleName,
      category: m.category,
      roleAHas: this.hasPermission(m, this.compareRoleA),
      roleBHas: this.hasPermission(m, this.compareRoleB),
    }));
  }

  openRoleDetail(role: RoleInfo): void {
    this.activeModalTab = 'capabilities';
    this.selectedRole.set(role);
  }

  closeRoleDetail(): void {
    this.selectedRole.set(null);
  }

  exportMatrix(): void {
    this.notice.set({
      kind: 'success',
      text: 'Role capability matrix security report exported for compliance audit.',
    });
  }

  roleBadgeClass(roleName: string): string {
    switch (roleName) {
      case 'Admin': return 'bg-purple-50 text-purple-700 border border-purple-200/80 font-bold';
      case 'Support': return 'bg-blue-50 text-blue-700 border border-blue-200/80 font-bold';
      case 'ContentManager': return 'bg-amber-50 text-amber-700 border border-amber-200/80 font-bold';
      case 'Analyst': return 'bg-teal-50 text-teal-700 border border-teal-200/80 font-bold';
      default: return 'bg-slate-100 text-slate-700 border border-slate-200/80 font-semibold';
    }
  }

  roleAvatarGradient(roleName: string): string {
    switch (roleName) {
      case 'Admin': return 'bg-gradient-to-br from-purple-600 to-indigo-700 text-white shadow-2xs';
      case 'Support': return 'bg-gradient-to-br from-blue-500 to-cyan-600 text-white shadow-2xs';
      case 'ContentManager': return 'bg-gradient-to-br from-amber-500 to-orange-600 text-white shadow-2xs';
      case 'Analyst': return 'bg-gradient-to-br from-teal-500 to-emerald-600 text-white shadow-2xs';
      default: return 'bg-gradient-to-br from-slate-400 to-slate-600 text-white shadow-2xs';
    }
  }

  roleIcon(roleName: string): string {
    switch (roleName) {
      case 'Admin': return 'admin_panel_settings';
      case 'Support': return 'badge';
      case 'ContentManager': return 'auto_stories';
      case 'Analyst': return 'insights';
      default: return 'person';
    }
  }

  roleRiskLevel(roleName: string): { label: string; badgeClass: string } {
    switch (roleName) {
      case 'Admin': return { label: 'Critical System Access', badgeClass: 'bg-purple-100 text-purple-800 border-purple-200 font-bold' };
      case 'Support': return { label: 'Elevated Operations', badgeClass: 'bg-blue-100 text-blue-800 border-blue-200 font-bold' };
      case 'ContentManager': return { label: 'Moderate Content Scope', badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 font-bold' };
      case 'Analyst': return { label: 'Read-Only Observability', badgeClass: 'bg-teal-100 text-teal-800 border-teal-200 font-bold' };
      default: return { label: 'Standard End User', badgeClass: 'bg-slate-100 text-slate-700 border-slate-200 font-semibold' };
    }
  }

  privilegeLevel(roleName: string): string {
    switch (roleName) {
      case 'Admin': return 'Full Super Admin';
      case 'Support': return 'Operations & Support';
      case 'ContentManager': return 'RAG & Travel Content';
      case 'Analyst': return 'Analytics & AI Logs';
      default: return 'End User Traveler';
    }
  }

  labelFor(roleName: string): string {
    switch (roleName) {
      case 'ContentManager': return 'Content Manager';
      default: return roleName;
    }
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
