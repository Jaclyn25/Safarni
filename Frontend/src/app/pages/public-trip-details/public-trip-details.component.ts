import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  PublicTripService,
  PublicTripDetailsResponse
} from '../../services/public-trip.service';
import { AuthService } from '../../services/auth.service';
import { firstValueFrom } from 'rxjs';
import { ChangeDetectorRef } from '@angular/core';

interface ItineraryPlace {
  id?: string;
  name?: string;
  category?: string;
  address?: string;
  time?: string;
  duration?: string;
  notes?: string;
  cost?: number;
  photoUrl?: string;
  isClosed?: boolean;
}

interface ItineraryDay {
  dayNumber: number;
  title?: string;
  description?: string;
  places: ItineraryPlace[];
}

@Component({
  selector: 'app-public-trip-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './public-trip-details.component.html',
  styleUrl: './public-trip-details.component.css'
})
export class PublicTripDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private publicTripService = inject(PublicTripService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  versionId: string = '';
  isLoading: boolean = true;
  errorMessage: string = '';

  trip: PublicTripDetailsResponse | null = null;
  parsedItinerary: ItineraryDay[] = [];
  parsedPhotos: string[] = [];

  // Interaction State
  isLiked: boolean = false;
  likesCount: number = 0;
  isSaved: boolean = false;
  savesCount: number = 0;
  copiesCount: number = 0;
  isLiking: boolean = false;
  isSaving: boolean = false;
  isCopying: boolean = false;

  // Rating State
  userScore: number = 0;
  hoverScore: number = 0;
  userComment: string = '';
  isSubmittingRating: boolean = false;
  ratingSuccessMessage: string = '';
  ratingErrorMessage: string = '';
  averageRating: number = 0;
  ratingsCount: number = 0;

  // Report State
  isReportModalOpen: boolean = false;
  reportReason: string = '';
  isSubmittingReport: boolean = false;
  reportSuccessMessage: string = '';
  reportErrorMessage: string = '';
  hasReported: boolean = false;

  // Toast feedback
  toastMessage: string = '';
  toastType: 'success' | 'error' | 'info' = 'info';

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('versionId');
      if (id) {
        this.versionId = id;
        this.loadTripDetails();
      } else {
        this.errorMessage = 'No version ID provided.';
        this.isLoading = false;
      }
    });
  }

  async loadTripDetails(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      const response = await firstValueFrom(
        this.publicTripService.getPublicTripDetails(this.versionId)
      );

      if (response && response.data) {
        this.trip = response.data;
        this.likesCount = this.trip.likesCount;
        this.savesCount = this.trip.savesCount;
        this.copiesCount = this.trip.copiesCount;
        this.averageRating = this.trip.averageRating;
        this.ratingsCount = this.trip.ratingsCount;
        this.isLiked = this.trip.isLikedByCurrentUser;
        this.isSaved = this.trip.isSavedByCurrentUser;

        if (this.trip.userRating) {
          this.userScore = this.trip.userRating.score;
          this.userComment = this.trip.userRating.comment || '';
        }

        this.parseItinerary(this.trip.itineraryJson);
        this.parsePhotos(this.trip.includedPhotoIdsJson);
      } else {
        this.errorMessage = response?.message || 'Failed to load public trip details.';
      }
    } catch (err: any) {
      this.errorMessage = err?.error?.message || err?.message || 'An error occurred while loading trip details.';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  private parseItinerary(json: string): void {
    if (!json) {
      this.parsedItinerary = [];
      return;
    }

    try {
      const raw = JSON.parse(json);
      if (Array.isArray(raw)) {
        this.parsedItinerary = raw.map((day: any, idx: number) => ({
          dayNumber: day.dayNumber || day.day || idx + 1,
          title: day.title || day.name || `Day ${day.dayNumber || idx + 1}`,
          description: day.description || '',
          places: Array.isArray(day.places) ? day.places : (Array.isArray(day.activities) ? day.activities : [])
        }));
      } else if (raw && Array.isArray(raw.days)) {
        this.parsedItinerary = raw.days.map((day: any, idx: number) => ({
          dayNumber: day.dayNumber || day.day || idx + 1,
          title: day.title || day.name || `Day ${day.dayNumber || idx + 1}`,
          description: day.description || '',
          places: Array.isArray(day.places) ? day.places : (Array.isArray(day.activities) ? day.activities : [])
        }));
      } else {
        this.parsedItinerary = [];
      }
    } catch (e) {
      console.warn('Failed to parse itinerary JSON', e);
      this.parsedItinerary = [];
    }
  }

  private parsePhotos(json: string): void {
    if (!json) {
      this.parsedPhotos = [];
      return;
    }

    try {
      const raw = JSON.parse(json);
      if (Array.isArray(raw)) {
        this.parsedPhotos = raw;
      } else {
        this.parsedPhotos = [];
      }
    } catch (e) {
      this.parsedPhotos = [];
    }
  }

  async toggleLike(): Promise<void> {
    if (!this.checkAuth()) return;
    if (this.isLiking) return;

    this.isLiking = true;
    try {
      const response = await firstValueFrom(
        this.publicTripService.toggleLike(this.versionId)
      );

      if (response && response.data) {
        this.isLiked = response.data.isActive;
        this.likesCount = response.data.newCount;
        this.showToast(this.isLiked ? 'Added to Liked Trips' : 'Removed from Liked Trips', 'success');
      }
    } catch (err: any) {
      this.showToast(err?.error?.message || 'Failed to toggle like', 'error');
    } finally {
      this.isLiking = false;
    }
  }

  async toggleSave(): Promise<void> {
    if (!this.checkAuth()) return;
    if (this.isSaving) return;

    this.isSaving = true;
    try {
      const response = await firstValueFrom(
        this.publicTripService.toggleSave(this.versionId)
      );

      if (response && response.data) {
        this.isSaved = response.data.isActive;
        this.savesCount = response.data.newCount;
        this.showToast(this.isSaved ? 'Saved to your bookmarks' : 'Removed from bookmarks', 'success');
      }
    } catch (err: any) {
      this.showToast(err?.error?.message || 'Failed to toggle save', 'error');
    } finally {
      this.isSaving = false;
    }
  }

  async copyTrip(): Promise<void> {
    if (!this.checkAuth()) return;
    if (this.isCopying) return;

    this.isCopying = true;
    try {
      const response = await firstValueFrom(
        this.publicTripService.copyTrip(this.versionId)
      );

      if (response && response.data && response.data.newTripId) {
        this.copiesCount++;
        this.showToast('Trip copied to your draft trips! Redirecting...', 'success');
        setTimeout(() => {
          if (response.data) {
  this.router.navigate(['/planner', response.data.newTripId]);
  }}, 1200);
      } else {
        this.showToast('Failed to copy trip.', 'error');
      }
    } catch (err: any) {
      this.showToast(err?.error?.message || 'Failed to copy trip.', 'error');
    } finally {
      this.isCopying = false;
    }
  }

  setRatingScore(score: number): void {
    this.userScore = score;
  }

  setHoverScore(score: number): void {
    this.hoverScore = score;
  }

  clearHoverScore(): void {
    this.hoverScore = 0;
  }

  async submitRating(): Promise<void> {
    console.log('[submitRating] Started', { score: this.userScore, comment: this.userComment });
    if (!this.checkAuth()) return;
    if (this.userScore < 1 || this.userScore > 5) {
      this.ratingErrorMessage = 'Please select a score between 1 and 5 stars.';
      return;
    }

    this.isSubmittingRating = true;
    this.ratingErrorMessage = '';
    this.ratingSuccessMessage = '';

    try {
      console.log('[submitRating] Sending request...');
      const response = await firstValueFrom(
        this.publicTripService.rateTrip(this.versionId, {
          score: this.userScore,
          comment: this.userComment?.trim() || undefined
        })
      );
      console.log('[submitRating] Response received:', response);

      if (response && response.data) {
        this.averageRating = response.data.newAverageRating;
        this.ratingsCount = response.data.newRatingsCount;
        this.ratingSuccessMessage = 'Thank you! Your rating has been submitted.';
        this.showToast('Rating saved successfully!', 'success');
      } else {
        this.ratingErrorMessage = response?.errors?.[0]?.message || response?.message || 'Failed to submit rating.';
      }
    } catch (err: any) {
      console.error('[submitRating] Error caught:', err);
      this.ratingErrorMessage = err?.error?.errors?.[0]?.message || err?.error?.message || err?.message || 'Failed to submit rating.';
    } finally {
      console.log('[submitRating] Finally block executing');
      this.isSubmittingRating = false;
      this.cdr.detectChanges();
    }
  }

  openReportModal(): void {
    if (!this.checkAuth()) return;
    if (this.hasReported) {
      this.showToast('You have already reported this trip.', 'info');
      return;
    }
    this.isReportModalOpen = true;
    this.reportErrorMessage = '';
    this.reportSuccessMessage = '';
  }

  closeReportModal(): void {
    this.isReportModalOpen = false;
    this.reportReason = '';
  }

  async submitReport(): Promise<void> {
    if (!this.reportReason.trim()) {
      this.reportErrorMessage = 'Please provide a reason for your report.';
      return;
    }

    this.isSubmittingReport = true;
    this.reportErrorMessage = '';

    try {
      const response = await firstValueFrom(
        this.publicTripService.reportTrip(this.versionId, {
          reason: this.reportReason.trim()
        })
      );

      if (response && response.data && response.data.success) {
        this.hasReported = true;
        this.showToast('Thank you. The report has been submitted to our moderation team.', 'success');
        this.closeReportModal();
      } else {
        this.reportErrorMessage = response?.message || 'Failed to submit report.';
      }
    } catch (err: any) {
      this.reportErrorMessage = err?.error?.message || 'Failed to submit report.';
    } finally {
      this.isSubmittingReport = false;
    }
  }

  shareTrip(): void {
    if (navigator.share) {
      navigator.share({
        title: this.trip?.tripName || 'Safarni Public Trip',
        url: window.location.href
      }).catch(() => {});
    } else {
      navigator.clipboard.writeText(window.location.href);
      this.showToast('Link copied to clipboard!', 'info');
    }
  }

  private checkAuth(): boolean {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], {
        queryParams: { returnUrl: this.router.url }
      });
      return false;
    }
    return true;
  }

  private showToast(message: string, type: 'success' | 'error' | 'info' = 'info'): void {
    this.toastMessage = message;
    this.toastType = type;
    setTimeout(() => {
      if (this.toastMessage === message) {
        this.toastMessage = '';
      }
    }, 3500);
  }
}