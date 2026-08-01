import {
  HttpErrorResponse
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  Observable,
  distinctUntilChanged,
  map,
  retry,
  switchMap,
  takeWhile,
  throwError,
  timer
} from 'rxjs';
import {
  ItineraryGenerationJob,
  ItineraryService
} from './itinerary.service';

@Injectable({
  providedIn: 'root'
})
export class ItineraryGenerationService {
  private static readonly POLL_INTERVAL_MS = 1500;

  constructor(
    private readonly itineraryService: ItineraryService
  ) {}

  createOrReuseIdempotencyKey(
    tripId: string
  ): string {
    const storageKey = this.idempotencyStorageKey(tripId);
    const existing = sessionStorage.getItem(storageKey);

    if (existing) {
      return existing;
    }

    const key = globalThis.crypto?.randomUUID?.() ??
      `${Date.now()}-${Math.random().toString(16).slice(2)}`;

    sessionStorage.setItem(storageKey, key);
    return key;
  }

  rememberJob(
    tripId: string,
    jobId: string
  ): void {
    sessionStorage.setItem(
      this.jobStorageKey(tripId),
      jobId
    );
  }

  getRememberedJobId(
    tripId: string
  ): string | null {
    return sessionStorage.getItem(
      this.jobStorageKey(tripId)
    );
  }

  clearRememberedGeneration(
    tripId: string
  ): void {
    sessionStorage.removeItem(
      this.jobStorageKey(tripId)
    );
    sessionStorage.removeItem(
      this.idempotencyStorageKey(tripId)
    );
  }

  trackJob(
    tripId: string,
    jobId: string,
    startImmediately = true
  ): Observable<ItineraryGenerationJob> {
    const initialDelay = startImmediately
      ? 0
      : ItineraryGenerationService.POLL_INTERVAL_MS;

    return timer(
      initialDelay,
      ItineraryGenerationService.POLL_INTERVAL_MS
    ).pipe(
      switchMap(() =>
        this.itineraryService
          .getGenerationJob(tripId, jobId)
          .pipe(
            retry({
              count: 4,
              delay: (error, retryCount) => {
                if (!this.isTransient(error)) {
                  return throwError(() => error);
                }

                return timer(
                  Math.min(5000, retryCount * 1000)
                );
              }
            })
          )
      ),
      map((response) => {
        if (!response.data) {
          throw new Error(
            'The server returned an empty generation status.'
          );
        }

        return response.data;
      }),
      distinctUntilChanged(
        (previous, current) =>
          previous.status === current.status &&
          previous.progressPercent === current.progressPercent &&
          previous.stage === current.stage &&
          previous.updatedAt === current.updatedAt
      ),
      takeWhile(
        (job) => !this.isTerminal(job.status),
        true
      )
    );
  }

  isTerminal(status: string): boolean {
    return (
      status === 'completed' ||
      status === 'failed' ||
      status === 'cancelled'
    );
  }

  private isTransient(error: unknown): boolean {
    return (
      error instanceof HttpErrorResponse &&
      [0, 502, 503, 504].includes(error.status)
    );
  }

  private jobStorageKey(tripId: string): string {
    return `safarni_itinerary_job_${tripId}`;
  }

  private idempotencyStorageKey(
    tripId: string
  ): string {
    return `safarni_itinerary_idempotency_${tripId}`;
  }
}
