import {
  AfterViewChecked,
  ChangeDetectorRef,
  Component,
  ElementRef,
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
  isSaved?: boolean;
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

  inputValue = '';
  searchConvQuery = '';
  isTyping = false;
  isLoadingConversation = false;
  conversationId: string | null = null;
  messages: ChatMessage[] = [];
  conversationsList: Conversation[] = [];
  readonly maxMessageLength = 4000;

  // AI Thinking Steps State
  thinkingSteps: string[] = [
    'Analyzing your travel request...',
    'Checking destinations & climate...',
    'Building day-by-day route ideas...',
    'Estimating budget & travel tips...'
  ];
  currentThinkingIndex = 0;
  private thinkingTimer?: any;

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
    this.clearThinkingTimer();
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

  deleteConversation(id: string, event: Event): void {
    event.stopPropagation();

    if (confirm('Are you sure you want to delete this trip chat?')) {
      this.chatService.deleteConversation(id).subscribe({
        next: () => {
          this.conversationsList = this.conversationsList.filter(c => c.id !== id);

          if (this.conversationId === id) {
            this.startNewConversation();
          }

          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to delete conversation', err);
          this.addErrorMessage('Could not delete this conversation. Please try again.');
          this.cdr.detectChanges();
        }
      });
    }
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

    if (!this.conversationId) {
      const title = text.length > 32 ? `${text.substring(0, 32)}...` : text;
      this.chatService.startConversation(title).subscribe({
        next: (conv) => {
          this.conversationId = conv.id;
          localStorage.setItem('active_conversation_id', conv.id);
          this.sendMessage(conv.id, text);
        },
        error: (err) => {
          this.stopThinkingAnimation();
          console.error('Error starting conversation', err);
          this.addErrorMessage('Unable to start chat. Check connection and try again.');
          this.cdr.detectChanges();
        }
      });
      return;
    }

    this.sendMessage(this.conversationId, text);
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
    if (this.messageSubscription) {
      this.messageSubscription.unsubscribe();
      this.messageSubscription = undefined;
    }
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
    const index = this.messages.indexOf(m);
    if (index === -1) return;

    // Truncate message history after this user message
    this.messages = this.messages.slice(0, index + 1);

    m.text = newText;
    m.isEditing = false;
    m.editText = undefined;
    m.formatted = this.formatMessage(newText, true);

    this.shouldScroll = true;
    this.startThinkingAnimation();

    if (this.conversationId) {
      this.sendMessage(this.conversationId, newText);
    }
  }

  regenerateAiResponse(m: ChatMessage): void {
    if (this.isTyping || !this.conversationId) return;

    m.isRegenerating = true;
    this.startThinkingAnimation();

    this.chatService.regenerateResponse(this.conversationId).subscribe({
      next: (conv) => {
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
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.stopThinkingAnimation();
        m.isRegenerating = false;
        console.error('Failed to regenerate response', err);
        this.addErrorMessage('Unable to regenerate response. Please try again.');
        this.cdr.detectChanges();
      }
    });
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

  toggleSaveMessage(m: ChatMessage): void {
    m.isSaved = !m.isSaved;
  }

  // --- Private Helpers ---

  private startThinkingAnimation(): void {
    this.isTyping = true;
    this.currentThinkingIndex = 0;
    this.clearThinkingTimer();

    this.thinkingTimer = setInterval(() => {
      this.currentThinkingIndex = (this.currentThinkingIndex + 1) % this.thinkingSteps.length;
      this.cdr.detectChanges();
    }, 1800);
  }

  private stopThinkingAnimation(): void {
    this.isTyping = false;
    this.clearThinkingTimer();
  }

  private clearThinkingTimer(): void {
    if (this.thinkingTimer) {
      clearInterval(this.thinkingTimer);
      this.thinkingTimer = undefined;
    }
  }

  private sendMessage(convId: string, text: string): void {
    this.messageSubscription = this.chatService.sendMessage(convId, text).subscribe({
      next: (conv) => {
        this.stopThinkingAnimation();
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user'));
        this.refreshConversations();
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.stopThinkingAnimation();
        console.error('Error sending message', err);
        this.addErrorMessage(this.getSendErrorMessage(err));
        this.cdr.detectChanges();
      }
    });
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

  private getSendErrorMessage(error: any): string {
    if (error?.name === 'TimeoutError') {
      return 'The request took longer than expected. Please try again with a shorter question.';
    }
    if (error?.status === 0) {
      return 'Unable to connect to the travel service. Please check your connection and try again.';
    }
    if (error?.status === 429) {
      return 'Too many requests right now. Please wait a moment and try again.';
    }
    return 'An unexpected error occurred while preparing your travel plan. Please try again.';
  }

  private addErrorMessage(text: string): void {
    this.messages.push(this.toChatMessage(text, false, 'error'));
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
      if (paragraphs.length > 0) {
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
