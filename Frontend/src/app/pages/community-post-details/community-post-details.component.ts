import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { CommunityComment, CommunityPost, CommunityService } from '../../services/community.service';

interface PendingEditMedia {
  file: File;
  previewUrl: string;
  mediaType: 'image' | 'video';
}

@Component({
  selector: 'app-community-post-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './community-post-details.component.html',
  styleUrl: './community-post-details.component.css'
})
export class CommunityPostDetailsComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly communityService = inject(CommunityService);
  private readonly authService = inject(AuthService);

  readonly post = signal<CommunityPost | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal('');
  readonly isCommenting = signal(false);
  readonly commentMessage = signal('');
  readonly isEditing = signal(false);
  readonly isUpdating = signal(false);
  readonly editMessage = signal('');

  commentBody = '';
  replyTo: CommunityComment | null = null;
  postId = '';
  editTitle = '';
  editBody = '';
  editDestination = '';
  pendingEditMedia: PendingEditMedia[] = [];

  readonly maxEditMediaFiles = 10;
  readonly maxImageSizeBytes = 10 * 1024 * 1024;
  readonly maxVideoSizeBytes = 60 * 1024 * 1024;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.postId = params.get('postId') || '';
      if (this.postId) this.loadPost();
    });
  }

  ngOnDestroy(): void {
    this.clearPendingEditMedia();
  }

  get isSignedIn(): boolean {
    return !!this.authService.getCurrentUser();
  }

  get comments(): CommunityComment[] {
    return this.post()?.comments ?? [];
  }

  get canEdit(): boolean {
    const currentPost = this.post();
    const currentUser = this.authService.getCurrentUser();
    return !!currentPost && !!currentUser && currentPost.author.userId === currentUser.userId;
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
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(error?.error?.message || 'We could not load this post.');
        this.isLoading.set(false);
      }
    });
  }

  openEdit(): void {
    const currentPost = this.post();
    if (!currentPost || !this.canEdit) return;

    this.editTitle = currentPost.title;
    this.editBody = currentPost.body;
    this.editDestination = currentPost.destination ?? '';
    this.editMessage.set('');
    this.isEditing.set(true);
  }

  closeEdit(): void {
    if (!this.isUpdating()) {
      this.isEditing.set(false);
      this.editMessage.set('');
      this.clearPendingEditMedia();
    }
  }

  onEditMediaSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';

    if (files.length === 0) return;

    if (this.pendingEditMedia.length + files.length > this.maxEditMediaFiles) {
      this.editMessage.set(`You can add up to ${this.maxEditMediaFiles} new files at a time.`);
      return;
    }

    const nextMedia: PendingEditMedia[] = [];
    for (const file of files) {
      const isImage = file.type.startsWith('image/');
      const isVideo = file.type.startsWith('video/');
      const maxSize = isVideo ? this.maxVideoSizeBytes : this.maxImageSizeBytes;

      if ((!isImage && !isVideo) || file.size > maxSize) {
        this.editMessage.set(isVideo
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

    this.editMessage.set('');
    this.pendingEditMedia = [...this.pendingEditMedia, ...nextMedia];
  }

  removeEditMedia(index: number): void {
    const [removed] = this.pendingEditMedia.splice(index, 1);
    if (removed) URL.revokeObjectURL(removed.previewUrl);
    this.pendingEditMedia = [...this.pendingEditMedia];
  }

  async submitEdit(): Promise<void> {
    const currentPost = this.post();
    if (!currentPost || !this.canEdit || this.isUpdating()) return;

    const title = this.editTitle.trim();
    const body = this.editBody.trim();
    if (title.length < 3 || body.length < 10) {
      this.editMessage.set('Add a title and a little more detail so travelers can help.');
      return;
    }

    this.isUpdating.set(true);
    this.editMessage.set('');

    try {
      const uploadedMedia = [] as Array<{
        mediaType: 'image' | 'video';
        url: string;
        thumbnailUrl: string | null;
        altText: string;
        sortOrder: number;
      }>;

      for (const [index, media] of this.pendingEditMedia.entries()) {
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

      const response = await firstValueFrom(this.communityService.updatePost(currentPost.id, {
        title,
        body,
        destination: this.editDestination.trim() || null,
        media: uploadedMedia
      }));

      if (response.data) {
        this.post.set(response.data);
        this.isEditing.set(false);
        this.clearPendingEditMedia();
      }
    } catch (error: any) {
      this.editMessage.set(error?.error?.errors?.[0]?.message || error?.error?.message || 'The post could not be updated.');
    } finally {
      this.isUpdating.set(false);
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

  async submitComment(): Promise<void> {
    if (!this.requireSignIn() || this.isCommenting()) return;
    const body = this.commentBody.trim();
    if (!body) {
      this.commentMessage.set('Write something before posting.');
      return;
    }

    this.isCommenting.set(true);
    this.commentMessage.set('');
    try {
      await firstValueFrom(this.communityService.addComment(this.postId, {
        body,
        parentCommentId: this.replyTo?.id ?? null
      }));
      this.commentBody = '';
      this.replyTo = null;
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

  private clearPendingEditMedia(): void {
    this.pendingEditMedia.forEach(media => URL.revokeObjectURL(media.previewUrl));
    this.pendingEditMedia = [];
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

  private requireSignIn(): boolean {
    if (this.isSignedIn) return true;
    void this.router.navigate(['/login']);
    return false;
  }
}
