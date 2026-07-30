import {
  AfterViewChecked,
  ChangeDetectorRef,
  Component,
  ElementRef,
  NgZone,
  OnDestroy,
  OnInit,
  ViewChild,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChatService, Conversation } from '../../services/chat.service';

export interface ItineraryDay {
  dayNumber: number;
  title: string;
  morning?: string;
  afternoon?: string;
  evening?: string;
  budget?: string;
}

export interface ActionChip {
  label: string;
  icon: string;
  promptText: string;
  actionType?: 'planner' | 'prompt';
}

export interface MessageLink {
  label: string;
  url: string;
  icon: string;
}

export interface FormattedMessage {
  direction: 'rtl' | 'ltr';
  summary?: string;
  paragraphs: string[];
  links: MessageLink[];
  itineraryDays: ItineraryDay[];
  actionChips: ActionChip[];
}

export interface ChatMessage {
  id: string;
  text: string;
  isUser: boolean;
  time: string;
  kind: 'normal' | 'error';
  formatted: FormattedMessage;
  versions?: string[];
  activeVersionIndex?: number;
  isEditing?: boolean;
  editText?: string;
  copied?: boolean;
  isRegenerating?: boolean;
  action?: 'new-chat';
}

export interface GroupedConversations {
  today: Conversation[];
  thisWeek: Conversation[];
  older: Conversation[];
}

