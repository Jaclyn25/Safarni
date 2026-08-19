import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';

export type CommunityPostType = 'TravelStory' | 'Vlog' | 'Question' | 'Recommendation';

export interface CommunityAuthor {
  userId: string;
  displayName: string;
  profilePhotoUrl?: string | null;
  bio?: string | null;
}

export interface CommunityMedia {
  id: string;
  mediaType: 'image' | 'video' | string;
  url: string;
  thumbnailUrl?: string | null;
  altText?: string | null;
  sortOrder: number;
}

export interface CommunityComment {
  id: string;
  parentCommentId?: string | null;
  body: string;
  isAcceptedAnswer: boolean;
  createdAt: string;
  author: CommunityAuthor;
}

export interface CommunityPost {
  id: string;
  type: CommunityPostType;
  title: string;
  body: string;
  destination?: string | null;
  linkedTripId?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  author: CommunityAuthor;
  likesCount: number;
  savesCount: number;
  commentsCount: number;
  isLikedByCurrentUser: boolean;
  isSavedByCurrentUser: boolean;
  media: CommunityMedia[];
  comments?: CommunityComment[] | null;
}

export interface CreateCommunityPostRequest {
  type: CommunityPostType;
  title: string;
  body: string;
  destination?: string | null;
  linkedTripId?: string | null;
  media?: Array<{
    mediaType: 'image' | 'video';
    url: string;
    thumbnailUrl?: string | null;
    altText?: string | null;
    sortOrder: number;
  }>;
}

export interface UpdateCommunityPostRequest {
  title: string;
  body: string;
  destination?: string | null;
  media?: Array<{
    mediaType: 'image' | 'video';
    url: string;
    thumbnailUrl?: string | null;
    altText?: string | null;
    sortOrder: number;
  }>;
}

export interface ToggleCommunityInteractionResult {
  postId: string;
  type: 'Like' | 'Save';
  newCount: number;
  isActive: boolean;
}

export interface CommunityMediaUploadResponse {
  mediaType: 'image' | 'video';
  url: string;
  thumbnailUrl?: string | null;
  altText: string;
}

export interface CreateCommunityCommentRequest {
  body: string;
  parentCommentId?: string | null;
}

export interface CreateCommunityCommentResult {
  commentId: string;
  newCommentsCount: number;
}

@Injectable({ providedIn: 'root' })
export class CommunityService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/community';

  getFeed(type?: CommunityPostType | '', destination?: string): Observable<ApiResponse<CommunityPost[]>> {
    let params = new HttpParams().set('take', '30');
    if (type) params = params.set('type', type);
    if (destination?.trim()) params = params.set('destination', destination.trim());

    return this.http.get<ApiResponse<CommunityPost[]>>(`${this.baseUrl}/feed`, { params });
  }

  getPost(postId: string): Observable<ApiResponse<CommunityPost>> {
    return this.http.get<ApiResponse<CommunityPost>>(`${this.baseUrl}/posts/${postId}`);
  }

  createPost(request: CreateCommunityPostRequest): Observable<ApiResponse<CommunityPost>> {
    return this.http.post<ApiResponse<CommunityPost>>(`${this.baseUrl}/posts`, request);
  }

  updatePost(postId: string, request: UpdateCommunityPostRequest): Observable<ApiResponse<CommunityPost>> {
    return this.http.put<ApiResponse<CommunityPost>>(`${this.baseUrl}/posts/${postId}`, request);
  }

  uploadMedia(file: File): Observable<ApiResponse<CommunityMediaUploadResponse>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<CommunityMediaUploadResponse>>(`${this.baseUrl}/media`, formData);
  }

  toggleLike(postId: string): Observable<ApiResponse<ToggleCommunityInteractionResult>> {
    return this.http.post<ApiResponse<ToggleCommunityInteractionResult>>(`${this.baseUrl}/posts/${postId}/like`, {});
  }

  toggleSave(postId: string): Observable<ApiResponse<ToggleCommunityInteractionResult>> {
    return this.http.post<ApiResponse<ToggleCommunityInteractionResult>>(`${this.baseUrl}/posts/${postId}/save`, {});
  }

  addComment(postId: string, request: CreateCommunityCommentRequest): Observable<ApiResponse<CreateCommunityCommentResult>> {
    return this.http.post<ApiResponse<CreateCommunityCommentResult>>(`${this.baseUrl}/posts/${postId}/comments`, request);
  }
}
