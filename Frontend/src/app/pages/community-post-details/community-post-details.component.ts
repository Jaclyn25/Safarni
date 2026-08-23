import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { CommunityComment, CommunityPost, CommunityService } from '../../services/community.service';
import { PublicTripSearchResponse, PublicTripService } from '../../services/public-trip.service';

interface PendingCommentImage {
  file: File;
  previewUrl: string;
}

@Component({
  selector: 'app-community-post-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './community-post-details.component.html',
  styleUrl: './community-post-details.component.css'
})
export class CommunityPostDetailsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly communityService = inject(CommunityService);
  private readonly authService = inject(AuthService);
  private readonly publicTripService = inject(PublicTripService);

  readonly post = signal<CommunityPost | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal('');
  readonly isCommenting = signal(false);
  readonly commentMessage = signal('');
  readonly relatedJourneys = signal<PublicTripSearchResponse[]>([]);
  readonly destinationPostCount = signal(0);

  commentBody = '';
  replyTo: CommunityComment | null = null;
  postId = '';
  activeMediaIndex = 0;
  showPostMenu = false;
  showEmojiPicker = false;
  pendingCommentImages: PendingCommentImage[] = [];
  readonly emojis = ['😀', '😍', '✈️', '🌍', '📍', '🏝️', '⛰️', '❤️', '👏', '🙏', '👍', '🎉'];

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.postId = params.get('postId') || '';
      this.activeMediaIndex = 0;
      if (this.postId) this.loadPost();
    });
  }

  get isSignedIn(): boolean {
    return !!this.authService.getCurrentUser();
  }

  get currentUserName(): string {
    return this.authService.getCurrentUser()?.fullName || 'Traveler';
  }

  get comments(): CommunityComment[] {
    return this.post()?.comments ?? [];
  }

  rootComments(): CommunityComment[] {
    return this.comments.filter(comment => !comment.parentCommentId);
  }

  repliesFor(commentId: string): CommunityComment[] {
    return this.comments.filter(comment => comment.parentCommentId === commentId);
  }

  loadPost(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.communityService.getPost(this.postId).subscribe({
      next: response => {
        this.post.set(response.data ?? null);
        this.loadRelatedJourneys(response.data?.destination);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(error?.error?.message || 'We could not load this post.');
        this.isLoading.set(false);
      }
    });
  }

  private loadRelatedJourneys(destination: string | null | undefined): void {
    if (!destination) {
      this.relatedJourneys.set([]);
      return;
    }
    this.publicTripService.searchPublicTrips({ destination, sortOrder: 'popular' }).subscribe({
      next: response => this.relatedJourneys.set((response.data ?? []).slice(0, 3)),
      error: () => this.relatedJourneys.set([])
    });
    this.communityService.getTrendingDestinations(20).subscribe({
      next: response => {
        const match = (response.data ?? []).find(item => item.destination.toLowerCase() === destination.toLowerCase());
        this.destinationPostCount.set(match?.postsCount ?? 1);
      },
      error: () => this.destinationPostCount.set(1)
    });
  }

  editPost(): void {
    const current = this.post();
    if (current) void this.router.navigate(['/community'], { queryParams: { edit: current.id } });
  }

  async deletePost(): Promise<void> {
    const current = this.post();
    if (!current || !window.confirm(`Delete “${current.title}”? This will remove it from the community.`)) return;
    try {
      await firstValueFrom(this.communityService.deletePost(current.id));
      void this.router.navigate(['/community']);
    } catch (error: any) {
      this.errorMessage.set(error?.error?.errors?.[0]?.message || 'The post could not be deleted.');
    }
  }

  async toggleLike(): Promise<void> {
    if (!this.requireSignIn()) return;
    const current = this.post();
    if (!current) return;

    try {
      const response = await firstValueFrom(this.communityService.toggleLike(current.id));
      if (response.data) {
        this.post.update(post => post ? ({
          ...post,
          isLikedByCurrentUser: response.data!.isActive,
          likesCount: response.data!.newCount
        }) : post);
      }
    } catch {
      this.errorMessage.set('Like could not be updated. Please try again.');
    }
  }

  async toggleSave(): Promise<void> {
    if (!this.requireSignIn()) return;
    const current = this.post();
    if (!current) return;

    try {
      const response = await firstValueFrom(this.communityService.toggleSave(current.id));
      if (response.data) {
        this.post.update(post => post ? ({
          ...post,
          isSavedByCurrentUser: response.data!.isActive,
          savesCount: response.data!.newCount
        }) : post);
      }
    } catch {
      this.errorMessage.set('Save could not be updated. Please try again.');
    }
  }

  startReply(comment: CommunityComment): void {
    if (!this.requireSignIn()) return;
    this.replyTo = comment;
    this.commentMessage.set('');
  }

  cancelReply(): void {
    this.replyTo = null;
  }

  addEmoji(emoji: string): void {
    this.commentBody += emoji;
    this.showEmojiPicker = false;
  }

  openCommentImagePicker(): void {
    (document.getElementById('comment-image') as HTMLInputElement | null)?.click();
  }

  onCommentImagesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (this.pendingCommentImages.length + files.length > 3) {
      this.commentMessage.set('You can attach up to three images to a comment.');
      return;
    }
    for (const file of files) {
      if (!file.type.startsWith('image/') || file.size > 10 * 1024 * 1024) {
        this.commentMessage.set('Comment images must be 10 MB or smaller.');
        return;
      }
      this.pendingCommentImages.push({ file, previewUrl: URL.createObjectURL(file) });
    }
    this.pendingCommentImages = [...this.pendingCommentImages];
  }

  removeCommentImage(index: number): void {
    const [removed] = this.pendingCommentImages.splice(index, 1);
    if (removed) URL.revokeObjectURL(removed.previewUrl);
    this.pendingCommentImages = [...this.pendingCommentImages];
  }

  selectMedia(index: number): void {
    this.activeMediaIndex = index;
  }

  showPreviousMedia(total: number): void {
    this.activeMediaIndex = (this.activeMediaIndex - 1 + total) % total;
  }

  showNextMedia(total: number): void {
    this.activeMediaIndex = (this.activeMediaIndex + 1) % total;
  }

  scrollToComments(): void {
    document.getElementById('comments-title')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  async submitComment(): Promise<void> {
    if (!this.requireSignIn() || this.isCommenting()) return;
    const body = this.commentBody.trim();
    if (!body && this.pendingCommentImages.length === 0) {
      this.commentMessage.set('Write something before posting.');
      return;
    }

    this.isCommenting.set(true);
    this.commentMessage.set('');
    try {
      const media = [];
      for (const [index, image] of this.pendingCommentImages.entries()) {
        const upload = await firstValueFrom(this.communityService.uploadMedia(image.file));
        if (!upload.data) throw new Error('Image upload failed.');
        media.push({
          mediaType: 'image' as const,
          url: upload.data.url,
          thumbnailUrl: upload.data.thumbnailUrl ?? null,
          altText: upload.data.altText,
          sortOrder: index
        });
      }
      await firstValueFrom(this.communityService.addComment(this.postId, {
        body,
        parentCommentId: this.replyTo?.id ?? null,
        media
      }));
      this.commentBody = '';
      this.replyTo = null;
      this.pendingCommentImages.forEach(image => URL.revokeObjectURL(image.previewUrl));
      this.pendingCommentImages = [];
      this.loadPost();
    } catch (error: any) {
      this.commentMessage.set(error?.error?.errors?.[0]?.message || error?.error?.message || 'Comment could not be added.');
    } finally {
      this.isCommenting.set(false);
    }
  }

  async sharePost(): Promise<void> {
    const url = window.location.href;
    try {
      if (navigator.share) {
        await navigator.share({ title: this.post()?.title || 'Safarni community post', url });
      } else {
        await navigator.clipboard.writeText(url);
        this.commentMessage.set('Link copied to your clipboard.');
      }
    } catch {
      // Sharing can be cancelled by the user, so no error state is needed.
    }
  }

  timeAgo(date: string): string {
    const seconds = Math.max(1, Math.floor((Date.now() - new Date(date).getTime()) / 1000));
    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;
    return new Date(date).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
  }

  postTypeLabel(type: string): string {
    return ({ TravelStory: 'Travel story', Vlog: 'Travel vlog', Question: 'Question', Recommendation: 'Recommendation' } as Record<string, string>)[type] || type;
  }

  postTypeIcon(type: string): string {
    return ({ TravelStory: 'auto_stories', Vlog: 'videocam', Question: 'help', Recommendation: 'tips_and_updates' } as Record<string, string>)[type] || 'article';
  }

  destinationCover(destination: string | null | undefined): string {
    const postImage = this.post()?.media.find(media => media.mediaType === 'image');
    if (postImage) return postImage.thumbnailUrl || postImage.url;
    const value = (destination || '').toLowerCase();
    if (value.includes('japan') || value.includes('tokyo') || value.includes('kyoto')) return 'assets/countries/japan.webp';
    if (value.includes('maldives')) return 'assets/countries/maldives.webp';
    if (value.includes('thailand') || value.includes('bangkok')) return 'assets/countries/thailand.webp';
    if (value.includes('dubai')) return 'assets/countries/dubai.webp';
    return 'assets/countries/turkey.webp';
  }

  destinationParts(destination: string | null | undefined): { city: string; country: string } {
    const parts = (destination || 'Featured destination').split(',').map(part => part.trim()).filter(Boolean);
    return { city: parts[0] || 'Featured destination', country: parts.slice(1).join(', ') || 'Shared by the community' };
  }

  private requireSignIn(): boolean {
    if (this.isSignedIn) return true;
    void this.router.navigate(['/login']);
    return false;
  }
}
