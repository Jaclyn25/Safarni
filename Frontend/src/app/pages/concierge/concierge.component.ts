import {
  AfterViewChecked,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChatService, Conversation } from '../../services/chat.service';

interface ChatMessage {
  text: string;
  isUser: boolean;
  time: string;
  kind: 'normal' | 'error';
  formatted: FormattedMessage;
}

interface FormattedMessage {
  direction: 'rtl' | 'ltr';
  paragraphs: string[];
  links: MessageLink[];
}

interface MessageLink {
  label: string;
  url: string;
  icon: string;
}

@Component({
  selector: 'app-concierge',
  standalone: true,
  imports: [FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './concierge.component.html'
})
export class ConciergeComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('chatMessages') private chatMessagesRef?: ElementRef<HTMLDivElement>;

  inputValue = '';
  isTyping = false;
  isLoadingConversation = false;
  conversationId: string | null = null;
  messages: ChatMessage[] = [];
  conversationsList: Conversation[] = [];
  readonly maxMessageLength = 4000;

  private messageSubscription?: Subscription;
  private shouldScroll = false;
  private readonly urlPattern = /https?:\/\/[^\s)\]]+/gi;

  constructor(private chatService: ChatService, private cdr: ChangeDetectorRef) { }

  ngOnInit(): void {
    this.loadOrCreateConversation();
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
        this.addErrorMessage('لم أتمكن من تحميل هذه المحادثة. حاول مرة أخرى.');
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
      return;
    }

    this.startNewConversation();
  }

  deleteConversation(id: string, event: Event): void {
    event.stopPropagation();

    if (confirm('هل أنت متأكد من حذف هذه المحادثة؟')) {
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
          this.addErrorMessage('لم أتمكن من حذف المحادثة الآن. حاول مرة أخرى بعد قليل.');
          this.cdr.detectChanges();
        }
      });
    }
  }

  handleSend(): void {
    if (this.isTyping) {
      return;
    }

    const text = this.inputValue.trim();
    if (!text) {
      return;
    }

    if (text.length > this.maxMessageLength) {
      this.addErrorMessage(`الرسالة أطول من ${this.maxMessageLength} حرف. اختصرها قليلًا ثم أرسلها مرة أخرى.`);
      return;
    }

    this.messages.push(this.toChatMessage(text, true));
    this.inputValue = '';
    this.shouldScroll = true;
    this.isTyping = true;

    if (!this.conversationId) {
      const title = text.length > 30 ? `${text.substring(0, 30)}...` : text;
      this.chatService.startConversation(title).subscribe({
        next: (conv) => {
          this.conversationId = conv.id;
          localStorage.setItem('active_conversation_id', conv.id);
          this.sendMessage(conv.id, text);
        },
        error: (err) => {
          this.isTyping = false;
          console.error('Error starting conversation', err);
          this.addErrorMessage('لم أستطع بدء محادثة جديدة. تأكد أن الخادم يعمل ثم حاول مرة أخرى.');
          this.cdr.detectChanges();
        }
      });
      return;
    }

    this.sendMessage(this.conversationId, text);
  }

  stopResponse(): void {
    if (this.messageSubscription) {
      this.messageSubscription.unsubscribe();
      this.messageSubscription = undefined;
    }

    this.isTyping = false;
  }

  handleRefresh(): void {
    this.startNewConversation();
  }

  private sendMessage(convId: string, text: string): void {
    this.messageSubscription = this.chatService.sendMessage(convId, text).subscribe({
      next: (conv) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user'));

        this.refreshConversations();

        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        console.error('Error sending message', err);

        this.addErrorMessage(this.getSendErrorMessage(err));
        this.cdr.detectChanges();
      }
    });
  }

  useSuggestion(text: string): void {
    if (this.isTyping) {
      return;
    }

    this.inputValue = text;
    this.handleSend();
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
      return 'الرد استغرق وقتًا أطول من المتوقع. حاول مرة أخرى، أو قسّم سؤالك إلى جزأين.';
    }

    if (error?.status === 0) {
      return 'لا أستطيع الوصول إلى خدمة المحادثة الآن. تحقق من الاتصال ثم حاول مرة أخرى.';
    }

    if (error?.status === 400) {
      return 'الرسالة لم تُقبل بصيغتها الحالية. راجع طولها وحاول مرة أخرى.';
    }

    if (error?.status === 401 || error?.status === 403) {
      return 'انتهت جلسة الدخول أو لا تملك صلاحية الوصول. سجّل الدخول ثم أعد المحاولة.';
    }

    if (error?.status === 429) {
      return 'هناك طلبات كثيرة الآن. انتظر لحظة قصيرة ثم أعد المحاولة.';
    }

    return 'حدث عطل مؤقت أثناء تجهيز الرد. رسالتك ما زالت ظاهرة هنا ويمكنك إعادة المحاولة.';
  }

  private addErrorMessage(text: string): void {
    this.messages.push(this.toChatMessage(text, false, 'error'));
    this.shouldScroll = true;
  }

  private toChatMessage(text: string, isUser: boolean, kind: 'normal' | 'error' = 'normal'): ChatMessage {
    return {
      text,
      isUser,
      time: this.nowLabel(),
      kind,
      formatted: this.formatMessage(text)
    };
  }

  private nowLabel(): string {
    return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  private formatMessage(text: string): FormattedMessage {
    const normalized = this.normalizeAssistantText(text);
    const links = this.extractLinks(normalized);
    const withoutUrls = normalized.replace(this.urlPattern, '').trim();
    const cleaned = this.cleanMarkdown(withoutUrls);

    return {
      direction: this.detectDirection(cleaned),
      paragraphs: this.extractParagraphs(cleaned),
      links
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

  private extractLinks(text: string): MessageLink[] {
    const matches = text.match(this.urlPattern) || [];
    const isArabic = this.detectDirection(text) === 'rtl';
    const seen = new Set<string>();

    return matches
      .map(rawUrl => rawUrl.replace(/[.,،؛]+$/, ''))
      .filter(url => {
        if (seen.has(url)) {
          return false;
        }

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
            : (host || (isArabic ? `افتح الرابط ${index + 1}` : `Open link ${index + 1}`))
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
      .map(paragraph => paragraph.replace(/\s+/g, ' ').trim())
      .filter(Boolean);
  }
}
