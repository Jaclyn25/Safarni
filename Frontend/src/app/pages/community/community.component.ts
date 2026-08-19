import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import {
  PublicTripSearchResponse,
  PublicTripService
} from '../../services/public-trip.service';
import {
  CommunityPost,
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
  private readonly communityService = inject(CommunityService);
  private readonly publicTripService = inject(PublicTripService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly posts = signal<CommunityPost[]>([]);
  readonly publicTrips = signal<PublicTripSearchResponse[]>([]);
  readonly isLoading = signal(true);
  readonly isPublicTripsLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal('');
  readonly formMessage = signal('');

  selectedType: CommunityPostType | '' = '';
  destinationFilter = '';
  showComposer = false;
  postType: CommunityPostType = 'TravelStory';
  postTitle = '';
  postBody = '';
  postDestination = '';
  pendingMedia: PendingMedia[] = [];

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
  }

  ngOnDestroy(): void {
    this.clearPendingMedia();
  }

  get isSignedIn(): boolean {
    return !!this.authService.getCurrentUser();
  }

  isOwnPost(post: CommunityPost): boolean {
    const currentUser = this.authService.getCurrentUser();
    return !!currentUser
      && post.author.userId.trim().toLowerCase() === currentUser.userId.trim().toLowerCase();
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

  openComposer(): void {
    if (!this.isSignedIn) {
      void this.router.navigate(['/login']);
      return;
    }
    this.showComposer = true;
    this.formMessage.set('');
  }

  closeComposer(): void {
    if (!this.isSubmitting()) {
      this.showComposer = false;
    }
  }

  onMediaSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';

    if (files.length === 0) return;

    if (this.pendingMedia.length + files.length > this.maxMediaFiles) {
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

    if (request.title.length < 3 || request.body.length < 10) {
      this.formMessage.set('Add a title and a little more detail so travelers can help.');
      return;
    }

    this.isSubmitting.set(true);
    this.formMessage.set('');

    try {
      const uploadedMedia: NonNullable<CreateCommunityPostRequest['media']> = [];
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
          sortOrder: index
        });
      }

      request.media = uploadedMedia;
      const response = await firstValueFrom(this.communityService.createPost(request));
      if (response.data) {
        this.posts.update(posts => [response.data!, ...posts]);
        this.postTitle = '';
        this.postBody = '';
        this.postDestination = '';
        this.clearPendingMedia();
        this.showComposer = false;
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

  editPost(event: Event, post: CommunityPost): void {
    event.stopPropagation();
    if (!this.isOwnPost(post)) return;
    void this.router.navigate(['/community', post.id], { queryParams: { edit: 'true' } });
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

  private updatePost(postId: string, patch: Partial<CommunityPost>): void {
    this.posts.update(posts => posts.map(post => post.id === postId ? { ...post, ...patch } : post));
  }
}
