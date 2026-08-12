import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { AuthService } from '../../../services/auth.service';
import {
  AdminUserDetail,
  AdminUserSummary,
  PagedResult,
  ROLE_LABELS,
  UserRole,
} from '../../../models/admin.models';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './users.component.html',
})
export class AdminUsersComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly trackByIndex = (index: number): number => index;
  readonly isActionPending = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly actionMessage = signal<{ kind: 'success' | 'error'; text: string } | null>(null);

  search = '';
  roleFilter = '';
  statusFilter = '';
  verifiedFilter = '';
  activePill: 'all' | 'active' | 'inactive' | 'admins' | 'unverified' = 'all';

  result: PagedResult<AdminUserSummary> | null = null;
  selectedUser: AdminUserDetail | null = null;
  readonly detailOpen = signal(false);
  activeModalTab: 'overview' | 'activity' | 'role' | 'security' = 'overview';
  confirmingDeactivate = false;
  confirmingRoleChange: string | null = null;

  roles = Object.keys(ROLE_LABELS) as UserRole[];
  assignableRoles = this.roles.filter(
    (role) => role !== 'TravelAgency'
  );

  constructor(
    private readonly adminService: AdminService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  get currentUserId(): string | null {
    return this.authService.getCurrentUser()?.userId || null;
  }

  isSelf(userId: string): boolean {
    return Boolean(this.currentUserId && this.currentUserId === userId);
  }

  applyPill(pill: 'all' | 'active' | 'inactive' | 'admins' | 'unverified'): void {
    this.activePill = pill;
    switch (pill) {
      case 'all':
        this.statusFilter = '';
        this.roleFilter = '';
        this.verifiedFilter = '';
        break;
      case 'active':
        this.statusFilter = 'active';
        this.roleFilter = '';
        this.verifiedFilter = '';
        break;
      case 'inactive':
        this.statusFilter = 'inactive';
        this.roleFilter = '';
        this.verifiedFilter = '';
        break;
      case 'admins':
        this.statusFilter = '';
        this.roleFilter = 'Admin';
        this.verifiedFilter = '';
        break;
      case 'unverified':
        this.statusFilter = '';
        this.roleFilter = '';
        this.verifiedFilter = 'unverified';
        break;
    }
    this.load(1);
  }

  load(page = 1): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const isActive =
      this.statusFilter === 'active'
        ? true
        : this.statusFilter === 'inactive'
        ? false
        : undefined;

    const isEmailVerified =
      this.verifiedFilter === 'verified'
        ? true
        : this.verifiedFilter === 'unverified'
        ? false
        : undefined;

    this.adminService
      .getUsers({
        search: this.search || undefined,
        role: (this.roleFilter as UserRole) || undefined,
        isActive,
        isEmailVerified,
        page,
        pageSize: 15,
      })
      .subscribe({
        next: (result: PagedResult<AdminUserSummary>) => {
          this.result = result;
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.isLoading.set(false);
          this.loadError.set(this.extractError(error, 'Could not load users.'));
        },
      });
  }

  openDetail(user: AdminUserSummary): void {
    this.actionMessage.set(null);
    this.confirmingDeactivate = false;
    this.confirmingRoleChange = null;
    this.activeModalTab = 'overview';

    this.adminService.getUser(user.id).subscribe({
      next: (detail: AdminUserDetail) => {
        this.selectedUser = detail;
        this.detailOpen.set(true);
      },
      error: (error: unknown) => {
        this.actionMessage.set({
          kind: 'error',
          text: this.extractError(error, 'Could not load user detail.'),
        });
      },
    });
  }

  closeDetail(): void {
    this.detailOpen.set(false);
    this.selectedUser = null;
    this.confirmingDeactivate = false;
    this.confirmingRoleChange = null;
  }

  copyUserId(userId: string): void {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(userId);
      this.actionMessage.set({ kind: 'success', text: `Copied User ID: ${userId}` });
    }
  }

  changeRole(newRole: string): void {
    if (!this.selectedUser) return;
    if (this.isSelf(this.selectedUser.id) && newRole !== 'Admin') {
      this.actionMessage.set({ kind: 'error', text: 'Self-demotion is disabled to preserve admin access.' });
      this.confirmingRoleChange = null;
      return;
    }

    this.isActionPending.set(true);
    this.actionMessage.set(null);

    this.adminService.updateUserRoles(this.selectedUser.id, newRole as UserRole).subscribe({
      next: (updated: AdminUserDetail) => {
        this.isActionPending.set(false);
        this.selectedUser = updated;
        this.confirmingRoleChange = null;
        this.actionMessage.set({ kind: 'success', text: `User role updated to ${this.roleLabel(newRole)}.` });
        this.load(this.result?.page || 1);
      },
      error: (error: unknown) => {
        this.isActionPending.set(false);
        this.actionMessage.set({
          kind: 'error',
          text: this.extractError(error, 'Could not update role.'),
        });
      },
    });
  }

  toggleStatus(): void {
    if (!this.selectedUser) return;
    this.isActionPending.set(true);
    this.actionMessage.set(null);

    const nextStatus = !this.selectedUser.isActive;

    this.adminService.updateUserStatus(this.selectedUser.id, nextStatus).subscribe({
      next: (updated: AdminUserDetail) => {
        this.isActionPending.set(false);
        this.selectedUser = updated;
        this.confirmingDeactivate = false;
        this.actionMessage.set({
          kind: 'success',
          text: `Account ${updated.isActive ? 'activated' : 'deactivated'} successfully.`,
        });
        this.load(this.result?.page || 1);
      },
      error: (error: unknown) => {
        this.isActionPending.set(false);
        this.actionMessage.set({
          kind: 'error',
          text: this.extractError(error, 'Could not update status.'),
        });
      },
    });
  }

  verifyEmail(): void {
    if (!this.selectedUser) return;
    this.isActionPending.set(true);
    this.actionMessage.set(null);

    this.adminService.verifyUserEmail(this.selectedUser.id).subscribe({
      next: (updated: AdminUserDetail) => {
        this.isActionPending.set(false);
        this.selectedUser = updated;
        this.actionMessage.set({ kind: 'success', text: 'Email verified successfully.' });
        this.load(this.result?.page || 1);
      },
      error: (error: unknown) => {
        this.isActionPending.set(false);
        this.actionMessage.set({
          kind: 'error',
          text: this.extractError(error, 'Could not verify email.'),
        });
      },
    });
  }

  forcePasswordReset(): void {
    if (!this.selectedUser) return;
    this.isActionPending.set(true);
    this.actionMessage.set(null);

    this.adminService.forcePasswordReset(this.selectedUser.id).subscribe({
      next: () => {
        this.isActionPending.set(false);
        this.actionMessage.set({
          kind: 'success',
          text: `Password reset email sent to ${this.selectedUser?.email}.`,
        });
      },
      error: (error: unknown) => {
        this.isActionPending.set(false);
        this.actionMessage.set({
          kind: 'error',
          text: this.extractError(error, 'Could not send password reset email.'),
        });
      },
    });
  }

  // Summary Metrics
  get totalUsersCount(): number {
    return this.result?.totalCount || 0;
  }

  get activeUsersCount(): number {
    if (!this.result) return 0;
    return this.result.items.filter((u) => u.isActive).length;
  }

  get verifiedUsersCount(): number {
    if (!this.result) return 0;
    return this.result.items.filter((u) => u.isEmailVerified).length;
  }

  get adminUsersCount(): number {
    if (!this.result) return 0;
    return this.result.items.filter((u) => u.role === 'Admin' || u.role === 'Support').length;
  }

  userInitials(name: string): string {
    const parts = (name || 'U').split(' ').filter(Boolean);
    return ((parts[0]?.charAt(0) ?? 'U') + (parts[1]?.charAt(0) ?? '')).toUpperCase();
  }

  roleLabel(role: UserRole | string | null | undefined): string {
    if (!role) return 'User';
    return ROLE_LABELS[role as UserRole] || role;
  }

  roleBadgeClass(role: UserRole | string | null | undefined): string {
    switch (role) {
      case 'Admin':
        return 'bg-purple-50 text-purple-700 border border-purple-200/80 font-bold';
      case 'Support':
        return 'bg-blue-50 text-blue-700 border border-blue-200/80 font-bold';
      case 'ContentManager':
        return 'bg-amber-50 text-amber-700 border border-amber-200/80 font-bold';
      case 'Analyst':
        return 'bg-teal-50 text-teal-700 border border-teal-200/80 font-bold';
      default:
        return 'bg-slate-100 text-slate-700 border border-slate-200/80 font-semibold';
    }
  }

  avatarBgClass(role: UserRole | string | null | undefined): string {
    switch (role) {
      case 'Admin':
        return 'bg-gradient-to-br from-purple-600 to-indigo-700 text-white shadow-2xs';
      case 'Support':
        return 'bg-gradient-to-br from-blue-500 to-cyan-600 text-white shadow-2xs';
      case 'ContentManager':
        return 'bg-gradient-to-br from-amber-500 to-orange-600 text-white shadow-2xs';
      case 'Analyst':
        return 'bg-gradient-to-br from-teal-500 to-emerald-600 text-white shadow-2xs';
      default:
        return 'bg-gradient-to-br from-sky-500 to-blue-600 text-white shadow-2xs';
    }
  }

  formatDate(iso: string | null | undefined): string {
    if (!iso) return 'N/A';
    return new Date(iso).toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
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
