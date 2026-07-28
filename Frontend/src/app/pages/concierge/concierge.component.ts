import {
  AfterViewChecked,
  Component,
  ElementRef,
  OnInit,
  ViewChild
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

  constructor(private chatService: ChatService) {}

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
        
        // Check localStorage first
        const savedId = localStorage.getItem('active_conversation_id');
        if (savedId && this.conversationsList.some(c => c.id === savedId)) {
          this.conversationId = savedId;
          this.loadConversationMessages(savedId);
        } else if (this.conversationsList.length > 0) {
          // Load the latest conversation
          const latest = this.conversationsList[this.conversationsList.length - 1];
          this.conversationId = latest.id;
          localStorage.setItem('active_conversation_id', latest.id);
          this.loadConversationMessages(latest.id);
        } else {
          // Create a new one
          this.startNewConversation();
        }
      },
      error: (err) => {
        console.error('Error fetching conversations, starting new one', err);
        this.startNewConversation();
      }
    });
  }

  startNewConversation(): void {
    this.stopResponse();
    this.chatService.startConversation('محادثة جديدة').subscribe({
      next: (conv) => {
        this.conversationId = conv.id;
        localStorage.setItem('active_conversation_id', conv.id);
        
        // Refresh conversations list
        this.chatService.getConversations().subscribe(list => {
          this.conversationsList = list || [];
        });

        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user'));
        this.shouldScroll = true;
      },
      error: (err) => {
        console.error('Failed to start conversation', err);
      }
    });
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
            localStorage.removeItem('active_conversation_id');
            this.conversationId = null;
            this.messages = [];
            
            if (this.conversationsList.length > 0) {
              const latest = this.conversationsList[this.conversationsList.length - 1];
              this.selectConversation(latest.id);
            } else {
              this.startNewConversation();
            }
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
    if (!text || !this.conversationId) {
      return;
    }

    // Add user message to UI immediately
    this.messages.push(this.toChatMessage(text, true));
    this.inputValue = '';
    this.shouldScroll = true;
    this.isTyping = true;

    this.messageSubscription = this.chatService.sendMessage(this.conversationId, text).subscribe({
      next: (conv) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user'));

        // Refresh list to update titles if changed from "محادثة جديدة"
        const currentConv = this.conversationsList.find(c => c.id === this.conversationId);
        if (currentConv && currentConv.title === 'محادثة جديدة') {
          this.chatService.getConversations().subscribe(list => {
            this.conversationsList = list || [];
          });
        }
        
        this.shouldScroll = true;
      },
      error: (err) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages.push({
          text: 'عذراً، حدث خطأ أثناء إرسال الرسالة. يرجى المحاولة مرة أخرى.',
          isUser: false,
          time: this.nowLabel(),
          formatted: this.formatMessage('Sorry, something went wrong while sending your message. Please try again.')
        });
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
