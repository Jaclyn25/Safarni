import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import {
  PublicTripSearchResponse,
  PublicTripService
} from '../../services/public-trip.service';
import {
  CommunityPost,
  CommunityMedia,
  CommunityDestinationTrend,
  DestinationSuggestion,
  CommunityPostType,
  CommunityService,
  CreateCommunityPostRequest
} from '../../services/community.service';

interface PendingMedia {
  file: File;
  previewUrl: string;
  mediaType: 'image' | 'video';
}

@Component({
  selector: 'app-community',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './community.component.html',
  styleUrl: './community.component.css'
})
export class CommunityComponent implements OnInit, OnDestroy {
  @ViewChild('bodyEditor') bodyEditor?: ElementRef<HTMLElement>;
  private readonly communityService = inject(CommunityService);
  private readonly publicTripService = inject(PublicTripService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly posts = signal<CommunityPost[]>([]);
  readonly publicTrips = signal<PublicTripSearchResponse[]>([]);
  readonly savedPosts = signal<CommunityPost[]>([]);
  readonly trendingDestinations = signal<CommunityDestinationTrend[]>([]);
  readonly destinationSuggestions = signal<DestinationSuggestion[]>([]);
  readonly isLoading = signal(true);
  readonly isPublicTripsLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal('');
  readonly formMessage = signal('');

  selectedType: CommunityPostType | '' = '';
  destinationFilter = '';
  showFilters = false;
  showComposer = false;
  activePostMenuId = '';
  editingPostId = '';
  postType: CommunityPostType = 'TravelStory';
  postTitle = '';
  postBody = '';
  postDestination = '';
  postTags = '';
  pendingMedia: PendingMedia[] = [];
  existingMedia: CommunityMedia[] = [];
  private destinationLookupTimer?: number;

  readonly maxMediaFiles = 5;
  readonly maxImageSizeBytes = 10 * 1024 * 1024;
  readonly maxVideoSizeBytes = 60 * 1024 * 1024;

  readonly postTypes: Array<{ value: CommunityPostType; label: string; icon: string }> = [
    { value: 'TravelStory', label: 'Travel story', icon: 'auto_stories' },
    { value: 'Vlog', label: 'Travel vlog', icon: 'videocam' },
    { value: 'Question', label: 'Ask the community', icon: 'help' },
    { value: 'Recommendation', label: 'Recommendation', icon: 'tips_and_updates' }
  ];

  ngOnInit(): void {
    this.loadFeed();
    this.loadCommunitySidebar();
    this.route.queryParamMap.subscribe(params => {
      const postId = params.get('edit');
      if (!postId || !this.isSignedIn) return;
      this.communityService.getPost(postId).subscribe({
        next: response => {
          if (response.data?.isOwnedByCurrentUser) this.openComposer(response.data);
        },
        error: () => this.errorMessage.set('The post could not be opened for editing.')
      });
    });
  }

  ngOnDestroy(): void {
    this.clearPendingMedia();
    if (this.destinationLookupTimer) window.clearTimeout(this.destinationLookupTimer);
  }

  get isSignedIn(): boolean {
    return !!this.authService.getCurrentUser();
  }

  get currentUserName(): string {
    return this.authService.getCurrentUser()?.fullName || 'Your name';
  }

  get tagCount(): number {
    return this.postTags.split(',').map(tag => tag.trim()).filter(Boolean).length;
  }

  get bodyLength(): number {
    const holder = document.createElement('div');
    holder.innerHTML = this.postBody;
    return (holder.textContent || '').length;
  }

  get isEditing(): boolean {
    return !!this.editingPostId;
  }

  loadFeed(): void {
    this.isLoading.set(true);
    this.isPublicTripsLoading.set(true);
    this.errorMessage.set('');
    this.loadPublicTrips();
    this.communityService.getFeed(this.selectedType, this.destinationFilter).subscribe({
      next: response => {
        this.posts.set(response.data ?? []);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(error?.error?.message || 'We could not load the community right now.');
        this.isLoading.set(false);
      }
    });
  }

  private loadCommunitySidebar(): void {
    this.communityService.getTrendingDestinations().subscribe({
      next: response => this.trendingDestinations.set(response.data ?? []),
      error: () => this.trendingDestinations.set([])
    });

    if (!this.isSignedIn) {
      this.savedPosts.set([]);
      return;
    }

    this.communityService.getSavedPosts().subscribe({
      next: response => this.savedPosts.set(response.data ?? []),
      error: () => this.savedPosts.set([])
    });
  }

  private loadPublicTrips(): void {
    this.publicTripService.searchPublicTrips({
      destination: this.destinationFilter.trim() || undefined,
      sortOrder: 'newest'
    }).subscribe({
      next: response => {
        this.publicTrips.set((response.data ?? []).slice(0, 6));
        this.isPublicTripsLoading.set(false);
      },
      error: () => {
        // Public trips are an enhancement to the feed. Keep community posts usable
        // if the discovery endpoint is temporarily unavailable.
        this.publicTrips.set([]);
        this.isPublicTripsLoading.set(false);
      }
    });
  }

  openComposer(post?: CommunityPost): void {
    if (!this.isSignedIn) {
      void this.router.navigate(['/login']);
      return;
    }
    this.editingPostId = post?.id ?? '';
    this.postType = post?.type ?? 'TravelStory';
    this.postTitle = post?.title ?? '';
    this.postBody = post?.body ?? '';
    this.postDestination = post?.destination ?? '';
    this.existingMedia = [...(post?.media ?? [])];
    this.clearPendingMedia();
    this.destinationSuggestions.set([]);
    this.activePostMenuId = '';
    this.showComposer = true;
    this.formMessage.set('');
    window.setTimeout(() => {
      if (this.bodyEditor) this.bodyEditor.nativeElement.innerHTML = this.postBody;
    });
  }

  closeComposer(): void {
    if (!this.isSubmitting()) {
      this.showComposer = false;
      this.editingPostId = '';
      this.existingMedia = [];
      this.destinationSuggestions.set([]);
    }
  }

  togglePostMenu(event: Event, postId: string): void {
    event.stopPropagation();
    this.activePostMenuId = this.activePostMenuId === postId ? '' : postId;
  }

  async deletePost(event: Event, post: CommunityPost): Promise<void> {
    event.stopPropagation();
    this.activePostMenuId = '';
    if (!window.confirm(`Delete “${post.title}”? This will remove it from the community.`)) return;

    try {
      await firstValueFrom(this.communityService.deletePost(post.id));
      this.posts.update(posts => posts.filter(item => item.id !== post.id));
      this.savedPosts.update(posts => posts.filter(item => item.id !== post.id));
      this.loadCommunitySidebar();
    } catch (error: any) {
      this.errorMessage.set(error?.error?.errors?.[0]?.message || 'The post could not be deleted.');
    }
  }

  onMediaSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';

    if (files.length === 0) return;

    if (this.existingMedia.length + this.pendingMedia.length + files.length > this.maxMediaFiles) {
      this.formMessage.set(`You can add up to ${this.maxMediaFiles} files to one post.`);
      return;
    }

    const nextMedia: PendingMedia[] = [];
    for (const file of files) {
      const isImage = file.type.startsWith('image/');
      const isVideo = file.type.startsWith('video/');
      const maxSize = isVideo ? this.maxVideoSizeBytes : this.maxImageSizeBytes;

      if ((!isImage && !isVideo) || file.size > maxSize) {
        this.formMessage.set(isVideo
          ? `${file.name} is too large. Videos must be 60 MB or smaller.`
          : `${file.name} is not a supported image or is larger than 10 MB.`);
        nextMedia.forEach(media => URL.revokeObjectURL(media.previewUrl));
        return;
      }

      nextMedia.push({
        file,
        previewUrl: URL.createObjectURL(file),
        mediaType: isVideo ? 'video' : 'image'
      });
    }

    this.formMessage.set('');
    this.pendingMedia = [...this.pendingMedia, ...nextMedia];
  }

