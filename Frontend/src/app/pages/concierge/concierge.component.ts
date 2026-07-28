import {
  AfterViewChecked,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  ChangeDetectorRef
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ChatService } from '../../services/chat.service';
import { Subscription } from 'rxjs';

interface ChatMessage {
  text: string;
  isUser: boolean;
  time: string;
  formatted: FormattedMessage;
}

interface FormattedMessage {
  direction: 'rtl' | 'ltr';
  paragraphs: string[];
  links: MessageLink[];
  recommendations: RecommendationItem[];
}

interface MessageLink {
  label: string;
  url: string;
  icon: string;
}

interface RecommendationItem {
  title: string;
  rating?: string;
  description: string;
  link?: MessageLink;
}

@Component({
  selector: 'app-concierge',
  standalone: true,
  imports: [FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './concierge.component.html'
})
export class ConciergeComponent implements OnInit, AfterViewChecked {
  @ViewChild('chatMessages') private chatMessagesRef?: ElementRef<HTMLDivElement>;

  inputValue = '';
  isTyping = false;
  conversationId: string | null = null;
  messages: ChatMessage[] = [];
  conversationsList: any[] = [];

  private messageSubscription?: Subscription;
  private shouldScroll = false;
  private readonly urlPattern = /https?:\/\/[^\s)\]]+/gi;

  constructor(private chatService: ChatService, private cdr: ChangeDetectorRef) { }

  ngOnInit(): void {
    this.loadOrCreateConversation();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.chatMessagesRef) {
      const el = this.chatMessagesRef.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  private nowLabel(): string {
    return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  loadOrCreateConversation(): void {
    this.chatService.getConversations().subscribe({
      next: (conversations) => {
        this.conversationsList = conversations || [];
        this.cdr.detectChanges();

        // Requirement 1: Open an empty chat on page load
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

    // Requirement 3: +new chat only opens an empty chat locally
    this.conversationId = null;
    this.messages = [];
    this.shouldScroll = true;
    localStorage.removeItem('active_conversation_id');
  }

  loadConversationMessages(id: string): void {
    this.chatService.getConversationById(id).subscribe({
      next: (conv) => {
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user'));
        this.shouldScroll = true;
      },
      error: (err) => {
        console.error('Failed to load conversation messages', err);
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

  deleteConversation(id: string, event: Event): void {
    event.stopPropagation(); // Prevent selection trigger

    if (confirm('هل أنت متأكد من رغبتك في حذف هذه المحادثة؟')) {
      this.chatService.deleteConversation(id).subscribe({
        next: () => {
          this.conversationsList = this.conversationsList.filter(c => c.id !== id);

          if (this.conversationId === id) {
            this.startNewConversation();
          }
        },
        error: (err) => {
          console.error('Failed to delete conversation', err);
        }
      });
    }
  }

  handleSend(): void {
    const text = this.inputValue.trim();
    if (!text) {
      return;
    }

    // Add user message to UI immediately
    this.messages.push(this.toChatMessage(text, true));
    this.inputValue = '';
    this.shouldScroll = true;
    this.isTyping = true;

    if (!this.conversationId) {
      // Create a new conversation on the backend first
      const title = text.length > 30 ? text.substring(0, 30) + '...' : text;
      this.chatService.startConversation(title).subscribe({
        next: (conv) => {
          this.conversationId = conv.id;
          localStorage.setItem('active_conversation_id', conv.id);

          // Now send the actual message
          this._sendMessage(conv.id, text);
        },
        error: (err) => {
          this.isTyping = false;
          this.messages.push(this.toChatMessage('عذراً، حدث خطأ أثناء إنشاء المحادثة.', false));
          console.error('Error starting conversation', err);
        }
      });
    } else {
      // Existing conversation
      this._sendMessage(this.conversationId, text);
    }
  }

  private _sendMessage(convId: string, text: string): void {
    this.messageSubscription = this.chatService.sendMessage(convId, text).subscribe({
      next: (conv) => {
        this.isTyping = false;
        this.messageSubscription = undefined;

        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user'));

        // Refresh the list to ensure the title is updated or newly created chat is visible
        this.chatService.getConversations().subscribe(list => {
          this.conversationsList = list || [];
          this.cdr.detectChanges();
        });

        this.shouldScroll = true;
      },
      error: (err) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages.push(this.toChatMessage('عذراً، حدث خطأ أثناء إرسال الرسالة. يرجى المحاولة مرة أخرى.', false));
        this.shouldScroll = true;
        console.error('Error sending message', err);
      }
    });
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

  private toChatMessage(text: string, isUser: boolean): ChatMessage {
    return {
      text,
      isUser,
      time: this.nowLabel(),
      formatted: this.formatMessage(text)
    };
  }

  private formatMessage(text: string): FormattedMessage {
    const links = this.extractLinks(text);
    const withoutUrls = text.replace(this.urlPattern, '').trim();
    const recommendations = this.extractRecommendations(withoutUrls, links);
    const cleaned = this.cleanMarkdown(withoutUrls);

    return {
      direction: this.detectDirection(cleaned),
      paragraphs: this.extractParagraphs(cleaned, recommendations),
      links: links.filter(link => !recommendations.some(item => item.link?.url === link.url)),
      recommendations
    };
  }

  private detectDirection(text: string): 'rtl' | 'ltr' {
    return /[\u0600-\u06FF]/.test(text) ? 'rtl' : 'ltr';
  }

  private cleanMarkdown(text: string): string {
    return text
      .replace(/\*\*(.*?)\*\*/g, '$1')
      .replace(/__(.*?)__/g, '$1')
      .replace(/\[(.*?)\]\((.*?)\)/g, '$1')
      .replace(/^\s*[-*]\s+/gm, '')
      .replace(/[ \t]+/g, ' ')
      .replace(/\n{3,}/g, '\n\n')
      .trim();
  }

  private extractLinks(text: string): MessageLink[] {
    const matches = text.match(this.urlPattern) || [];

    return matches.map((rawUrl, index) => {
      const url = rawUrl.replace(/[.,،؛]+$/, '');
      const host = this.getHostname(url);
      const isMap = /maps\.google|google\.com\/maps|place_id/i.test(url);

      return {
        url,
        icon: isMap ? 'map' : 'open_in_new',
        label: isMap ? 'View on map' : (host || `Open link ${index + 1}`)
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

  private extractRecommendations(text: string, links: MessageLink[]): RecommendationItem[] {
    const normalized = this.cleanMarkdown(text).replace(/\s*\n+\s*/g, ' ');
    const candidatePattern = /(?:^|\s)(?:\d+[.)]\s*)?([^:：.!؟]{3,80})\s*[:：]\s*([^:：]+?)(?=(?:\s+\d+[.)]\s*)?[^:：.!؟]{3,80}\s*[:：]|$)/g;
    const items: RecommendationItem[] = [];
    let match: RegExpExecArray | null;

    while ((match = candidatePattern.exec(normalized)) !== null) {
      const item = this.parseRecommendation(match[1], match[2], links[items.length]);
      if (item) {
        items.push(item);
      }
    }

    return items.length >= 2 ? items.slice(0, 6) : [];
  }

  private parseRecommendation(titleText: string, bodyText: string, link?: MessageLink): RecommendationItem | null {
    const title = titleText.replace(/^[*-]\s*/, '').trim();
    const body = bodyText.trim();
    const rating = body.match(/(?:⭐\s*)?([0-5](?:\.\d)?\s*\/\s*5)/)?.[1]?.replace(/\s+/g, '');

    if (!title || title.length > 80 || body.length < 12 || !rating) {
      return null;
    }

    return {
      title,
      rating,
      description: this.compactDescription(body.replace(/(?:⭐\s*)?[0-5](?:\.\d)?\s*\/\s*5/g, '')),
      link
    };
  }

  private compactDescription(text: string): string {
    return text
      .replace(/\s*\[.*?\]\s*/g, ' ')
      .replace(/\s+/g, ' ')
      .replace(/^[\s,،:：.-]+/, '')
      .slice(0, 220)
      .trim();
  }

  private extractParagraphs(text: string, recommendations: RecommendationItem[]): string[] {
    if (recommendations.length) {
      const intro = text.split(/(?:\d+[.)]\s*)?[^:：.!؟]{3,80}\s*[:：]/)[0]?.trim();
      return intro ? [intro] : [];
    }

    return text
      .split(/\n{2,}|(?<=[.!؟])\s+(?=[\u0600-\u06FFA-Z])/)
      .map(paragraph => paragraph.replace(/\s+/g, ' ').trim())
      .filter(Boolean)
      .slice(0, 6);
  }
}
