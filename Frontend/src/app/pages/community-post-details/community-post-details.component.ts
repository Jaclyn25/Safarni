import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { CommunityComment, CommunityPost, CommunityService } from '../../services/community.service';

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

  readonly post = signal<CommunityPost | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal('');
  readonly isCommenting = signal(false);
  readonly commentMessage = signal('');

  commentBody = '';
  replyTo: CommunityComment | null = null;
  postId = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.postId = params.get('postId') || '';
      if (this.postId) this.loadPost();
    });
  }

  get isSignedIn(): boolean {
    return !!this.authService.getCurrentUser();
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
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(error?.error?.message || 'We could not load this post.');
        this.isLoading.set(false);
      }
    });
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