  removeMedia(index: number): void {
    const [removed] = this.pendingMedia.splice(index, 1);
    if (removed) URL.revokeObjectURL(removed.previewUrl);
    this.pendingMedia = [...this.pendingMedia];
  }

  async submitPost(): Promise<void> {
    if (this.isSubmitting()) return;

    const request: CreateCommunityPostRequest = {
      type: this.postType,
      title: this.postTitle.trim(),
      body: this.postBody.trim(),
      destination: this.postDestination.trim() || null
    };

    if (request.title.length < 3 || this.bodyLength < 10) {
      this.formMessage.set('Add a title and a little more detail so travelers can help.');
      return;
    }

    this.isSubmitting.set(true);
    this.formMessage.set('');

    try {
      const uploadedMedia: NonNullable<CreateCommunityPostRequest['media']> = this.existingMedia.map((media, index) => ({
        mediaType: media.mediaType === 'video' ? 'video' : 'image',
        url: media.url,
        thumbnailUrl: media.thumbnailUrl ?? null,
        altText: media.altText ?? null,
        sortOrder: index
      }));
      for (const [index, media] of this.pendingMedia.entries()) {
        const uploadResponse = await firstValueFrom(this.communityService.uploadMedia(media.file));
        if (!uploadResponse.data) {
          throw new Error(`Could not upload ${media.file.name}.`);
        }

        uploadedMedia.push({
          mediaType: uploadResponse.data.mediaType,
          url: uploadResponse.data.url,
          thumbnailUrl: uploadResponse.data.thumbnailUrl ?? null,
          altText: uploadResponse.data.altText,
          sortOrder: this.existingMedia.length + index
        });
      }

      request.media = uploadedMedia;
      const response = await firstValueFrom(this.editingPostId
        ? this.communityService.updatePost(this.editingPostId, request)
        : this.communityService.createPost(request));
      if (response.data) {
        this.posts.update(posts => this.editingPostId
          ? posts.map(post => post.id === this.editingPostId ? response.data! : post)
          : [response.data!, ...posts]);
        this.postTitle = '';
        this.postBody = '';
        this.postDestination = '';
        this.postTags = '';
        this.clearPendingMedia();
        this.existingMedia = [];
        this.editingPostId = '';
        this.showComposer = false;
        this.loadCommunitySidebar();
      }
    } catch (error: any) {
      this.formMessage.set(error?.error?.errors?.[0]?.message || error?.error?.message || 'We could not publish your post.');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  clearPendingMedia(): void {
    this.pendingMedia.forEach(media => URL.revokeObjectURL(media.previewUrl));
    this.pendingMedia = [];
  }

  removeExistingMedia(index: number): void {
    this.existingMedia = this.existingMedia.filter((_, mediaIndex) => mediaIndex !== index);
  }

  onBodyInput(event: Event): void {
    this.postBody = (event.target as HTMLElement).innerHTML;
  }

  formatBody(command: 'bold' | 'italic' | 'insertUnorderedList' | 'insertOrderedList' | 'formatBlock' | 'justifyLeft' | 'justifyCenter' | 'justifyRight' | 'undo' | 'redo'): void {
    this.bodyEditor?.nativeElement.focus();
    document.execCommand(command, false, command === 'formatBlock' ? 'blockquote' : undefined);
    this.postBody = this.bodyEditor?.nativeElement.innerHTML ?? this.postBody;
  }

  openMediaPicker(): void {
    (document.getElementById('post-media') as HTMLInputElement | null)?.click();
  }

  focusDestination(): void {
    document.getElementById('post-destination')?.focus();
  }

  onDestinationInput(): void {
    if (this.destinationLookupTimer) window.clearTimeout(this.destinationLookupTimer);
    const query = this.postDestination.trim();
    if (query.length < 2) {
      this.destinationSuggestions.set([]);
      return;
    }

    this.destinationLookupTimer = window.setTimeout(() => {
      this.communityService.suggestDestinations(query).subscribe({
        next: response => this.destinationSuggestions.set(response.data ?? []),
        error: () => this.destinationSuggestions.set([])
      });
    }, 280);
  }

  selectDestination(suggestion: DestinationSuggestion): void {
    this.postDestination = `${suggestion.city}, ${suggestion.country}`;
    this.destinationSuggestions.set([]);
  }

  saveDraft(): void {
    localStorage.setItem('safarni-community-draft', JSON.stringify({
      type: this.postType,
      title: this.postTitle,
      body: this.postBody,
      destination: this.postDestination,
      tags: this.postTags
    }));
    this.formMessage.set('Draft saved on this device.');
  }

  async toggleLike(event: Event, post: CommunityPost): Promise<void> {
    event.stopPropagation();
    if (!this.isSignedIn) {
      void this.router.navigate(['/login']);
      return;
    }

    try {
      const response = await firstValueFrom(this.communityService.toggleLike(post.id));
      if (response.data) {
        this.updatePost(post.id, {
          isLikedByCurrentUser: response.data.isActive,
          likesCount: response.data.newCount
        });
      }
    } catch {
      this.errorMessage.set('Like could not be updated. Please try again.');
    }
  }

  async toggleSave(event: Event, post: CommunityPost): Promise<void> {
    event.stopPropagation();
    if (!this.isSignedIn) {
      void this.router.navigate(['/login']);
      return;
    }

    try {
      const response = await firstValueFrom(this.communityService.toggleSave(post.id));
      if (response.data) {
        this.updatePost(post.id, {
          isSavedByCurrentUser: response.data.isActive,
          savesCount: response.data.newCount
        });
        this.loadCommunitySidebar();
      }
    } catch {
      this.errorMessage.set('Save could not be updated. Please try again.');
    }
  }

  postTypeLabel(type: CommunityPostType): string {
    return this.postTypes.find(item => item.value === type)?.label ?? type;
  }

  postTypeIcon(type: CommunityPostType): string {
    return this.postTypes.find(item => item.value === type)?.icon ?? 'article';
  }

  authorInitial(post: CommunityPost): string {
    return (post.author.displayName || 'Traveler').charAt(0).toUpperCase();
  }

  timeAgo(date: string): string {
    const seconds = Math.max(1, Math.floor((Date.now() - new Date(date).getTime()) / 1000));
    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;
    return new Date(date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }

  formatTripDate(date: string): string {
    return new Date(date).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatTripCost(amount: number | null): string {
    if (amount == null) return 'Budget flexible';
    return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(amount);
  }

  tripCover(index: number): string {
    const covers = [
      'assets/countries/turkey.webp',
      'assets/countries/thailand.webp',
      'assets/countries/japan.webp',
      'assets/countries/maldives.webp'
    ];

    return covers[index % covers.length];
  }

  postCover(post: CommunityPost, index = 0): string {
    const image = post.media.find(media => media.mediaType === 'image');
    return image?.thumbnailUrl || image?.url || this.destinationCover(post.destination, index);
  }

  destinationCover(destination: string | null | undefined, index = 0): string {
    const value = (destination || '').toLowerCase();
    if (value.includes('japan') || value.includes('tokyo') || value.includes('kyoto')) return 'assets/countries/japan.webp';
    if (value.includes('maldives')) return 'assets/countries/maldives.webp';
    if (value.includes('thailand') || value.includes('bangkok') || value.includes('bali')) return 'assets/countries/thailand.webp';
    if (value.includes('dubai')) return 'assets/countries/dubai.webp';
    const covers = ['assets/countries/turkey.webp', 'assets/countries/thailand.webp', 'assets/countries/japan.webp', 'assets/countries/maldives.webp'];
    return covers[index % covers.length];
  }

  async sharePost(event: Event, post: CommunityPost): Promise<void> {
    event.stopPropagation();
    const url = new URL(`/community/${post.id}`, window.location.origin).toString();

    try {
      if (navigator.share) {
        await navigator.share({ title: post.title, url });
        return;
      }

      await navigator.clipboard.writeText(url);
      this.formMessage.set('Link copied to your clipboard.');
      window.setTimeout(() => this.formMessage.set(''), 2500);
    } catch {
      // Sharing can be cancelled by the user; no error state is needed.
    }
  }

  private updatePost(postId: string, patch: Partial<CommunityPost>): void {
    this.posts.update(posts => posts.map(post => post.id === postId ? { ...post, ...patch } : post));
  }
}
