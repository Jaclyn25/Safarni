import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PackageBookingMessage, PackageBookingRequest } from '../../models/marketplace.models';

@Component({
  selector: 'app-booking-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (request; as activeRequest) {
      <div class="fixed inset-0 z-[70]" role="presentation">
        <button type="button" aria-label="Close booking chat" (click)="closed.emit()" class="absolute inset-0 h-full w-full cursor-default bg-slate-950/30 backdrop-blur-[1px]"></button>
        <section class="absolute inset-x-0 bottom-0 flex h-[min(88vh,44rem)] flex-col overflow-hidden rounded-t-[2rem] border border-slate-200 bg-slate-50 shadow-2xl sm:inset-y-4 sm:left-auto sm:right-4 sm:h-auto sm:w-[min(27rem,calc(100vw-2rem))] sm:rounded-[2rem]" role="dialog" aria-modal="true" aria-labelledby="booking-chat-title">
          <header class="shrink-0 bg-slate-950 px-5 pb-5 pt-4 text-white sm:px-6 sm:pt-5">
            <div class="mb-5 flex items-center justify-between gap-3">
              <div class="flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.16em] text-blue-200">
                <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-white/10"><span class="material-symbols-outlined text-base">forum</span></span>
                Booking conversation
              </div>
              <button type="button" (click)="closed.emit()" aria-label="Close booking chat" class="flex h-9 w-9 items-center justify-center rounded-xl text-slate-400 transition hover:bg-white/10 hover:text-white"><span class="material-symbols-outlined">close</span></button>
            </div>
            <div class="flex items-center gap-3">
              <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-blue-600 text-sm font-bold text-white">{{ initials(activeRequest) }}</div>
              <div class="min-w-0">
                <h2 id="booking-chat-title" class="truncate font-display text-lg font-bold">{{ counterpartName(activeRequest) }}</h2>
                <p class="mt-1 truncate text-xs text-slate-400">{{ activeRequest.packageTitle }} · {{ activeRequest.destination }}</p>
              </div>
            </div>
          </header>

          <div class="flex min-h-0 flex-1 flex-col bg-slate-50">
            <div class="flex items-center justify-between border-b border-slate-200 bg-white px-5 py-3 text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400 sm:px-6">
              <span>{{ messages.length ? messages.length + ' messages' : 'New conversation' }}</span>
              <span class="flex items-center gap-1.5 text-emerald-600"><span class="h-1.5 w-1.5 rounded-full bg-emerald-500"></span>Secure chat</span>
            </div>
            <div class="min-h-0 flex-1 space-y-3 overflow-y-auto px-5 py-5 sm:px-6">
              @for (message of messages; track message.id) {
                <div class="flex" [class.justify-end]="message.isMine">
                  <div class="max-w-[86%]">
                    <div class="mb-1 flex items-center gap-2" [class.justify-end]="message.isMine">
                      <span class="text-[10px] font-bold text-slate-500">{{ message.isMine ? 'You' : message.senderName }}</span>
                      <span class="text-[10px] text-slate-400">{{ message.createdAt | date:'shortTime' }}</span>
                    </div>
                    <div class="rounded-2xl px-3.5 py-3 text-sm leading-5" [class]="message.isMine ? 'rounded-br-md bg-blue-600 text-white' : 'rounded-bl-md border border-slate-200 bg-white text-slate-700 shadow-sm'">{{ message.body }}</div>
                  </div>
                </div>
              } @empty {
                <div class="flex h-full min-h-48 flex-col items-center justify-center text-center">
                  <span class="material-symbols-outlined text-3xl text-blue-600/70">waving_hand</span>
                  <p class="mt-3 text-sm font-semibold text-slate-700">Start the conversation</p>
                  <p class="mt-1 max-w-[22ch] text-xs leading-5 text-slate-400">Ask a question or share an update about this booking.</p>
                </div>
              }
            </div>
            <footer class="shrink-0 border-t border-slate-200 bg-white p-4 sm:p-5">
              <div class="flex items-end gap-2 rounded-2xl border border-slate-200 bg-slate-50 p-2 transition focus-within:border-blue-500 focus-within:bg-white focus-within:ring-4 focus-within:ring-blue-600/10">
                <textarea [ngModel]="draft" (ngModelChange)="draftChange.emit($event)" (keydown.control.enter)="sendMessage()" rows="1" aria-label="Write a booking message" placeholder="Write a message..." class="max-h-28 min-h-10 flex-1 resize-none border-0 bg-transparent px-2 py-2 text-sm text-slate-900 outline-none placeholder:text-slate-400"></textarea>
                <button type="button" (click)="sendMessage()" [disabled]="sending || !draft.trim()" aria-label="Send booking message" class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-blue-600 text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-40"><span class="material-symbols-outlined text-base">send</span></button>
              </div>
              <p class="mt-2 text-center text-[10px] text-slate-400">Press Ctrl + Enter to send</p>
            </footer>
          </div>
        </section>
      </div>
    }
  `
})
export class BookingChatPanelComponent {
  @Input() request: PackageBookingRequest | null = null;
  @Input() messages: PackageBookingMessage[] = [];
  @Input() draft = '';
  @Input() sending = false;
  @Input() counterpart: 'traveler' | 'agency' = 'agency';
  @Output() closed = new EventEmitter<void>();
  @Output() draftChange = new EventEmitter<string>();
  @Output() send = new EventEmitter<void>();

  counterpartName(request: PackageBookingRequest): string {
    return this.counterpart === 'traveler' ? request.travelerName : request.companyName || 'Travel agency';
  }

  initials(request: PackageBookingRequest): string {
    return this.counterpartName(request).slice(0, 2).toUpperCase();
  }

  sendMessage(): void {
    if (!this.draft.trim() || this.sending) return;
    this.send.emit();
  }
}
