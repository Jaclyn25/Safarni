import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { KnowledgeInput, KnowledgeItem, PagedResult } from '../../../models/admin.models';

@Component({
  selector: 'app-admin-knowledge',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './knowledge.component.html',
})
export class AdminKnowledgeComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly isActionPending = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly notice = signal<{ kind: 'success' | 'error'; text: string } | null>(null);
  readonly reindexRunning = signal(false);
  readonly articleOpen = signal(false);

  search = '';
  categoryFilter = '';
  archiveFilter: 'active' | 'archived' | 'all' = 'active';
  countryFilter = '';
  articleModalTab: 'preview' | 'edit' | 'rag' = 'preview';

  result: PagedResult<KnowledgeItem> | null = null;
  selectedItem: KnowledgeItem | null = null;
  isEditing = false;
  editorTab: 'edit' | 'preview' = 'edit';
  qdrantHealthy = true;

  categories = [
    'General',
    'Safety & Emergency',
    'Culture & Etiquette',
    'Transport',
    'Dining & Food',
    'Destinations',
  ];

  form: KnowledgeInput = {
    title: '',
    category: 'General',
    content: '',
    country: null,
    city: null,
    sourceUrl: null,
  };

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.checkQdrantHealth();
    this.load();
  }

  checkQdrantHealth(): void {
    this.adminService.getIntegrationsHealth().subscribe({
      next: (health) => {
        const qdrant = health.find((h) => h.name.toLowerCase().includes('qdrant'));
        this.qdrantHealthy = qdrant ? qdrant.status === 'Healthy' : true;
      },
      error: () => {
        this.qdrantHealthy = true;
      },
    });
  }

  applyCategoryPill(cat: string): void {
    this.categoryFilter = cat;
    this.load(1);
  }

  load(page = 1): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.adminService
      .getKnowledge({
        search: this.search || undefined,
        category: this.categoryFilter || undefined,
        isArchived: this.archiveFilter === 'archived' ? true : this.archiveFilter === 'active' ? false : undefined,
        page,
        pageSize: 20,
      })
      .subscribe({
        next: (result) => {
          this.result = result;
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.isLoading.set(false);
          this.loadError.set(this.extractError(error, 'Could not load knowledge base.'));
        },
      });
  }

  selectItem(item: KnowledgeItem): void {
    this.selectedItem = item;
    this.isEditing = false;
    this.articleModalTab = 'preview';
    this.articleOpen.set(true);
  }

  openCreate(): void {
    this.selectedItem = null;
    this.isEditing = true;
    this.articleModalTab = 'edit';
    this.form = { title: '', category: 'General', content: '', country: null, city: null, sourceUrl: null };
    this.articleOpen.set(true);
  }

  openEdit(item: KnowledgeItem): void {
    this.selectedItem = item;
    this.isEditing = true;
    this.articleModalTab = 'edit';
    this.form = {
      title: item.title,
      category: item.category,
      content: item.content,
      country: item.country,
      city: item.city,
      sourceUrl: item.sourceUrl,
    };
    this.articleOpen.set(true);
  }

  closeArticle(): void {
    this.articleOpen.set(false);
    this.isEditing = false;
    this.selectedItem = null;
  }

  cancelEdit(): void {
    this.isEditing = false;
    if (!this.selectedItem) {
      this.closeArticle();
    } else {
      this.articleModalTab = 'preview';
    }
  }

  save(): void {
    if (!this.form.title.trim() || !this.form.content.trim()) {
      this.notice.set({ kind: 'error', text: 'Title and content are required.' });
      return;
    }

    this.isActionPending.set(true);
    this.notice.set(null);

    const isUpdate = !!this.selectedItem;
    const request = isUpdate
      ? this.adminService.updateKnowledge(this.selectedItem!.id, this.form)
      : this.adminService.createKnowledge(this.form);

    request.subscribe({
      next: (savedItem) => {
        this.isActionPending.set(false);
        this.isEditing = false;
        this.selectedItem = savedItem;
        this.articleModalTab = 'preview';
        this.notice.set({
          kind: 'success',
          text: isUpdate ? 'Knowledge item updated and vector indexed.' : 'Knowledge item created and added to RAG index.',
        });
        this.load();
      },
      error: (error: unknown) => {
        this.isActionPending.set(false);
        this.notice.set({
          kind: 'error',
          text: this.extractError(error, 'Could not save knowledge item.'),
        });
      },
    });
  }

  toggleArchive(item: KnowledgeItem): void {
    this.isActionPending.set(true);
    const request = item.isArchived
      ? this.adminService.restoreKnowledge(item.id)
      : this.adminService.archiveKnowledge(item.id);

    request.subscribe({
      next: (updatedItem) => {
        this.isActionPending.set(false);
        this.selectedItem = updatedItem;
        this.notice.set({
          kind: 'success',
          text: item.isArchived ? 'Knowledge item restored to RAG index.' : 'Knowledge item archived.',
        });
        this.load();
      },
      error: (error: unknown) => {
        this.isActionPending.set(false);
        this.notice.set({
          kind: 'error',
          text: this.extractError(error, 'Could not update knowledge item status.'),
        });
      },
    });
  }

  reindex(): void {
    this.reindexRunning.set(true);
    this.notice.set(null);

    this.adminService.reindexKnowledge().subscribe({
      next: (result) => {
        this.reindexRunning.set(false);
        this.qdrantHealthy = result.qdrantHealthy;
        this.notice.set({
          kind: result.success ? 'success' : 'error',
          text: result.message,
        });
      },
      error: (error: unknown) => {
        this.reindexRunning.set(false);
        this.notice.set({
          kind: 'error',
          text: this.extractError(error, 'Reindex process failed to execute.'),
        });
      },
    });
  }

  // Calculated Metrics
  get totalArticlesCount(): number {
    return this.result?.totalCount || 0;
  }

  get activeArticlesCount(): number {
    if (!this.result) return 0;
    return this.result.items.filter((i) => !i.isArchived).length;
  }

  get archivedArticlesCount(): number {
    if (!this.result) return 0;
    return this.result.items.filter((i) => i.isArchived).length;
  }

  get categoriesCount(): number {
    return this.categories.length;
  }

  wordCount(text: string | null): number {
    if (!text) return 0;
    return text.trim().split(/\s+/).filter(Boolean).length;
  }

  estimatedChunks(text: string | null): number {
    const words = this.wordCount(text);
    return Math.max(1, Math.ceil(words / 150));
  }

  categoryBadgeClass(category: string): string {
    switch (category) {
      case 'Visa & Customs': return 'bg-purple-50 text-purple-700 border-purple-200';
      case 'Safety & Emergency': return 'bg-red-50 text-red-700 border-red-200';
      case 'Culture & Etiquette': return 'bg-amber-50 text-amber-700 border-amber-200';
      case 'Transport': return 'bg-blue-50 text-blue-700 border-blue-200';
      case 'Dining & Food': return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'Destinations': return 'bg-teal-50 text-teal-700 border-teal-200';
      default: return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }

  categoryIcon(category: string): string {
    switch (category) {
      case 'Visa & Customs': return 'badge';
      case 'Safety & Emergency': return 'emergency';
      case 'Culture & Etiquette': return 'language';
      case 'Transport': return 'directions_car';
      case 'Dining & Food': return 'restaurant';
      case 'Destinations': return 'travel_explore';
      default: return 'article';
    }
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  locationOf(item: KnowledgeItem): string {
    return [item.city, item.country].filter((val) => val && val.trim().length > 0).join(', ') || 'Global';
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