@Component({
  selector: 'app-concierge',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './concierge.component.html',
  styleUrls: ['./concierge.component.css']
})
export class ConciergeComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('chatMessages') private chatMessagesRef?: ElementRef<HTMLDivElement>;
  @ViewChild('messageTextarea') private textareaRef?: ElementRef<HTMLTextAreaElement>;

  private chatService = inject(ChatService);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private zone = inject(NgZone);

  inputValue = '';
  searchConvQuery = '';
  isTyping = false;
  isLoadingConversation = false;
  conversationId: string | null = null;
  messages: ChatMessage[] = [];
  conversationsList: Conversation[] = [];
  readonly maxMessageLength = 4000;

  // Empty State Prompt Cards
  emptyStateCards = [
    {
      title: 'Build a 5-day trip',
      subtitle: 'Day-by-day itinerary with budget & spots',
      prompt: 'Build a complete 5-day itinerary for a relaxed vacation with estimated budget',
      icon: 'map'
    },
    {
      title: 'Compare two destinations',
      subtitle: 'Maldives vs Bali vs Thailand',
      prompt: 'Compare Maldives vs Thailand for a 7-day beach vacation including cost and vibe',
      icon: 'compare_arrows'
    },
    {
      title: 'Visa & travel requirements',
      subtitle: 'Passport, visa info & tips',
      prompt: 'What are the visa requirements and best time to visit for tourists?',
      icon: 'verified_user'
    },
    {
      title: 'Find hidden gems',
      subtitle: 'Secret spots & local dining',
      prompt: 'Suggest hidden gems and unique local experiences away from crowded tourist spots',
      icon: 'diamond'
    }
  ];

  private messageSubscription?: Subscription;
  private abortController: AbortController | null = null;
  private streamingMessageIndex: number | null = null;
  private activeRequestId = 0;
  private shouldScroll = false;
  private readonly urlPattern = /https?:\/\/[^\s)\]]+/gi;

  ngOnInit(): void {
    this.loadOrCreateConversation();

    // Handle incoming query param ?q=... from Explore or Destination pages
    this.route.queryParams.subscribe(params => {
      const q = params['q'];
      if (q && q.trim()) {
        setTimeout(() => {
          this.inputValue = q.trim();
          this.handleSend();
        }, 400);
      }
    });
  }

  ngOnDestroy(): void {
    this.stopResponse();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.chatMessagesRef) {
      const el = this.chatMessagesRef.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  loadOrCreateConversation(): void {
    this.chatService.getConversations().subscribe({
      next: (conversations) => {
        this.conversationsList = conversations || [];
        this.cdr.detectChanges();
        this.startNewConversation();
      },
      error: (err) => {
        console.error('Error fetching conversations', err);
        this.startNewConversation();
      }
    });
  }

  startNewConversation(): void {
    this.stopResponse();
    this.isLoadingConversation = false;
    this.conversationId = null;
    this.messages = [];
    this.shouldScroll = true;
    localStorage.removeItem('active_conversation_id');
  }

  loadConversationMessages(id: string): void {
    this.isLoadingConversation = true;
    this.messages = [];

    this.chatService.getConversationById(id).subscribe({
      next: (conv) => {
        this.isLoadingConversation = false;
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user'));
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoadingConversation = false;
        console.error('Failed to load conversation messages', err);
        this.addErrorMessage('Unable to load this conversation. Please try again.');
        this.cdr.detectChanges();
      }
    });
  }

  selectConversation(id: string): void {
    if (this.conversationId === id) return;
    this.stopResponse();
    this.conversationId = id;
    localStorage.setItem('active_conversation_id', id);
    this.loadConversationMessages(id);
  }

  openConversation(id: string): void {
    if (id) {
      this.selectConversation(id);
    } else {
      this.startNewConversation();
    }
  }

  // --- Custom Delete Confirmation Modal State & Handlers ---
  showDeleteModal = false;
  deletingConvId: string | null = null;
  deletingConvTitle: string = '';
  isDeletingConversation = false;

  confirmDeleteConversation(id: string, title: string, event: Event): void {
    event.stopPropagation();
    this.deletingConvId = id;
    this.deletingConvTitle = title || 'this trip chat';
    this.showDeleteModal = true;
  }

  cancelDeleteConversation(): void {
    this.showDeleteModal = false;
    this.deletingConvId = null;
    this.deletingConvTitle = '';
  }

  executeDeleteConversation(): void {
    if (!this.deletingConvId || this.isDeletingConversation) return;

    const id = this.deletingConvId;
    this.isDeletingConversation = true;

    this.chatService.deleteConversation(id).subscribe({
      next: () => {
        this.isDeletingConversation = false;
        this.showDeleteModal = false;
        this.conversationsList = this.conversationsList.filter(c => c.id !== id);

        if (this.conversationId === id) {
          this.startNewConversation();
        }

        this.deletingConvId = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isDeletingConversation = false;
        this.showDeleteModal = false;
        console.error('Failed to delete conversation', err);
        this.addErrorMessage('Could not delete this conversation. Please try again.');
        this.deletingConvId = null;
        this.cdr.detectChanges();
      }
    });
  }

  get groupedConversations(): GroupedConversations {
    const query = this.searchConvQuery.trim().toLowerCase();
    const filtered = this.conversationsList.filter(c => !query || c.title.toLowerCase().includes(query));

    const today: Conversation[] = [];
    const thisWeek: Conversation[] = [];
    const older: Conversation[] = [];

    const now = new Date();
    const oneDay = 24 * 60 * 60 * 1000;
    const sevenDays = 7 * oneDay;

    filtered.forEach(conv => {
      const updatedAt = new Date(conv.createdAt || Date.now());
      const diff = now.getTime() - updatedAt.getTime();

      if (diff < oneDay) {
        today.push(conv);
      } else if (diff < sevenDays) {
        thisWeek.push(conv);
      } else {
        older.push(conv);
      }
    });

    return { today, thisWeek, older };
  }

  handleSend(): void {
    if (this.isTyping) return;

    const text = this.inputValue.trim();
    if (!text) return;

    if (text.length > this.maxMessageLength) {
      this.addErrorMessage(`Message exceeds ${this.maxMessageLength} characters. Please shorten it.`);
      return;
    }

    this.messages.push(this.toChatMessage(text, true));
    this.inputValue = '';
    this.resetTextareaHeight();
    this.shouldScroll = true;
    this.startThinkingAnimation();
    this.renderNow();

    if (!this.conversationId) {
      const title = text.length > 32 ? `${text.substring(0, 32)}...` : text;
      this.chatService.startConversation(title).subscribe({
        next: (conv) => {
          this.zone.run(() => {
            this.conversationId = conv.id;
            localStorage.setItem('active_conversation_id', conv.id);
            this.renderNow();
            void this.streamMessage(conv.id, text, this.activeRequestId);
          });
        },
        error: (err) => {
          this.zone.run(() => {
            this.stopThinkingAnimation();
            console.error('Error starting conversation', err);
            this.addErrorMessage('Unable to start chat. Check connection and try again.');
            this.renderNow();
          });
        }
      });
      return;
    }

    void this.streamMessage(this.conversationId, text, this.activeRequestId);
  }

  onTextareaInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    if (textarea) {
      textarea.style.height = 'auto';
      textarea.style.height = `${Math.min(textarea.scrollHeight, 140)}px`;
    }
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.handleSend();
    }
  }

  private resetTextareaHeight(): void {
    if (this.textareaRef) {
      this.textareaRef.nativeElement.style.height = 'auto';
    }
  }

  stopResponse(): void {
    this.activeRequestId++;

    if (this.messageSubscription) {
      this.messageSubscription.unsubscribe();
      this.messageSubscription = undefined;
    }

    this.abortController?.abort();
    this.abortController = null;
    this.streamingMessageIndex = null;

    this.stopThinkingAnimation();
  }

  handleRefresh(): void {
    this.startNewConversation();
  }

  useSuggestion(text: string): void {
    if (this.isTyping) return;
    this.inputValue = text;
    this.handleSend();
  }

  executeActionChip(chip: ActionChip): void {
    if (chip.actionType === 'planner') {
      this.router.navigate(['/planner'], { queryParams: { trip: chip.promptText } });
    } else {
      this.useSuggestion(chip.promptText);
    }
  }

  // --- Message Controls & Toolbar Methods ---

  copyMessage(m: ChatMessage): void {
    if (!m.text) return;
    navigator.clipboard.writeText(m.text).then(() => {
      m.copied = true;
      this.cdr.detectChanges();
      setTimeout(() => {
        m.copied = false;
        this.cdr.detectChanges();
      }, 2000);
    }).catch(err => {
      console.error('Clipboard copy failed', err);
    });
  }

  startEdit(m: ChatMessage): void {
    m.isEditing = true;
    m.editText = m.text;
  }

  cancelEdit(m: ChatMessage): void {
    m.isEditing = false;
    m.editText = undefined;
  }

  saveEditAndResend(m: ChatMessage): void {
    if (!m.editText || !m.editText.trim() || this.isTyping) return;

    const newText = m.editText.trim();
    m.isEditing = false;
    m.editText = undefined;
    this.inputValue = newText;
    this.handleSend();
  }

  regenerateAiResponse(m: ChatMessage): void {
    if (this.isTyping || !this.conversationId || !this.isLatestAssistantMessage(m)) return;

    m.isRegenerating = true;
    this.startThinkingAnimation();

    this.chatService.regenerateResponse(this.conversationId).subscribe({
      next: (conv) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          m.isRegenerating = false;

          const updatedMessages = conv.messages;
          const lastAiMsg = updatedMessages[updatedMessages.length - 1];

          if (lastAiMsg && lastAiMsg.role !== 'user') {
            const newText = lastAiMsg.content;

            m.versions = m.versions || [m.text];
            m.versions.push(newText);
            m.activeVersionIndex = m.versions.length - 1;

            m.text = newText;
            m.formatted = this.formatMessage(newText, false);
          }

          this.refreshConversations();
          this.shouldScroll = true;
          this.renderNow();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          m.isRegenerating = false;
          console.error('Failed to regenerate response', err);
          this.addErrorMessage('Unable to regenerate response. Please try again.');
          this.renderNow();
        });
      }
    });
  }

  isLatestAssistantMessage(message: ChatMessage): boolean {
    const latestAssistant = [...this.messages]
      .reverse()
      .find(item => !item.isUser && item.kind === 'normal');

    return latestAssistant === message;
  }

  switchVersion(m: ChatMessage, delta: number): void {
    if (!m.versions || m.versions.length <= 1) return;

    const currentIdx = m.activeVersionIndex || 0;
    const newIdx = currentIdx + delta;

    if (newIdx >= 0 && newIdx < m.versions.length) {
      m.activeVersionIndex = newIdx;
      m.text = m.versions[newIdx];
      m.formatted = this.formatMessage(m.text, false);
      this.cdr.detectChanges();
    }
  }

  // --- Private Helpers ---

  private startThinkingAnimation(): void {
    this.isTyping = true;
    this.renderNow();
  }

  private stopThinkingAnimation(): void {
    this.isTyping = false;
    this.renderNow();
  }

  private async streamMessage(
    conversationId: string,
    text: string,
    requestId: number
  ): Promise<void> {
    const controller = new AbortController();
    this.abortController = controller;

    try {
      await this.chatService.streamMessage(
        conversationId,
        text,
        {
          onToken: (token) => {
            if (requestId !== this.activeRequestId) {
              return;
            }

            this.appendStreamingToken(token);
          },
          onCompleted: (conversation) => {
            if (requestId !== this.activeRequestId) {
              return;
            }

            this.messages = conversation.messages.map(message =>
              this.toChatMessage(message.content, message.role === 'user')
            );

            this.streamingMessageIndex = null;
            this.refreshConversations();
            this.shouldScroll = true;
            this.renderNow();
          }
        },
        controller.signal
      );
    } catch (error: unknown) {
      if (
        requestId !== this.activeRequestId ||
        controller.signal.aborted ||
        (error instanceof DOMException && error.name === 'AbortError')
      ) {
        return;
      }

      console.error('Error streaming chat response', error);
      const errorDetails = this.getSendErrorDetails(error);
      this.addErrorMessage(errorDetails.text, errorDetails.action);
      this.renderNow();
    } finally {
      if (requestId === this.activeRequestId) {
        this.abortController = null;
        this.streamingMessageIndex = null;
        this.stopThinkingAnimation();
      }
    }
  }

  private appendStreamingToken(token: string): void {
    if (this.streamingMessageIndex === null) {
      const message = this.toChatMessage(token, false);

      this.messages.push(message);
      this.streamingMessageIndex = this.messages.length - 1;
    } else {
      const message = this.messages[this.streamingMessageIndex];
      message.text += token;
      message.formatted = this.formatMessage(message.text, false);
    }

    this.shouldScroll = true;
    this.renderNow();
  }

  private sendMessage(convId: string, text: string): void {
    this.messageSubscription = this.chatService.sendMessage(convId, text).subscribe({
      next: (conv) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          this.messageSubscription = undefined;
          this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user'));
          this.refreshConversations();
          this.shouldScroll = true;
          this.renderNow();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          this.messageSubscription = undefined;
          console.error('Error sending message', err);
          const errorDetails = this.getSendErrorDetails(err);
          this.addErrorMessage(errorDetails.text, errorDetails.action);
          this.renderNow();
        });
      }
    });
  }

  private renderNow(): void {
    const shouldScrollAfterRender = this.shouldScroll;
    this.cdr.detectChanges();

    if (shouldScrollAfterRender) {
      window.setTimeout(() => {
        this.scrollMessagesToBottom();
      }, 0);
    }
  }

  private scrollMessagesToBottom(): void {
    const el = this.chatMessagesRef?.nativeElement;
    if (!el) {
      return;
    }

    el.scrollTop = el.scrollHeight;
    this.shouldScroll = false;
  }

  private refreshConversations(): void {
    this.chatService.getConversations().subscribe({
      next: (list) => {
        this.conversationsList = list || [];
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to refresh conversation list', err)
    });
  }

  private getSendErrorDetails(error: any): { text: string; action?: 'new-chat' } {
    if (error?.name === 'TimeoutError') {
      return { text: 'The request took longer than expected. Please try again with a shorter question.' };
    }
    if (error?.status === 0) {
      return { text: 'Unable to connect to the travel service. Please check your connection and try again.' };
    }
    if (error?.status === 429) {
      return {
        text: 'وصلت هذه المحادثة إلى الحد المتاح حاليًا. ابدأ شات جديد للمتابعة.',
        action: 'new-chat'
      };
    }
    return { text: 'An unexpected error occurred while preparing your travel plan. Please try again.' };
  }

  private addErrorMessage(text: string, action?: 'new-chat'): void {
    const message = this.toChatMessage(text, false, 'error');
    message.action = action;
    this.messages.push(message);
    this.shouldScroll = true;
  }

  private toChatMessage(text: string, isUser: boolean, kind: 'normal' | 'error' = 'normal'): ChatMessage {
    return {
      id: Math.random().toString(36).substring(2, 9),
      text,
      isUser,
      time: this.nowLabel(),
      kind,
      formatted: this.formatMessage(text, isUser),
      versions: isUser ? undefined : [text],
      activeVersionIndex: isUser ? undefined : 0
    };
  }

  private nowLabel(): string {
    return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  private formatMessage(text: string, isUser: boolean): FormattedMessage {
    const normalized = this.normalizeAssistantText(text);
    const links = this.extractLinks(normalized);
    const withoutUrls = normalized.replace(this.urlPattern, '').trim();
    const cleaned = this.cleanMarkdown(withoutUrls);

    const direction = this.detectDirection(cleaned);
    const paragraphs = this.extractParagraphs(cleaned);

    // AI Response specific parsing
    let summary: string | undefined = undefined;
    let itineraryDays: ItineraryDay[] = [];
    let actionChips: ActionChip[] = [];

    if (!isUser) {
      const hasSubstantialTravelResponse = paragraphs.length >= 3 && cleaned.length >= 280;
      if (hasSubstantialTravelResponse) {
        summary = paragraphs[0];
      }
      itineraryDays = this.parseItineraryDays(cleaned);
      actionChips = this.generateActionChips(cleaned, itineraryDays);
    }

    return {
      direction,
      summary,
      paragraphs: summary ? paragraphs.slice(1) : paragraphs,
      links,
      itineraryDays,
      actionChips
    };
  }

  private detectDirection(text: string): 'rtl' | 'ltr' {
    return /[\u0600-\u06FF]/.test(text) ? 'rtl' : 'ltr';
  }

  private normalizeAssistantText(text: string): string {
    return text
      .replace(/\r\n/g, '\n')
      .replace(/\\n/g, '\n')
      .replace(/<br\s*\/?>/gi, '\n')
      .replace(/<\/li>\s*<li[^>]*>/gi, '\n')
      .replace(/<\/?(?:ul|ol)[^>]*>/gi, '\n')
      .replace(/<\/?(?:p|div|span|strong|b|em|i|h[1-6])[^>]*>/gi, ' ')
      .replace(/&nbsp;/gi, ' ')
      .replace(/&amp;/gi, '&')
      .replace(/&lt;/gi, '<')
      .replace(/&gt;/gi, '>')
      .replace(/\n{3,}/g, '\n\n')
      .trim();
  }

  private cleanMarkdown(text: string): string {
    return text
      .replace(/^#{1,6}\s*/gm, '')
      .replace(/\*\*(.*?)\*\*/g, '$1')
      .replace(/__(.*?)__/g, '$1')
      .replace(/`{1,3}([^`]+)`{1,3}/g, '$1')
      .replace(/\[(.*?)\]\((.*?)\)/g, '$1')
      .replace(/^\s*[-*]\s+/gm, '• ')
      .replace(/[ \t]+/g, ' ')
      .replace(/\n{3,}/g, '\n\n')
      .trim();
  }

  private parseItineraryDays(text: string): ItineraryDay[] {
    const days: ItineraryDay[] = [];
    const dayRegex = /(?:Day|اليوم)\s*(\d+)[:\s–-]*(.*?)(?=(?:Day|اليوم)\s*\d+|$)/gsi;
    let match: RegExpExecArray | null;

    while ((match = dayRegex.exec(text)) !== null) {
      const dayNumber = parseInt(match[1], 10);
      const content = match[2].trim();

      if (dayNumber && content) {
        days.push({
          dayNumber,
          title: content.split('\n')[0].substring(0, 60),
          morning: this.extractTimeSlot(content, ['morning', 'صباحاً', 'صباح']),
          afternoon: this.extractTimeSlot(content, ['afternoon', 'ظهراً', 'عصراً']),
          evening: this.extractTimeSlot(content, ['evening', 'night', 'مساءً', 'ليلاً']),
          budget: this.extractBudgetSlot(content)
        });
      }
    }

    return days.slice(0, 7); // Cap at 7 days max
  }

  private extractTimeSlot(content: string, keywords: string[]): string | undefined {
    const lines = content.split('\n');
    for (const line of lines) {
      if (keywords.some(kw => line.toLowerCase().includes(kw))) {
        return line.replace(/^[-•*\s]+/, '').trim();
      }
    }
    return undefined;
  }

  private extractBudgetSlot(content: string): string | undefined {
    const match = content.match(/(\$\d+|\d+\s*USD|\d+\s*جنيه|\d+\s*دولار)/i);
    return match ? match[1] : undefined;
  }

  private generateActionChips(text: string, itineraryDays: ItineraryDay[]): ActionChip[] {
    // Strictly exclude fallback, out-of-scope, or error messages
    if (/(?:خارج نطاق|خارج التخصص|تعذر|حدث خطأ|ليس لدي معلومات)/i.test(text)) {
      return [];
    }

    // Require parsed day cards or explicit structured itinerary markers
    const isExplicitItinerary = itineraryDays.length > 0 ||
      /(?:Day\s*\d|اليوم\s*\d|خطة الرحلة|جدول الرحلة|البرنامج المقترح|Suggested Itinerary|Day-by-Day Plan)/i.test(text);

    if (!isExplicitItinerary) {
      return [];
    }

    const isArabic = this.detectDirection(text) === 'rtl';

    return [
      {
        label: isArabic ? 'إنشاء رحلة بالخطط' : 'Create Trip from this',
        icon: 'map',
        promptText: isArabic ? 'قم بإنشاء رحلة تفصيلية بناءً على هذا الرد' : 'Create a detailed trip in Planner based on this recommendation',
        actionType: 'planner'
      },
      {
        label: isArabic ? 'ميزانية أقل' : 'Make it cheaper',
        icon: 'savings',
        promptText: isArabic ? 'كيف يمكن تقليل ميزانية هذه الرحلة؟' : 'How can I lower the budget for this itinerary?',
        actionType: 'prompt'
      },
      {
        label: isArabic ? 'إضافة أنشطة ثقافية' : 'Add more culture',
        icon: 'museum',
        promptText: isArabic ? 'أضف المزيد من الأنشطة الثقافية والتاريخية لهذه الخطّة' : 'Add more historical and cultural experiences to this plan',
        actionType: 'prompt'
      },
      {
        label: isArabic ? 'اختصار لـ 3 أيام' : 'Shorten to 3 days',
        icon: 'schedule',
        promptText: isArabic ? 'اختصر هذه الرحلة لتصبح 3 أيام فقط' : 'Shorten this recommendation into a 3-day itinerary',
        actionType: 'prompt'
      }
    ];
  }

  private extractLinks(text: string): MessageLink[] {
    const matches = text.match(this.urlPattern) || [];
    const isArabic = this.detectDirection(text) === 'rtl';
    const seen = new Set<string>();

    return matches
      .map(rawUrl => rawUrl.replace(/[.,،؛]+$/, ''))
      .filter(url => {
        if (seen.has(url)) return false;
        seen.add(url);
        return true;
      })
      .map((url, index) => {
        const host = this.getHostname(url);
        const isMap = /maps\.google|google\.com\/maps|place_id/i.test(url);

        return {
          url,
          icon: isMap ? 'map' : 'open_in_new',
          label: isMap
            ? (isArabic ? 'افتح على الخريطة' : 'View on map')
            : (host || (isArabic ? `رابط ${index + 1}` : `Open Link ${index + 1}`))
        };
      });
  }

  private getHostname(url: string): string {
    try {
      return new URL(url).hostname.replace(/^www\./, '');
    } catch {
      return '';
    }
  }

  private extractParagraphs(text: string): string[] {
    return text
      .split(/\n{2,}|(?=\n\s*(?:[•*-]|\d+[.)])\s+)|(?<=[.!؟])\s+(?=[\u0600-\u06FFA-Z0-9])/)
      .map(p => p.replace(/\s+/g, ' ').trim())
      .filter(Boolean);
  }
}
